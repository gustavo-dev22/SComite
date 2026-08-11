using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Actividades.Dtos
{
    public record ActividadComiteDto(
        int Id,
        int AulaId,
        string NombreActividad,
        string? Descripcion,
        DateTime FechaProgramada,
        decimal MontoPresupuestado,
        decimal CuotaSugeridaPorAlumno,
        string Estado,
        DateTime FechaRegistro
    );
}
