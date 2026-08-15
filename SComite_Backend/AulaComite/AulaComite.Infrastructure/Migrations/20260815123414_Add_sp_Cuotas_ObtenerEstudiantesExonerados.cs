using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_sp_Cuotas_ObtenerEstudiantesExonerados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- 1. SP para listar los estudiantes exonerados de una cuota específica
                CREATE OR ALTER PROCEDURE [dbo].[sp_Cuotas_ObtenerEstudiantesExonerados]
                    @CuotaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        cde.Id AS CuotaDetalleId,
                        cde.EstudianteId,
                        e.TipoDocumento,
                        e.NumeroDocumento,
                        (e.ApellidoPaterno + ' ' + e.ApellidoMaterno + ', ' + e.Nombres) AS NombreEstudiante,
                        ISNULL(e.NombreApoderado, 'Sin Apoderado Asignado') AS NombreApoderado,
                        ISNULL(e.TelefonoApoderado, '-') AS TelefonoApoderado,
                        cde.MontoAsignado AS MontoAsignado,
                        ISNULL(cde.MotivoExoneracion, 'Sin motivo especificado') AS MotivoExoneracion,
                        cde.FechaModificacionEstado AS FechaExoneracion
                    FROM CuotaDetalleEstudiante cde
                    JOIN Estudiantes e ON e.Id = cde.EstudianteId
                    WHERE cde.CuotaId = @CuotaId
                      AND UPPER(ISNULL(cde.EstadoPago, 'PENDIENTE')) = 'EXONERADO'
                      AND e.Estado = 1
                    ORDER BY cde.FechaModificacionEstado DESC;
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_Cuotas_ObtenerEstudiantesExonerados')
                    DROP PROCEDURE sp_Cuotas_ObtenerEstudiantesExonerados;
            ");
        }
    }
}
