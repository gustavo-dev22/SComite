using FluentValidation;

namespace AulaComite.Application.Apoderado.Commands
{
    public class RegistrarLecturaAnuncioCommandValidator : AbstractValidator<RegistrarLecturaAnuncioCommand>
    {
        public RegistrarLecturaAnuncioCommandValidator()
        {
            RuleFor(x => x.AnuncioId).GreaterThan(0).WithMessage("El AnuncioId es obligatorio.");
            RuleFor(x => x.EstudianteId).GreaterThan(0).WithMessage("El EstudianteId es obligatorio.");
        }
    }
}