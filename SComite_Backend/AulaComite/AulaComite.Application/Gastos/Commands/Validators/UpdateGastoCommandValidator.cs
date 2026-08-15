using FluentValidation;

namespace AulaComite.Application.Gastos.Commands
{
    public class UpdateGastoCommandValidator : AbstractValidator<UpdateGastoCommand>
    {
        private static readonly string[] CategoriasValidas =
        {
            "MATERIALES",
            "MANTENIMIENTO",
            "ACTIVIDADES_EVENTOS",
            "REFRIGERIOS",
            "OTROS"
        };

        public UpdateGastoCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("El Id del gasto es obligatorio.");
            RuleFor(x => x.AulaId).GreaterThan(0)
                .WithMessage("El AulaId es obligatorio.");
            RuleFor(x => x.Concepto).NotEmpty().MaximumLength(200)
                .WithMessage("El Concepto es obligatorio y no debe superar los 200 caracteres.");
            RuleFor(x => x.Categoria).NotEmpty().MaximumLength(100)
                .Must(c => CategoriasValidas.Contains(c, StringComparer.OrdinalIgnoreCase))
                .WithMessage("La Categoria no es válida. Valores permitidos: MATERIALES, MANTENIMIENTO, ACTIVIDADES_EVENTOS, REFRIGERIOS, OTROS.");
            RuleFor(x => x.Monto).GreaterThan(0).LessThanOrEqualTo(100000)
                .WithMessage("El Monto debe ser mayor a 0 y menor o igual a 100000.");
            RuleFor(x => x.FechaGasto).NotEmpty()
                .WithMessage("La FechaGasto es obligatoria.");
            RuleFor(x => x.TipoComprobante).NotEmpty().MaximumLength(30)
                .WithMessage("El TipoComprobante es obligatorio.");
        }
    }
}