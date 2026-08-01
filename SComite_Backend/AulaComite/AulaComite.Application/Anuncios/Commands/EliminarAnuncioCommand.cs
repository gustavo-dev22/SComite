using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Anuncios.Commands
{
    public record EliminarAnuncioCommand(int Id, int AulaId) : IRequest<bool>;
}
