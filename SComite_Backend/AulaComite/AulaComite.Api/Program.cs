using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using AulaComite.Api.Middlewares;
using AulaComite.Api.Services;
using AulaComite.Application;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Infrastructure;
using AulaComite.Infrastructure.Persistence;
using AulaComite.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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

        options.AddPolicy("Administrador", policy =>
            policy.RequireAuthenticatedUser().RequireRole("Administrador"));

        options.AddPolicy("ComiteAula", policy =>
            policy.RequireAuthenticatedUser().RequireRole("Comité de Aula"));

        options.AddPolicy("ManejoFinanciero", policy =>
            policy.RequireAuthenticatedUser().RequireRole("Administrador", "Comité de Aula"));

        options.AddPolicy("GestionEscolar", policy =>
            policy.RequireAuthenticatedUser().RequireRole("Administrador", "Comité de Aula"));

        options.AddPolicy("AccesoApoderado", policy =>
            policy.RequireAuthenticatedUser().RequireRole("Administrador", "Apoderado"));
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // Limitar intentos de inicio de sesión por IP para mitigar ataques de fuerza bruta.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("LoginLimiter", httpContext =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
        });
    });

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

    // Auditoría automática de peticiones HTTP en consola y logs
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("Sistema de Comité de Aula API")
                .WithTheme(ScalarTheme.DeepSpace)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("CorsAngularPolicy");
    app.UseStaticFiles();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    

    // Aplicar Migraciones Automáticas en el arranque SOLO en Desarrollo.
    // En Producción las migraciones se aplican vía CI/CD o herramientas dedicadas.
    if (app.Environment.IsDevelopment())
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                context.Database.Migrate();
                Log.Information("Migraciones de la base de datos verificadas/aplicadas correctamente.");
            }
            catch (Exception ex)
            {
                Log.Warning("Atención al verificar migraciones: {Message}", ex.Message);
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
