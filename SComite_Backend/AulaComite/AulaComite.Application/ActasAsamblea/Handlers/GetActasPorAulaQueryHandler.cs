using AulaComite.Application.ActasAsamblea.Dtos;
using AulaComite.Application.ActasAsamblea.Queries;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;

namespace AulaComite.Application.ActasAsamblea.Handlers
{
    /// <summary>
    /// 🚀 T3.5: Listado de actas de asamblea por aula. Límite defensivo de 200 registros
    /// para evitar sobrecarga de memoria (OOM) en respuestas masivas.
    /// </summary>
    public class GetActasPorAulaQueryHandler : IRequestHandler<GetActasPorAulaQuery, IEnumerable<ActaAsambleaComiteDto>>
    {
        private const int LimiteMaximoRegistros = 200;

        private readonly IActaAsambleaRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GetActasPorAulaQueryHandler(
            IActaAsambleaRepository repository,
            IComiteRepository comiteRepository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<IEnumerable<ActaAsambleaComiteDto>> Handle(GetActasPorAulaQuery request, CancellationToken cancellationToken)
        {
            // 🛡️ IDOR mitigación: el usuario debe pertenecer al Aula consultada (o ser Administrador Global).
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

            var actas = await _repository.ObtenerPorAulaAsync(request.AulaId, request.AnioLectivo);

            // 🚀 T5: Límite defensivo de volumen para prevenir OOM en listados masivos.
            return actas.Take(LimiteMaximoRegistros).Select(a => new ActaAsambleaComiteDto
            {
                Id = a.Id,
                AulaId = a.AulaId,
                NumeroActa = a.NumeroActa,
                Titulo = a.Titulo,
                FechaReunion = a.FechaReunion,
                AgendaAcuerdos = a.AgendaAcuerdos,
                EstadoActa = a.EstadoActa,
                UrlDocumentoPdf = a.UrlDocumentoPdf,
                UsuarioRegistro = a.UsuarioRegistro,
                FechaRegistro = a.FechaRegistro,
                UsuarioActualizacion = a.UsuarioActualizacion,
                FechaActualizacion = a.FechaActualizacion,
                Estado = a.Estado
            });
        }
    }
}