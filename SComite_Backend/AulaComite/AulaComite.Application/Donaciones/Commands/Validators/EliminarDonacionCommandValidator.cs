using FluentValidation;

namespace AulaComite.Application.Donaciones.Commands
{
    public class EliminarDonacionCommandValidator : AbstractValidator<EliminarDonacionCommand>
    {
        public EliminarDonacionCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("El Id de la donación es obligatorio.");
            RuleFor(x => x.AulaId).GreaterThan(0)
                .WithMessage("El AulaId es obligatorio.");
        }
    }
}