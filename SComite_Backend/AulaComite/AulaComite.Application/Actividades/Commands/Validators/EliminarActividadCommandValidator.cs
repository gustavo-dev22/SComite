using FluentValidation;

namespace AulaComite.Application.Actividades.Commands
{
    public class EliminarActividadCommandValidator : AbstractValidator<EliminarActividadCommand>
    {
        public EliminarActividadCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("El Id de la actividad es obligatorio.");
            RuleFor(x => x.AulaId).GreaterThan(0)
                .WithMessage("El AulaId es obligatorio.");
        }
    }
}