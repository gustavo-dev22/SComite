using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.ActasAsamblea.Commands
{
    public record GuardarActaCommand(
        int Id,
        int AulaId,
        string NumeroActa,
        string Titulo,
        DateTime FechaReunion,
        string AgendaAcuerdos,
        string EstadoActa,
        string? UrlDocumentoPdf,
        string UsuarioRegistro
    ) : IRequest<int>;
}
