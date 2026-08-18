using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Estudiantes.Dtos;
using MediatR;

namespace AulaComite.Application.Estudiantes.Commands
{
    public record MigrarEstudiantesCommand(
        int AulaDestinoId,
        List<int> EstudianteIds
    ) : IRequest<ResultadoMigracionDto>;
}
