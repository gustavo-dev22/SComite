using FluentValidation;

namespace AulaComite.Application.ActasAsamblea.Commands
{
    public class GuardarActaCommandValidator : AbstractValidator<GuardarActaCommand>
    {
        public GuardarActaCommandValidator()
        {
            RuleFor(x => x.AulaId).GreaterThan(0).WithMessage("El AulaId es obligatorio.");
            RuleFor(x => x.NumeroActa).NotEmpty().MaximumLength(50)
                .WithMessage("El NumeroActa es obligatorio.");
            RuleFor(x => x.Titulo).NotEmpty().MaximumLength(200)
                .WithMessage("El Titulo es obligatorio.");
            RuleFor(x => x.FechaReunion).NotEmpty().WithMessage("La FechaReunion es obligatoria.");
            RuleFor(x => x.AgendaAcuerdos).NotEmpty().MaximumLength(5000)
                .WithMessage("La AgendaAcuerdos es obligatoria.");
            RuleFor(x => x.EstadoActa).NotEmpty().MaximumLength(30)
                .WithMessage("El EstadoActa es obligatorio.");
            RuleFor(x => x.UsuarioRegistro).NotEmpty().MaximumLength(100)
                .WithMessage("El UsuarioRegistro es obligatorio.");
        }
    }
}