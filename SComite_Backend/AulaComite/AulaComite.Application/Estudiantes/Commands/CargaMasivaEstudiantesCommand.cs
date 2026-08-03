using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Estudiantes.Dtos;
using MediatR;

namespace AulaComite.Application.Estudiantes.Commands
{
    public record CargaMasivaEstudiantesCommand(int AulaId, List<EstudianteImportacionItemDto> Estudiantes)
    : IRequest<CargaMasivaResultadoDto>;
}
