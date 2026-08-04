using AulaComite.Application.Institucion.Dtos;
using MediatR;

namespace AulaComite.Application.Institucion.Queries
{
    public record GetInstitucionEducativaQuery() : IRequest<InstitucionEducativaDto>;
}