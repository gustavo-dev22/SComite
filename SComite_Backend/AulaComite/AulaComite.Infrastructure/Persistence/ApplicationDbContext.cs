using AulaComite.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<PeriodoLectivo> PeriodosLectivos => Set<PeriodoLectivo>();
        public DbSet<Aula> Aulas => Set<Aula>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de la entidad PeriodoLectivo
            modelBuilder.Entity<PeriodoLectivo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Anio).IsUnique();
                entity.Property(e => e.Nombre).HasMaxLength(50).IsRequired();
            });

            // Configuración de la entidad Aula
            modelBuilder.Entity<Aula>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nivel).HasMaxLength(30).IsRequired();
                entity.Property(e => e.Grado).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Seccion).HasMaxLength(10).IsRequired();
                entity.Ignore(e => e.AnioPeriodo);
            });
        }
    }
}
