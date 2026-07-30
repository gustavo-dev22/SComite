using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Cuotas.Commands
{
    public record GenerarCuotasMensualesCommand(
        int AulaId,
        string ConceptoBase, 
        decimal MontoMensual,
        int MesInicio, 
        int DiaVencimiento, 
        int AnioLectivo
    ) : IRequest<bool>;
}
