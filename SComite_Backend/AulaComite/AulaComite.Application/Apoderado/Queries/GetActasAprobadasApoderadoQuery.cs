using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Apoderado.Dtos;
using MediatR;

namespace AulaComite.Application.Apoderado.Queries
{
    public record GetActasAprobadasApoderadoQuery(int EstudianteId, int AnioLectivo)
    : IRequest<List<ActaApoderadoDto>>;
}
