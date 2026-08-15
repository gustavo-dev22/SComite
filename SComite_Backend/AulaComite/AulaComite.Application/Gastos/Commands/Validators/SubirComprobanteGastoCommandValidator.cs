using FluentValidation;

namespace AulaComite.Application.Gastos.Commands
{
    public class SubirComprobanteGastoCommandValidator : AbstractValidator<SubirComprobanteGastoCommand>
    {
        public SubirComprobanteGastoCommandValidator()
        {
            RuleFor(x => x.ContenidoArchivo).NotNull()
                .WithMessage("El archivo del comprobante es obligatorio.");
            RuleFor(x => x.NombreArchivo).NotEmpty().MaximumLength(255)
                .WithMessage("El nombre del archivo es obligatorio.");
        }
    }
}