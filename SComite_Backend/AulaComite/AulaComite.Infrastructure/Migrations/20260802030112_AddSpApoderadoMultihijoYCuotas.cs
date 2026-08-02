using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpApoderadoMultihijoYCuotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🚀 1. SP PARA OBTENER LOS HIJOS DEL APODERADO Y LOS DATOS DEL TESORERO DE SU AULA
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Apoderado_ObtenerHijos
                    @UsuarioApoderado VARCHAR(100),
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        e.Id AS EstudianteId,
                        (e.Nombres + ' ' + e.ApellidoPaterno + ' ' + ISNULL(e.ApellidoMaterno, '')) AS NombreEstudiante,
                        a.Id AS AulaId,
                        (a.Nivel + ' - ' + CAST(a.Grado AS VARCHAR(50)) + ' ""' + a.Seccion + '""') AS NombreAula,
                        a.Nivel,
                        CAST(a.Grado AS VARCHAR(50)) AS Grado,
                        a.Seccion,
                        -- Datos del Tesorero del Aula
                        ISNULL(ci.NombreCompleto, 'Tesorero de Aula') AS TesoreroNombre
                    FROM Estudiantes e
                    JOIN Aulas a ON a.Id = e.AulaId
                    JOIN PeriodosLectivos p ON p.Id = a.PeriodoId
                    LEFT JOIN ComiteIntegrantes ci ON ci.AulaId = a.Id 
                        AND UPPER(ci.Cargo) LIKE '%TESORERO%' 
                        AND ci.Estado = 1
                    WHERE (e.NombreApoderado = @UsuarioApoderado)
                      AND p.Anio = @AnioLectivo
                      AND e.Estado = 1
                    ORDER BY a.Nivel ASC, a.Grado ASC;
                END
            ");

            // 🚀 2. SP PARA OBTENER EL ESTADO DE CUENTAS DEL HIJO SELECCIONADO
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Apoderado_ObtenerCuotasPendientes
                    @EstudianteId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @AulaId INT;

                    SELECT TOP 1 @AulaId = a.Id
                    FROM Estudiantes e
                    JOIN Aulas a ON a.Id = e.AulaId
                    JOIN PeriodosLectivos p ON p.Id = a.PeriodoId
                    WHERE e.Id = @EstudianteId 
                      AND p.Anio = @AnioLectivo
                      AND e.Estado = 1;

                    IF @AulaId IS NULL RETURN;

                    SELECT 
                        c.Id AS CuotaId,
                        c.Concepto,
                        c.TipoCuota,
                        c.MontoIndividual AS MontoTotalCuota,
                        c.FechaVencimiento,
                        ISNULL(d.MontoPagado, 0) AS MontoPagado,
                        (c.MontoIndividual - ISNULL(d.MontoPagado, 0)) AS MontoPendiente,
                        ISNULL(d.EstadoPago, 'PENDIENTE') AS EstadoPago,
                        CASE 
                            WHEN ISNULL(d.EstadoPago, 'PENDIENTE') IN ('COMPLETO', 'PAGADO') THEN 'PAGADO'
                            WHEN c.FechaVencimiento < GETDATE() THEN 'VENCIDO'
                            ELSE 'PENDIENTE'
                        END AS EstadoVisual,
                        d.FechaUltimoPago
                    FROM Cuotas c
                    LEFT JOIN CuotaDetalleEstudiante d ON d.CuotaId = c.Id AND d.EstudianteId = @EstudianteId
                    WHERE c.AulaId = @AulaId
                      AND (c.Estado IS NULL OR CAST(c.Estado AS VARCHAR(50)) NOT IN ('INACTIVO', 'ELIMINADO', '0'))
                    ORDER BY c.FechaVencimiento ASC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Apoderado_ObtenerHijos;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Apoderado_ObtenerCuotasPendientes;");
        }
    }
}
