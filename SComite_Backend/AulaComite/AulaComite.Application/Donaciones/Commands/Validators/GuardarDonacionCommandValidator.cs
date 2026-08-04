using FluentValidation;

namespace AulaComite.Application.Donaciones.Commands
{
    public class GuardarDonacionCommandValidator : AbstractValidator<GuardarDonacionCommand>
    {
        public GuardarDonacionCommandValidator()
        {
            RuleFor(x => x.AulaId).GreaterThan(0).WithMessage("El AulaId es obligatorio.");
            RuleFor(x => x.Donante).NotEmpty().MaximumLength(200)
                .WithMessage("El Donante es obligatorio.");
            RuleFor(x => x.Monto).GreaterThan(0).WithMessage("El Monto debe ser mayor a 0.");
            RuleFor(x => x.FechaDonacion).NotEmpty().WithMessage("La FechaDonacion es obligatoria.");
            RuleFor(x => x.Concepto).NotEmpty().MaximumLength(200)
                .WithMessage("El Concepto es obligatorio.");
        }
    }
}