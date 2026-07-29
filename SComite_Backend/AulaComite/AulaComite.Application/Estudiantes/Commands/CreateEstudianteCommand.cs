using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Estudiantes.Commands
{
    public record CreateEstudianteCommand(
        int AulaId,
        string TipoDocumento,
        string NumeroDocumento,
        string Nombres,
        string ApellidoPaterno,
        string ApellidoMaterno,
        string? UsuarioIdApoderadoSasi,
        string? NombreApoderado,
        string? TelefonoApoderado
    ) : IRequest<int>;
}
