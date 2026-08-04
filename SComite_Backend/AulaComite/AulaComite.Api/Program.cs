using AulaComite.Api.Middlewares;
using AulaComite.Application;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Infrastructure;
using AulaComite.Infrastructure.Persistence;
using AulaComite.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Text;

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsAngularPolicy", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();

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
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.UseStaticFiles();

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
