using AulaComite.Application.Comite.Dtos;
using AulaComite.Application.Comite.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;

namespace AulaComite.Application.Comite.Handlers
{
    public class GetComitePorAulaQueryHandler : IRequestHandler<GetComitePorAulaQuery, IEnumerable<ComiteIntegranteDto>>
    {
        private readonly IComiteRepository _repository;

        public GetComitePorAulaQueryHandler(IComiteRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ComiteIntegranteDto>> Handle(GetComitePorAulaQuery request, CancellationToken cancellationToken)
        {
            var integrantes = await _repository.ObtenerPorAulaAsync(request.AulaId);

            return integrantes.Select(i => new ComiteIntegranteDto
            {
                Id = i.Id,
                AulaId = i.AulaId,
                UsuarioIdSasi = i.UsuarioIdSasi,
                NombreCompleto = i.NombreCompleto,
                Email = i.Email,
                Cargo = i.Cargo,
                Estado = i.Estado,
                FechaAsignacion = i.FechaAsignacion
            });
        }
    }
}