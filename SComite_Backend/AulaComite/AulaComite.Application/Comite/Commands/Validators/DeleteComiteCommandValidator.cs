using FluentValidation;

namespace AulaComite.Application.Comite.Commands
{
    public class DeleteComiteCommandValidator : AbstractValidator<DeleteComiteCommand>
    {
        public DeleteComiteCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("El Id del comité es obligatorio.");
        }
    }
}