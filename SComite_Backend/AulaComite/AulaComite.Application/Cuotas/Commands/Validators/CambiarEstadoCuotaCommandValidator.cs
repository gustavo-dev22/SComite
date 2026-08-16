using FluentValidation;

namespace AulaComite.Application.Cuotas.Commands
{
    public class CambiarEstadoCuotaCommandValidator : AbstractValidator<CambiarEstadoCuotaCommand>
    {
        // 🛡️ T3: Los únicos estados transitables contablemente son EN COBRO y CERRADA.
        // 'ANULADA' no se admite por este flujo: la anulación contable se gestiona
        // mediante el endpoint específico de pagos (sp_Cuotas_AnularPagoEstudiante).
        private static readonly string[] EstadosValidos = { "EN COBRO", "CERRADA" };

        public CambiarEstadoCuotaCommandValidator()
        {
            RuleFor(x => x.CuotaId).GreaterThan(0)
                .WithMessage("El CuotaId es obligatorio.");
            RuleFor(x => x.NuevoEstado).NotEmpty()
                .Must(e => EstadosValidos.Contains(e, StringComparer.OrdinalIgnoreCase))
                .WithMessage("El NuevoEstado debe ser 'EN COBRO' o 'CERRADA'.");
        }
    }
}