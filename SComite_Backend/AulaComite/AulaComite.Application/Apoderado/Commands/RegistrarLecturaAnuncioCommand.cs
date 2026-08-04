using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Apoderado.Commands
{
    public record RegistrarLecturaAnuncioCommand(int AnuncioId, int EstudianteId, int AnioLectivo) : IRequest<bool>;
}
