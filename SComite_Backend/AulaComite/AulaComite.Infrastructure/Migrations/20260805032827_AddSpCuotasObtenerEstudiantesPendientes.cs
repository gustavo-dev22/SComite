using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpCuotasObtenerEstudiantesPendientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Cuotas_ObtenerEstudiantesPendientes]
                    @CuotaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        cde.EstudianteId,
                        e.TipoDocumento,
                        e.NumeroDocumento,
                        (e.ApellidoPaterno + ' ' + e.ApellidoMaterno + ', ' + e.Nombres) AS NombreEstudiante,
                        ISNULL(e.NombreApoderado, 'Sin Apoderado Asignado') AS NombreApoderado,
                        ISNULL(e.TelefonoApoderado, '-') AS TelefonoApoderado,
                        cde.MontoAsignado AS MontoTotalCuota,
                        ISNULL(cde.MontoPagado, 0) AS MontoAbonado,
                        (cde.MontoAsignado - ISNULL(cde.MontoPagado, 0)) AS MontoPendiente,
                        ISNULL(cde.EstadoPago, 'PENDIENTE') AS EstadoPago
                    FROM CuotaDetalleEstudiante cde
                    JOIN Estudiantes e ON e.Id = cde.EstudianteId
                    WHERE cde.CuotaId = @CuotaId
                      AND UPPER(ISNULL(cde.EstadoPago, 'PENDIENTE')) IN ('PENDIENTE', 'PARCIAL')
                      AND e.Estado = 1
                    ORDER BY e.ApellidoPaterno ASC, e.ApellidoMaterno ASC, e.Nombres ASC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_Cuotas_ObtenerEstudiantesPendientes];");
        }
    }
}
