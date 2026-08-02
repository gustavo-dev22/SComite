using AulaComite.Application.Apoderado.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Apoderado.Queries
{
    public record GetAnunciosMuroApoderadoQuery(int EstudianteId, int AnioLectivo) : IRequest<List<AnuncioApoderadoDto>>;
}
