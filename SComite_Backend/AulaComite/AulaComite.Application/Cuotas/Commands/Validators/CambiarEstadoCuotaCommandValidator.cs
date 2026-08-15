using FluentValidation;

namespace AulaComite.Application.Cuotas.Commands
{
    public class CambiarEstadoCuotaCommandValidator : AbstractValidator<CambiarEstadoCuotaCommand>
    {
        private static readonly string[] EstadosValidos = { "EN COBRO", "CERRADA", "ANULADA" };

        public CambiarEstadoCuotaCommandValidator()
        {
            RuleFor(x => x.CuotaId).GreaterThan(0)
                .WithMessage("El CuotaId es obligatorio.");
            RuleFor(x => x.NuevoEstado).NotEmpty()
                .Must(e => EstadosValidos.Contains(e, StringComparer.OrdinalIgnoreCase))
                .WithMessage("El NuevoEstado debe ser 'EN COBRO', 'CERRADA' o 'ANULADA'.");
        }
    }
}