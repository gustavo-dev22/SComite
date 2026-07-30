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

            // 🚀 Registrar DbContext para Entity Framework (Migraciones)
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            // 🚀 Registrar DbConnectionFactory para Dapper
            services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

            services.AddScoped<IUserContextService, UserContextService>();
            services.AddScoped<IAulaRepository, AulaRepository>();
            services.AddScoped<IComiteRepository, ComiteRepository>();
            services.AddScoped<IEstudianteRepository, EstudianteRepository>();
            services.AddScoped<IPeriodoRepository, PeriodoRepository>();
            services.AddScoped<ILogRepository, LogRepository>();
            services.AddScoped<ICuotaRepository, CuotaRepository>();
            services.AddScoped<IGastoRepository, GastoRepository>();
            services.AddScoped<IBalanceRepository, BalanceRepository>();

            return services;
        }
    }
}
