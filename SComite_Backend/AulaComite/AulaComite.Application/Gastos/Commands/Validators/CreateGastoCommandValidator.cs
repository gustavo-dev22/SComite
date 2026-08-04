using FluentValidation;

namespace AulaComite.Application.Gastos.Commands
{
    public class CreateGastoCommandValidator : AbstractValidator<CreateGastoCommand>
    {
        public CreateGastoCommandValidator()
        {
            RuleFor(x => x.AulaId).GreaterThan(0).WithMessage("El AulaId es obligatorio.");
            RuleFor(x => x.Concepto).NotEmpty().MaximumLength(200)
                .WithMessage("El Concepto es obligatorio.");
            RuleFor(x => x.Categoria).NotEmpty().MaximumLength(100)
                .WithMessage("La Categoria es obligatoria.");
            RuleFor(x => x.Monto).GreaterThan(0).WithMessage("El Monto debe ser mayor a 0.");
            RuleFor(x => x.FechaGasto).NotEmpty().WithMessage("La FechaGasto es obligatoria.");
            RuleFor(x => x.TipoComprobante).NotEmpty().MaximumLength(30)
                .WithMessage("El TipoComprobante es obligatorio.");
        }
    }
}