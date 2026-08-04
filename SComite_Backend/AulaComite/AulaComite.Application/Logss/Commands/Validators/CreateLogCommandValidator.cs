using FluentValidation;

namespace AulaComite.Application.Logss.Commands
{
    public class CreateLogCommandValidator : AbstractValidator<CreateLogCommand>
    {
        public CreateLogCommandValidator()
        {
            RuleFor(x => x.Nivel).NotEmpty().MaximumLength(20)
                .WithMessage("El Nivel es obligatorio.");
            RuleFor(x => x.Modulo).NotEmpty().MaximumLength(50)
                .WithMessage("El Modulo es obligatorio.");
            RuleFor(x => x.Accion).NotEmpty().MaximumLength(200)
                .WithMessage("La Accion es obligatoria.");
            RuleFor(x => x.Mensaje).NotEmpty().MaximumLength(2000)
                .WithMessage("El Mensaje es obligatorio.");
        }
    }
}