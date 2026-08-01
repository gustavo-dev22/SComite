using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.Institucion.Queries
{
    public record GetInstitucionEducativaQuery() : IRequest<InstitucionEducativa?>;
}
