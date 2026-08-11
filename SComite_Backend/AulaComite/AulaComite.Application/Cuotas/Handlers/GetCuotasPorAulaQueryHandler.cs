using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Cuotas.Dtos;
using AulaComite.Application.Cuotas.Queries;
using MediatR;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class GetCuotasPorAulaQueryHandler : IRequestHandler<GetCuotasPorAulaQuery, IEnumerable<CuotaDto>>
    {
        private readonly ICuotaRepository _repository;

        public GetCuotasPorAulaQueryHandler(ICuotaRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CuotaDto>> Handle(GetCuotasPorAulaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerPorAulaAsync(request.AulaId);
        }
    }
}