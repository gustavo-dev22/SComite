using FluentValidation;

namespace AulaComite.Application.Cuotas.Commands
{
    public class CreateCuotaCommandValidator : AbstractValidator<CreateCuotaCommand>
    {
        public CreateCuotaCommandValidator()
        {
            RuleFor(x => x.AulaId).GreaterThan(0).WithMessage("El AulaId es obligatorio.");
            RuleFor(x => x.Concepto).NotEmpty().MaximumLength(200)
                .WithMessage("El Concepto es obligatorio.");
            RuleFor(x => x.MontoIndividual).GreaterThan(0)
                .WithMessage("El MontoIndividual debe ser mayor a 0.");
            RuleFor(x => x.FechaVencimiento).NotEmpty()
                .WithMessage("La FechaVencimiento es obligatoria.");
        }
    }
}