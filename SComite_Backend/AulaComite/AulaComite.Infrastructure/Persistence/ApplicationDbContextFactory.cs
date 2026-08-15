using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Infrastructure.Persistence
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // 1. Construir la configuración buscando el appsettings.json de la API
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../AulaComite.Api");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // 2. Obtener la cadena de conexión
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // 3. Configurar DbContextOptions. EnableRetryOnFailure habilita resiliencia
            // ante caídas temporales de conexión durante la aplicación de migraciones en arranque.
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.EnableRetryOnFailure());

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
