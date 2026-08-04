using FluentValidation;

namespace AulaComite.Application.Estudiantes.Commands
{
    public class UpdateEstudianteCommandValidator : AbstractValidator<UpdateEstudianteCommand>
    {
        public UpdateEstudianteCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("El Id es obligatorio.");
            RuleFor(x => x.AulaId).GreaterThan(0).WithMessage("El AulaId es obligatorio.");
            RuleFor(x => x.TipoDocumento).NotEmpty().MaximumLength(20)
                .WithMessage("El TipoDocumento es obligatorio.");
            RuleFor(x => x.NumeroDocumento).NotEmpty().MaximumLength(20)
                .WithMessage("El NumeroDocumento es obligatorio.");
            RuleFor(x => x.Nombres).NotEmpty().MaximumLength(100)
                .WithMessage("Los Nombres son obligatorios.");
            RuleFor(x => x.ApellidoPaterno).NotEmpty().MaximumLength(80)
                .WithMessage("El ApellidoPaterno es obligatorio.");
            RuleFor(x => x.ApellidoMaterno).NotEmpty().MaximumLength(80)
                .WithMessage("El ApellidoMaterno es obligatorio.");
        }
    }
}