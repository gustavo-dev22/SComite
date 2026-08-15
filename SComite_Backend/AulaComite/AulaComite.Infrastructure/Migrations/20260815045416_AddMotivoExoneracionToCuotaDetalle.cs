using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMotivoExoneracionToCuotaDetalle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MotivoExoneracion",
                table: "CuotaDetalleEstudiante",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaModificacionEstado",
                table: "CuotaDetalleEstudiante",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MotivoExoneracion",
                table: "CuotaDetalleEstudiante");

            migrationBuilder.DropColumn(
                name: "FechaModificacionEstado",
                table: "CuotaDetalleEstudiante");
        }
    }
}
