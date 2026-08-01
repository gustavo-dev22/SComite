using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Actividades.Commands
{
    public record GuardarActividadCommand(
        int Id,
        int AulaId,
        string NombreActividad,
        string? Descripcion,
        DateTime FechaProgramada,
        decimal MontoPresupuestado,
        decimal CuotaSugeridaPorAlumno,
        string Estado
    ) : IRequest<int>;
}
