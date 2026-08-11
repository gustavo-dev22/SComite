using AulaComite.Application.Apoderado.Dtos;
using AulaComite.Application.Apoderado.Queries;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Apoderado.Handlers
{
    public class GetHijosApoderadoQueryHandler : IRequestHandler<GetHijosApoderadoQuery, List<HijoApoderadoDto>>
    {
        private readonly IApoderadoRepository _repository;
        private readonly IUserContextService _userContextService;

        public GetHijosApoderadoQueryHandler(IApoderadoRepository repository, IUserContextService userContextService)
        {
            _repository = repository;
            _userContextService = userContextService;
        }

        public async Task<List<HijoApoderadoDto>> Handle(GetHijosApoderadoQuery request, CancellationToken cancellationToken)
        {
            var usuarioApoderado = _userContextService.ObtenerUsuarioActual();

            if (string.IsNullOrEmpty(usuarioApoderado) || usuarioApoderado == "Anónimo")
            {
                return new List<HijoApoderadoDto>();
            }

            var result = await _repository.ObtenerHijosApoderadoAsync(usuarioApoderado, request.AnioLectivo);

            // 🛡️ M7: En el listado de hijos se enmascara el teléfono del tesorero del Aula.
            foreach (var hijo in result)
            {
                hijo.TesoreroTelefono = PiiMasker.EnmascararTelefono(hijo.TesoreroTelefono);
            }

            return result.ToList();
        }
    }
}
