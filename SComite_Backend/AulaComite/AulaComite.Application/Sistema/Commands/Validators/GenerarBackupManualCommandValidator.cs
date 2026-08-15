using FluentValidation;

namespace AulaComite.Application.Sistema.Commands
{
    public class GenerarBackupManualCommandValidator : AbstractValidator<GenerarBackupManualCommand>
    {
        public GenerarBackupManualCommandValidator()
        {
            // Sin reglas: el comando no recibe parámetros de entrada validables.
        }
    }
}