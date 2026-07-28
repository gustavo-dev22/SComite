using AulaComite.Api.Middlewares;
using AulaComite.Application;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Infrastructure;
using AulaComite.Infrastructure.Persistence;
using AulaComite.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    var secretKey = jwtSettings["SecretKey"] ?? "Tu_Clave_Super_Segura_Y_Secreta_De_AulaComite_2026_123456";

    builder.Services.AddHttpClient<ISasiAuthService, SasiAuthService>(client =>
    {
        var baseUrl = builder.Configuration["SasiSettings:BaseUrl"] ?? "https://localhost:44337/SASI/api/";
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
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsAngularPolicy", policy =>
        {
            policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200", "http://localhost:4202")
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
        app.MapOpenApi();
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

    // Aplicar Migraciones Automáticas en el arranque
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
            Log.Error(ex, "Ocurrió un error al aplicar las migraciones en la base de datos.");
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
