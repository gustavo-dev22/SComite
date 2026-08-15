using FluentValidation;

namespace AulaComite.Application.Sistema.Commands
{
    public class ResetBaseDeDatosCommandValidator : AbstractValidator<ResetBaseDeDatosCommand>
    {
        public ResetBaseDeDatosCommandValidator()
        {
            RuleFor(x => x.ConfirmacionTexto).NotEmpty()
                .Equal("ELIMINAR TODO")
                .WithMessage("El texto de confirmación debe ser 'ELIMINAR TODO'.");
        }
    }
}