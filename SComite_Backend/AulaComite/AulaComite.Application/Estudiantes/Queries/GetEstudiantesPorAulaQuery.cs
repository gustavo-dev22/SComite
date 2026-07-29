using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.Estudiantes.Queries
{
    public record GetEstudiantesPorAulaQuery(int AulaId) : IRequest<IEnumerable<Estudiante>>;
}
