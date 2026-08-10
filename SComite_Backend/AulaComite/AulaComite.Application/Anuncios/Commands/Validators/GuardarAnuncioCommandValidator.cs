using FluentValidation;

namespace AulaComite.Application.Anuncios.Commands
{
    public class GuardarAnuncioCommandValidator : AbstractValidator<GuardarAnuncioCommand>
    {
        public GuardarAnuncioCommandValidator()
        {
            RuleFor(x => x.AulaId).GreaterThan(0).WithMessage("El AulaId es obligatorio.");
            RuleFor(x => x.Titulo).NotEmpty().MaximumLength(200)
                .WithMessage("El Titulo es obligatorio.");
            RuleFor(x => x.Contenido).NotEmpty().MaximumLength(5000)
                .WithMessage("El Contenido es obligatorio.");
            RuleFor(x => x.Categoria).NotEmpty().MaximumLength(100)
                .WithMessage("La Categoria es obligatoria.");
        }
    }
}