using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Institucion.Commands
{
    public record GuardarInstitucionEducativaCommand(
        string NombreInstitucion,
        string? CodigoModular,
        string? LemaInstitucional,
        string? NombreDirector,
        string? Direccion,
        string? Telefono,
        string? CorreoContacto,
        string? UrlLogo
    ) : IRequest<bool>;
}
