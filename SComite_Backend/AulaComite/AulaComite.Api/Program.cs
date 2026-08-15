using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using AulaComite.Api.Middlewares;
using AulaComite.Api.Services;
using AulaComite.Application;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Models;
using AulaComite.Infrastructure;
using AulaComite.Infrastructure.Persistence;
using AulaComite.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using IPNetwork = System.Net.IPNetwork;

// 1. Inicializar Serilog desde appsettings
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
        .Build())
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Iniciando el host del servidor Backend de Comité de Aula...");

    var builder = WebApplication.CreateBuilder(args);

    // Conectar Serilog al Host de .NET
    builder.Host.UseSerilog();

    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

    // Configuración de Servicios
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // Vincular JwtSettings a opciones para emisión y validación de tokens locales
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"]
        ?? throw new InvalidOperationException("JwtSettings:SecretKey no está configurado.");
    var issuer = jwtSettings["Issuer"]
        ?? throw new InvalidOperationException("JwtSettings:Issuer no está configurado.");
    var audience = jwtSettings["Audience"]
        ?? throw new InvalidOperationException("JwtSettings:Audience no está configurado.");

    builder.Services.AddHttpClient<ISasiAuthService, SasiAuthService>(client =>
    {
        var baseUrl = builder.Configuration["SasiSettings:BaseUrl"]
            ?? throw new InvalidOperationException("SasiSettings:BaseUrl no está configurado.");
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        // 🛡️ T2.5: Timeout de 10 segundos para no dejar colgada la autenticación/consulta a SASI.
        client.Timeout = TimeSpan.FromSeconds(10);
    });

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        // Roles con acceso a la gestión del aula. ManejoFinanciero y GestionEscolar
        // comparten la misma regla funcional, por lo que se definen desde un único
        // origen para evitar duplicación/deriva entre políticas.
        var rolesGestionAula = new[] { "Administrador", "Comité de Aula" };

        options.AddPolicy("Administrador", policy =>
            policy.RequireAuthenticatedUser().RequireRole("Administrador"));

        options.AddPolicy("ManejoFinanciero", policy =>
            policy.RequireAuthenticatedUser().RequireRole(rolesGestionAula));

        options.AddPolicy("GestionEscolar", policy =>
            policy.RequireAuthenticatedUser().RequireRole(rolesGestionAula));

        options.AddPolicy("AccesoApoderado", policy =>
            policy.RequireAuthenticatedUser().RequireRole("Administrador", "Apoderado"));
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // 🛡️ Trustar cabeceras de proxy inverso (IIS/ARR de runasp, Cloudflare, NGINX) para
    // resolver la dirección IP real del cliente y el esquema HTTP original.
    // Se debe ejecutar antes de autenticación y del Rate Limiter.
    //
    // Hardening: solo se procesan las cabeceras cuando el peer directo es un proxy de
    // confianza — bucle local, redes privadas del host de aplicaciones o los rangos IP
    // publicados por Cloudflare. Un cliente de Internet que NO provenga de uno de estos
    // nodos no puede inyectar X-Forwarded-For para falsificar su IP de auditoría/rate-limit.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 2;

        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        // Nodos directos de confianza (IIS/ARR del mismo host y bucle local de pruebas).
        options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Loopback, 8));       // 127.0.0.0/8
        options.KnownIPNetworks.Add(new IPNetwork(IPAddress.IPv6Loopback, 128)); // ::1
        AgregarRedesPrivadas(options.KnownIPNetworks);
        AgregarRedesCloudflare(options.KnownIPNetworks);
    });

    // Limitar intentos de inicio de sesión por IP real para mitigar ataques de fuerza bruta.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("LoginLimiter", httpContext =>
        {
            var partitionKey = ObtenerPartitionKey(httpContext, incluirUsuario: false);
            return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
        });

        // Rate Limiter Global: combina la IP real del cliente (detrás de proxies)
        // con la identidad del usuario autenticado cuando existe sesión activa.
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var partitionKey = ObtenerPartitionKey(httpContext, incluirUsuario: true);
            return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
        });
    });

    static string ObtenerIpRealCliente(HttpContext httpContext)
    {
        // 🛡️ Lógica unificada con UserContextService.ObtenerIpCliente: NO se lee
        // X-Forwarded-For a ciegas; se usa la IP ya resuelta por UseForwardedHeaders
        // (que solo confía en proxies conocidos configurados arriba).
        var userContext = httpContext.RequestServices.GetService<IUserContextService>();
        if (userContext != null)
        {
            return userContext.ObtenerIpCliente();
        }

        // Fallback sin cabeceras: IP del nodo directo (seguro por defecto).
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    static void AgregarRedesPrivadas(ICollection<IPNetwork> networks)
    {
        networks.Add(new IPNetwork(IPAddress.Parse("10.0.0.0"), 8));     // 10.0.0.0/8
        networks.Add(new IPNetwork(IPAddress.Parse("172.16.0.0"), 12));   // 172.16.0.0/12
        networks.Add(new IPNetwork(IPAddress.Parse("192.168.0.0"), 16));  // 192.168.0.0/16
        networks.Add(new IPNetwork(IPAddress.Parse("169.254.0.0"), 16));  // link-local IPv4
        networks.Add(new IPNetwork(IPAddress.Parse("fe80::"), 10));       // link-local IPv6
        networks.Add(new IPNetwork(IPAddress.Parse("fc00::"), 7));        // ULA IPv6
    }

    static void AgregarRedesCloudflare(ICollection<IPNetwork> networks)
    {
        // Rangos IPv4 publicados por Cloudflare: https://www.cloudflare.com/ips-v4/
        var ipv4 = new[]
        {
            "173.245.48.0/20", "103.21.244.0/22", "103.22.200.0/22", "103.31.4.0/22",
            "141.101.64.0/18", "108.162.192.0/18", "190.93.240.0/20", "188.114.96.0/20",
            "197.234.240.0/22", "198.41.128.0/17", "162.158.0.0/15", "104.16.0.0/13",
            "104.24.0.0/14", "172.64.0.0/13", "131.0.72.0/22"
        };
        foreach (var cidr in ipv4) networks.Add(ParseIPNetwork(cidr));

        // Rangos IPv6 publicados por Cloudflare: https://www.cloudflare.com/ips-v6/
        var ipv6 = new[]
        {
            "2400:cb00::/32", "2606:4700::/32", "2803:f800::/32", "2405:b500::/32",
            "2405:8100::/32", "2a06:98c0::/29", "2c0f:f248::/32"
        };
        foreach (var cidr in ipv6) networks.Add(ParseIPNetwork(cidr));

        static IPNetwork ParseIPNetwork(string cidr)
        {
            var partes = cidr.Split('/');
            return new IPNetwork(IPAddress.Parse(partes[0]), int.Parse(partes[1]));
        }
    }

    static string ObtenerPartitionKey(HttpContext httpContext, bool incluirUsuario)
    {
        var ip = ObtenerIpRealCliente(httpContext);
        string identidad = "anonimo";

        if (incluirUsuario
            && httpContext.User?.Identity?.IsAuthenticated == true)
        {
            identidad = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User.FindFirst("sub")?.Value
                ?? httpContext.User.Identity?.Name
                ?? "anonimo";
        }

        return $"{ip}|{identidad}";
    }

    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    // Entorno de Producción: permitir el mismo-origen cuando no se configuran orígenes
    // externos, evitando exponer el API a un CORS "comodín" no deseado.
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsAngularPolicy", policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                policy.SetIsOriginAllowed(origin => false);
            }
        });
    });

    // 🚀 REGISTRO DE SERVICIOS (Usa 'builder.Environment', ANTES de builder.Build())
    if (builder.Environment.IsDevelopment())
    {
        // Mientras pruebes Cloudinary en tu laptop:
        //builder.Services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();

        // (Cuando quieras volver a guardar en disco local en desarrollo, solo cambias la línea de arriba por esta):
        builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
    }
    else
    {
        builder.Services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();
    }

    var app = builder.Build();

    var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    if (!Directory.Exists(webRootPath))
    {
        Directory.CreateDirectory(webRootPath);
    }

    // Middleware Global para captura de errores
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // 🛡️ Procesar cabeceras de proxy ANTES de autenticación y del Rate Limiter
    // para resolver la IP real del cliente detrás de Cloudflare/IIS/MonsterASP.
    app.UseForwardedHeaders();

    // Auditoría automática de peticiones HTTP en consola y logs
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    // 🛡️ HSTS: fuerza el tránsito estricto por HTTPS en Producción.
    if (app.Environment.IsProduction())
    {
        app.UseHsts();
    }

    app.UseCors("CorsAngularPolicy");
    app.UseStaticFiles();
    app.UseAuthentication();

    // El Rate Limiter se ejecuta DESPUÉS de la autenticación para poder combinar
    // la IP real del cliente con la identidad del usuario autenticado.
    app.UseRateLimiter();
    app.UseAuthorization();
    app.MapControllers();

    // 🛡️ OpenAPI y Scalar SOLO se exponen fuera de Producción: evita publicar el
    // contrato de la API (esquemas, endpoints) a Internet desde el entorno real.
    if (!app.Environment.IsProduction())
    {
        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("Sistema de Comité de Aula API")
                .WithTheme(ScalarTheme.DeepSpace)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        }).AllowAnonymous();
    }


    // ------------------------------------------------------------------------
    // Aplicar Migraciones Automáticas en el arranque (Desarrollo y Producción).
    // NOSONAR: La regla de Sonar "Database.Migrate() en el arranque" (mínimo
    // privilegio / DDL en tiempo de ejecución) se suprime de forma deliberada y
    // documentada porque en este hosting (runasp, instancia única + BD única)
    // es la ÚNICA vía viable:
    //   * Los runners de GitHub/CI NO pueden conectar a SQL Server (timeout
    //     TCP 258: runasp bloquea IPs externas), demostrado en CI.
    //   * El servidor es el único proceso que alcanza su propia BD.
    //   * No hay concurrencia (1 instancia) y EF Core 9+ usa lock global de BD.
    // Safety valve: AplicarMigracionesAutomaticas=false desactiva el bloque.
    // Fail-open: si algo falla se registra el error y la app arranca igual.
    // ------------------------------------------------------------------------
    if (app.Configuration.GetValue<bool>("AplicarMigracionesAutomaticas", true))
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                context.Database.Migrate(); // NOSONAR - justificación en el bloque superior
                Log.Information("Migraciones de la base de datos verificadas/aplicadas correctamente.");
            }
            catch (Exception ex)
            {
                Log.Warning("Atención al aplicar migraciones: {Message}", ex.Message);
            }
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló críticamente durante el arranque.");
}
finally
{
    Log.CloseAndFlush();
}
