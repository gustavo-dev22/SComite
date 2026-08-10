using AulaComite.Application.Common.Interfaces;
using AulaComite.Infrastructure.Persistence;
using AulaComite.Infrastructure.Repositories;
using AulaComite.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace AulaComite.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // 🚀 Registrar DbContext para Entity Framework. SOLO se usa para generar/aplicar
            // migraciones (design-time y arranque en Desarrollo). Toda la lectura/escritura
            // de datos en tiempo de ejecución se realiza con Dapper vía IDbConnectionFactory.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    sqlOptions =>
                    {
                        // 🛡️ Resiliencia: reintenta operaciones ante caídas temporales de conexión
                        // (enmascaradas por el proveedor de SQL Server) en lugar de fallar de inmediato.
                        sqlOptions.EnableRetryOnFailure();
                    }));

            // 🚀 Registrar DbConnectionFactory para Dapper
            services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

            services.AddScoped<IUserContextService, UserContextService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IAulaRepository, AulaRepository>();
            services.AddScoped<IComiteRepository, ComiteRepository>();
            services.AddScoped<IEstudianteRepository, EstudianteRepository>();
            services.AddScoped<IPeriodoRepository, PeriodoRepository>();
            services.AddScoped<ILogRepository, LogRepository>();
            services.AddScoped<ICuotaRepository, CuotaRepository>();
            services.AddScoped<IGastoRepository, GastoRepository>();
            services.AddScoped<IBalanceRepository, BalanceRepository>();
            services.AddScoped<IActividadRepository, ActividadRepository>();
            services.AddScoped<ISistemaRepository, SistemaRepository>();
            services.AddScoped<IDonacionRepository, DonacionRepository>();
            services.AddScoped<IAnuncioRepository, AnuncioRepository>();
            services.AddScoped<IActaAsambleaRepository, ActaAsambleaRepository>();
            services.AddScoped<IInstitucionRepository, InstitucionRepository>();
            services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
            services.AddScoped<IApoderadoRepository, ApoderadoRepository>();
            services.AddScoped<ITransparenciaRepository, TransparenciaRepository>();

            return services;
        }
    }
}
