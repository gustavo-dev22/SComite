using AulaComite.Application.Apoderado.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Apoderado.Queries
{
    public record GetHijosApoderadoQuery(int AnioLectivo) : IRequest<List<HijoApoderadoDto>>;
}
