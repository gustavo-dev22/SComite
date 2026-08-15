using FluentValidation;

namespace AulaComite.Application.Estudiantes.Commands
{
    public class DeleteEstudianteCommandValidator : AbstractValidator<DeleteEstudianteCommand>
    {
        public DeleteEstudianteCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("El Id del estudiante es obligatorio.");
        }
    }
}