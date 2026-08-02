using AulaComite.Application.Comite.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Comite.Queries
{
    public record GetAuditoriaLecturasAnuncioQuery(int AnuncioId) : IRequest<ResumenAuditoriaAnuncioDto>;
}
