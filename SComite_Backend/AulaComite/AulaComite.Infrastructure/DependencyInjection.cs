using AulaComite.Application.Common.Interfaces;
using AulaComite.Infrastructure.Persistence;
using AulaComite.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AulaComite.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Registro de la fábrica de conexión para SQL Server
            services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

            services.AddScoped<IAulaRepository, AulaRepository>();

            return services;
        }
    }
}
