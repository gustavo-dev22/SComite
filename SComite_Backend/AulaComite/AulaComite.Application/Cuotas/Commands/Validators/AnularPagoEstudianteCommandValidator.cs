using FluentValidation;

namespace AulaComite.Application.Cuotas.Commands
{
    public class AnularPagoEstudianteCommandValidator : AbstractValidator<AnularPagoEstudianteCommand>
    {
        public AnularPagoEstudianteCommandValidator()
        {
            RuleFor(x => x.CuotaDetalleId).GreaterThan(0)
                .WithMessage("El CuotaDetalleId es obligatorio.");
        }
    }
}