using System;
using System.Collections.Generic;
using System.Text;
using MediatR;
using AulaComite.Application.Auditoria.Dtos;

namespace AulaComite.Application.Auditoria.Queries
{
    public record GetResumenGeneralCajasQuery(int AnioLectivo, string? Nivel) : IRequest<ResumenGeneralCajasConsolidadasDto>;
}
