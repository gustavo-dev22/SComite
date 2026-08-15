using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Cuotas.Dtos;
using MediatR;

namespace AulaComite.Application.Cuotas.Queries
{
    public record GetEstudiantesExoneradosCuotaQuery(int CuotaId) : IRequest<List<EstudianteExoneradoCuotaDto>>;
}
