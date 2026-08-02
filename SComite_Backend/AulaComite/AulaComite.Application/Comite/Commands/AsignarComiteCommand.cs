using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Comite.Commands
{
    public record AsignarComiteCommand(
        int AulaId,
        string UsuarioIdSasi,
        string NombreCompleto,
        string Email,
        string? Celular,
        string Cargo
    ) : IRequest<int>;
}
