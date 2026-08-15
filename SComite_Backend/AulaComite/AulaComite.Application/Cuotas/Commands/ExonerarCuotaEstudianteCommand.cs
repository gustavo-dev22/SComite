using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Cuotas.Commands
{
    public record ExonerarCuotaEstudianteCommand(
        int CuotaDetalleId,
        string NuevoEstado, // "EXONERADO" o "PENDIENTE" (para revertir)
        string? MotivoExoneracion
    ) : IRequest<bool>;
}
