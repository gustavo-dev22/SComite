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

    // 🛡️ Trustar cabeceras de proxy inverso (Cloudflare/IIS/MonsterASP/NGINX) para
    // resolver la dirección IP real del cliente y el esquema HTTP original.
    // Se debe ejecutar antes de autenticación y del Rate Limiter.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Los proxies de Cloudflare/IIS rotan direcciones, por lo que se aceptan
        // cabeceras de cualquier origen. La validación final de la IP de auditoría
        // la realiza UserContextService (no confía en X-Forwarded-For de orígenes privados).
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.ForwardLimit = null;
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
        // Tras UseForwardedHeaders, RemoteIpAddress ya es la IP real. Aun así, se
        // prefiere X-Forwarded-For (primer valor) como capa adicional de compatibilidad.
        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
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
    app.UseCors("CorsAngularPolicy");
    app.UseStaticFiles();
    app.UseAuthentication();

    // El Rate Limiter se ejecuta DESPUÉS de la autenticación para poder combinar
    // la IP real del cliente con la identidad del usuario autenticado.
    app.UseRateLimiter();
    app.UseAuthorization();
    app.MapControllers();

    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Sistema de Comité de Aula API")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    }).AllowAnonymous();


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
