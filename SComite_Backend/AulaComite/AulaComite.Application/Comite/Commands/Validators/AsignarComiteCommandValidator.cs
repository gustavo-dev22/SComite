using FluentValidation;

namespace AulaComite.Application.Comite.Commands
{
    public class AsignarComiteCommandValidator : AbstractValidator<AsignarComiteCommand>
    {
        public AsignarComiteCommandValidator()
        {
            RuleFor(x => x.AulaId).GreaterThan(0).WithMessage("El AulaId es obligatorio.");
            RuleFor(x => x.UsuarioIdSasi).NotEmpty().MaximumLength(100)
                .WithMessage("El UsuarioIdSasi es obligatorio.");
            RuleFor(x => x.NombreCompleto).NotEmpty().MaximumLength(200)
                .WithMessage("El NombreCompleto es obligatorio.");
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("El Email no tiene un formato válido.");
            RuleFor(x => x.Cargo).NotEmpty().MaximumLength(100)
                .WithMessage("El Cargo es obligatorio.");
        }
    }
}