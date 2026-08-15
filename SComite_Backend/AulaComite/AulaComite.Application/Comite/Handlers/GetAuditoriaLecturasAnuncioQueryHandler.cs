using AulaComite.Application.Comite.Dtos;
using AulaComite.Application.Comite.Queries;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Comite.Handlers
{
    public class GetAuditoriaLecturasAnuncioQueryHandler : IRequestHandler<GetAuditoriaLecturasAnuncioQuery, ResumenAuditoriaAnuncioDto>
    {
        private readonly IAnuncioRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GetAuditoriaLecturasAnuncioQueryHandler(
            IAnuncioRepository repository,
            IComiteRepository comiteRepository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<ResumenAuditoriaAnuncioDto> Handle(GetAuditoriaLecturasAnuncioQuery request, CancellationToken cancellationToken)
        {
            var anuncio = await _repository.ObtenerPorIdAsync(request.AnuncioId);
            if (anuncio == null)
                throw new UnauthorizedAccessException("El anuncio no existe o no tiene permisos para consultarlo. Acceso denegado.");

            // 🛡️ IDOR mitigación: el usuario debe pertenecer al Aula del anuncio (o ser Administrador Global).
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, anuncio.AulaId);

            var lista = (await _repository.ObtenerAuditoriaLecturasAsync(request.AnuncioId)).ToList();

            // 🛡️ M7: Enmascarar el teléfono del apoderado en la auditoría de lecturas.
            foreach (var lectura in lista)
                lectura.TelefonoApoderado = PiiMasker.EnmascararTelefono(lectura.TelefonoApoderado);

            return new ResumenAuditoriaAnuncioDto
            {
                AnuncioId = request.AnuncioId,
                TotalEstudiantesAula = lista.Count,
                TotalLeidos = lista.Count(x => x.Leido),
                TotalPendientes = lista.Count(x => !x.Leido),
                Lecturas = lista
            };
        }
    }
}
