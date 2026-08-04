using FluentValidation;

namespace AulaComite.Application.Actividades.Commands
{
    public class GuardarActividadCommandValidator : AbstractValidator<GuardarActividadCommand>
    {
        public GuardarActividadCommandValidator()
        {
            RuleFor(x => x.AulaId).GreaterThan(0).WithMessage("El AulaId es obligatorio.");
            RuleFor(x => x.NombreActividad).NotEmpty().MaximumLength(200)
                .WithMessage("El NombreActividad es obligatorio.");
            RuleFor(x => x.FechaProgramada).NotEmpty().WithMessage("La FechaProgramada es obligatoria.");
            RuleFor(x => x.MontoPresupuestado).GreaterThanOrEqualTo(0)
                .WithMessage("El MontoPresupuestado no puede ser negativo.");
            RuleFor(x => x.CuotaSugeridaPorAlumno).GreaterThanOrEqualTo(0)
                .WithMessage("La CuotaSugeridaPorAlumno no puede ser negativa.");
            RuleFor(x => x.Estado).NotEmpty().MaximumLength(30)
                .WithMessage("El Estado es obligatorio.");
        }
    }
}