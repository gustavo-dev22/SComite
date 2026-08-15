using FluentValidation;

namespace AulaComite.Application.Anuncios.Commands
{
    public class EliminarAnuncioCommandValidator : AbstractValidator<EliminarAnuncioCommand>
    {
        public EliminarAnuncioCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("El Id del anuncio es obligatorio.");
            RuleFor(x => x.AulaId).GreaterThan(0)
                .WithMessage("El AulaId es obligatorio.");
        }
    }
}