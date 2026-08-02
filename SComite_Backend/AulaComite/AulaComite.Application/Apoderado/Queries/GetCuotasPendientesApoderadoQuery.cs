using AulaComite.Application.Apoderado.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Apoderado.Queries
{
    public record GetCuotasPendientesApoderadoQuery(int EstudianteId, int AnioLectivo) : IRequest<ResumenPagosApoderadoDto>;
}
