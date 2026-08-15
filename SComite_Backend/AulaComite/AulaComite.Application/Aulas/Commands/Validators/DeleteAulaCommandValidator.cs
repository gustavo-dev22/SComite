using FluentValidation;

namespace AulaComite.Application.Aulas.Commands
{
    public class DeleteAulaCommandValidator : AbstractValidator<DeleteAulaCommand>
    {
        public DeleteAulaCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("El Id del aula es obligatorio.");
        }
    }
}