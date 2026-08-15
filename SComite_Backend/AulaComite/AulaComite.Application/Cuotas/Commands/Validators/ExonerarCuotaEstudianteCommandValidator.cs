using FluentValidation;

namespace AulaComite.Application.Cuotas.Commands
{
    public class ExonerarCuotaEstudianteCommandValidator : AbstractValidator<ExonerarCuotaEstudianteCommand>
    {
        public ExonerarCuotaEstudianteCommandValidator()
        {
            RuleFor(x => x.CuotaDetalleId).GreaterThan(0)
                .WithMessage("El CuotaDetalleId es obligatorio.");
            RuleFor(x => x.NuevoEstado).NotEmpty()
                .Must(e => e == "EXONERADO" || e == "PENDIENTE")
                .WithMessage("El NuevoEstado debe ser 'EXONERADO' o 'PENDIENTE'.");
            RuleFor(x => x.MotivoExoneracion).NotEmpty().MaximumLength(500)
                .When(x => x.NuevoEstado == "EXONERADO")
                .WithMessage("El MotivoExoneracion es obligatorio al exonerar.");
        }
    }
}