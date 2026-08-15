using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Gastos.Dtos;
using AulaComite.Application.Gastos.Queries;
using MediatR;

namespace AulaComite.Application.Gastos.Handlers
{
    public class GetResumenCajaQueryHandler : IRequestHandler<GetResumenCajaQuery, ResumenCajaAulaDto>
    {
        private readonly IGastoRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GetResumenCajaQueryHandler(
            IGastoRepository repository,
            IComiteRepository comiteRepository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<ResumenCajaAulaDto> Handle(GetResumenCajaQuery request, CancellationToken cancellationToken)
        {
            // 🛡️ IDOR mitigación: el usuario debe pertenecer al Aula consultada (o ser Administrador Global).
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

            return await _repository.ObtenerResumenCajaAsync(request.AulaId);
        }
    }
}