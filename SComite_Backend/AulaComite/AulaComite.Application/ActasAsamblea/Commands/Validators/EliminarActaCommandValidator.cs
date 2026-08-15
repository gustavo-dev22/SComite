using FluentValidation;

namespace AulaComite.Application.ActasAsamblea.Commands
{
    public class EliminarActaCommandValidator : AbstractValidator<EliminarActaCommand>
    {
        public EliminarActaCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("El Id del acta es obligatorio.");
            RuleFor(x => x.AulaId).GreaterThan(0)
                .WithMessage("El AulaId es obligatorio.");
        }
    }
}