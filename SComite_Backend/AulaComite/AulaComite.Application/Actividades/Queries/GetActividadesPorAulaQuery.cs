using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Actividades.Dtos;
using MediatR;

namespace AulaComite.Application.Actividades.Queries
{
    public record GetActividadesPorAulaQuery(int AulaId, int AnioLectivo) : IRequest<IEnumerable<ActividadComiteDto>>;
}
