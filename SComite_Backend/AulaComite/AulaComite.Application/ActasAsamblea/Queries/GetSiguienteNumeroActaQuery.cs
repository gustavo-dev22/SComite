using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.ActasAsamblea.Queries
{
    public record GetSiguienteNumeroActaQuery(int AulaId, int AnioLectivo) : IRequest<string>;
}
