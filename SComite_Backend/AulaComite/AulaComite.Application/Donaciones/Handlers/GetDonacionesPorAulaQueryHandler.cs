using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Donaciones.Dtos;
using AulaComite.Application.Donaciones.Queries;
using MediatR;

namespace AulaComite.Application.Donaciones.Handlers
{
    public class GetDonacionesPorAulaQueryHandler : IRequestHandler<GetDonacionesPorAulaQuery, IEnumerable<DonacionDto>>
    {
        private readonly IDonacionRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GetDonacionesPorAulaQueryHandler(
            IDonacionRepository repository,
            IComiteRepository comiteRepository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<IEnumerable<DonacionDto>> Handle(GetDonacionesPorAulaQuery request, CancellationToken cancellationToken)
        {
            // 🛡️ IDOR mitigación: el usuario debe pertenecer al Aula consultada (o ser Administrador Global).
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

            return await _repository.ObtenerPorAulaAsync(request.AulaId, request.AnioLectivo, request.Mes);
        }
    }
}