using FluentValidation;

namespace AulaComite.Application.Cuotas.Commands
{
    public class GenerarCuotasMensualesCommandValidator : AbstractValidator<GenerarCuotasMensualesCommand>
    {
        public GenerarCuotasMensualesCommandValidator()
        {
            RuleFor(x => x.AulaId).GreaterThan(0)
                .WithMessage("El AulaId es obligatorio.");
            RuleFor(x => x.ConceptoBase).NotEmpty().MaximumLength(200)
                .WithMessage("El ConceptoBase es obligatorio.");
            RuleFor(x => x.MontoMensual).GreaterThan(0).LessThanOrEqualTo(100000)
                .WithMessage("El MontoMensual debe ser mayor a 0 y menor o igual a 100000.");
            RuleFor(x => x.MesInicio).InclusiveBetween(1, 12)
                .WithMessage("El MesInicio debe estar entre 1 y 12.");
            RuleFor(x => x.DiaVencimiento).InclusiveBetween(1, 31)
                .WithMessage("El DiaVencimiento debe estar entre 1 y 31.");
            RuleFor(x => x.AnioLectivo).InclusiveBetween(2000, 2100)
                .WithMessage("El AnioLectivo no es válido.");
        }
    }
}