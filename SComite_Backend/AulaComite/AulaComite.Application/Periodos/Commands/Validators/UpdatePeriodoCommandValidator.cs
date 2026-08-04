using FluentValidation;

namespace AulaComite.Application.Periodos.Commands
{
    public class UpdatePeriodoCommandValidator : AbstractValidator<UpdatePeriodoCommand>
    {
        public UpdatePeriodoCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("El Id es obligatorio.");
            RuleFor(x => x.Anio).GreaterThan(2000).LessThan(2100)
                .WithMessage("El Año debe estar entre 2000 y 2100.");
            RuleFor(x => x.FechaInicio).NotEmpty().WithMessage("La FechaInicio es obligatoria.");
            RuleFor(x => x.FechaFin).NotEmpty().WithMessage("La FechaFin es obligatoria.");
            RuleFor(x => x.FechaFin).GreaterThanOrEqualTo(x => x.FechaInicio)
                .WithMessage("La FechaFin no puede ser anterior a la FechaInicio.");
        }
    }
}