using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Anuncios.Commands
{
    public record GuardarAnuncioCommand(
        int Id,
        int AulaId,
        string Titulo,
        string Contenido,
        string Categoria,
        bool EsFijado,
        string? UrlAdjunto
    ) : IRequest<int>;
}
