using AulaComite.Application.Estudiantes.Dtos;
using FluentValidation;

namespace AulaComite.Application.Estudiantes.Commands
{
    public class CargaMasivaEstudiantesCommandValidator : AbstractValidator<CargaMasivaEstudiantesCommand>
    {
        public CargaMasivaEstudiantesCommandValidator()
        {
            RuleFor(x => x.AulaId).GreaterThan(0).WithMessage("El AulaId es obligatorio.");
            RuleFor(x => x.Estudiantes).NotNull().NotEmpty()
                .WithMessage("Debe enviar al menos un estudiante.");

            RuleForEach(x => x.Estudiantes).SetValidator(new EstudianteImportacionItemValidator());
        }

        private class EstudianteImportacionItemValidator : AbstractValidator<EstudianteImportacionItemDto>
        {
            public EstudianteImportacionItemValidator()
            {
                RuleFor(x => x.NumeroDocumento).NotEmpty().MaximumLength(20)
                    .WithMessage("El NumeroDocumento es obligatorio.");
                RuleFor(x => x.Nombres).NotEmpty().MaximumLength(100)
                    .WithMessage("Los Nombres son obligatorios.");
                RuleFor(x => x.ApellidoPaterno).NotEmpty().MaximumLength(80)
                    .WithMessage("El ApellidoPaterno es obligatorio.");
            }
        }
    }
}