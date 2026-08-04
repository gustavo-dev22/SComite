using FluentValidation;

namespace AulaComite.Application.Aulas.Commands
{
    public class CreateAulaCommandValidator : AbstractValidator<CreateAulaCommand>
    {
        public CreateAulaCommandValidator()
        {
            RuleFor(x => x.PeriodoId).GreaterThan(0).WithMessage("El PeriodoId es obligatorio.");
            RuleFor(x => x.Nivel).NotEmpty().MaximumLength(30).WithMessage("El Nivel es obligatorio.");
            RuleFor(x => x.Grado).NotEmpty().MaximumLength(50).WithMessage("El Grado es obligatorio.");
            RuleFor(x => x.Seccion).NotEmpty().MaximumLength(10).WithMessage("La Seccion es obligatoria.");
        }
    }
}