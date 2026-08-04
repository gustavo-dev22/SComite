using FluentValidation;

namespace AulaComite.Application.Institucion.Commands
{
    public class GuardarInstitucionEducativaCommandValidator : AbstractValidator<GuardarInstitucionEducativaCommand>
    {
        public GuardarInstitucionEducativaCommandValidator()
        {
            RuleFor(x => x.NombreInstitucion).NotEmpty().MaximumLength(300)
                .WithMessage("El NombreInstitucion es obligatorio.");
            RuleFor(x => x.CorreoContacto).EmailAddress().When(x => !string.IsNullOrEmpty(x.CorreoContacto))
                .WithMessage("El CorreoContacto no tiene un formato válido.");
            RuleFor(x => x.UsuarioActualizacion).NotEmpty().MaximumLength(100)
                .WithMessage("El UsuarioActualizacion es obligatorio.");
        }
    }
}