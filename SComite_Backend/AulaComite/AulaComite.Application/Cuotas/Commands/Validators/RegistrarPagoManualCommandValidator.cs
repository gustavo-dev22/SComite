using FluentValidation;

namespace AulaComite.Application.Cuotas.Commands
{
    public class RegistrarPagoManualCommandValidator : AbstractValidator<RegistrarPagoManualCommand>
    {
        public RegistrarPagoManualCommandValidator()
        {
            RuleFor(x => x.CuotaDetalleId).GreaterThan(0)
                .WithMessage("El CuotaDetalleId es obligatorio.");
            RuleFor(x => x.MontoAbonado).GreaterThan(0)
                .WithMessage("El MontoAbonado debe ser mayor a 0.");
            RuleFor(x => x.FormaPago).NotEmpty().MaximumLength(30)
                .WithMessage("La FormaPago es obligatoria.");
        }
    }
}