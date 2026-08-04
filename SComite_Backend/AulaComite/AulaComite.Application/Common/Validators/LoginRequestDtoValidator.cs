using AulaComite.Application.Common.Models;
using FluentValidation;

namespace AulaComite.Application.Common.Validators
{
    public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestDtoValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty()
                .WithMessage("El nombre de usuario es obligatorio.")
                .MaximumLength(50)
                .WithMessage("El nombre de usuario no puede superar los 50 caracteres.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("La contraseña es obligatoria.")
                .MaximumLength(200)
                .WithMessage("La contraseña no puede superar los 200 caracteres.");
        }
    }
}