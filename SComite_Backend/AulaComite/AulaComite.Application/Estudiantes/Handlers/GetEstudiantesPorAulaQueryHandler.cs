using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Estudiantes.Dtos;
using AulaComite.Application.Estudiantes.Queries;
using MediatR;

namespace AulaComite.Application.Estudiantes.Handlers
{
    /// <summary>
    /// 🚀 T3.5: Listado de estudiantes por aula. Límite defensivo de 200 registros
    /// para evitar sobrecarga de memoria (OOM) en respuestas masivas.
    /// </summary>
    public class GetEstudiantesPorAulaQueryHandler : IRequestHandler<GetEstudiantesPorAulaQuery, IEnumerable<EstudianteDto>>
    {
        private const int LimiteMaximoRegistros = 200;

        private readonly IEstudianteRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GetEstudiantesPorAulaQueryHandler(
            IEstudianteRepository repository,
            IComiteRepository comiteRepository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<IEnumerable<EstudianteDto>> Handle(GetEstudiantesPorAulaQuery request, CancellationToken cancellationToken)
        {
            // 🛡️ IDOR mitigación: el usuario debe pertenecer al Aula consultada (o ser Administrador Global).
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

            var estudiantes = await _repository.ObtenerPorAulaAsync(request.AulaId);

            // 🛡️ M7: En listados se enmascaran el DNI y el teléfono del apoderado.
            // 🚀 T5: Límite defensivo de volumen para prevenir OOM en listados masivos.
            return estudiantes.Take(LimiteMaximoRegistros).Select(e => new EstudianteDto
            {
                Id = e.Id,
                AulaId = e.AulaId,
                TipoDocumento = e.TipoDocumento,
                NumeroDocumento = PiiMasker.EnmascararDocumento(e.NumeroDocumento),
                Nombres = e.Nombres,
                ApellidoPaterno = e.ApellidoPaterno,
                ApellidoMaterno = e.ApellidoMaterno,
                NombreCompleto = e.NombreCompleto,
                UsuarioIdApoderadoSasi = e.UsuarioIdApoderadoSasi,
                NombreApoderado = e.NombreApoderado,
                TelefonoApoderado = PiiMasker.EnmascararTelefono(e.TelefonoApoderado),
                Estado = e.Estado,
                FechaRegistro = e.FechaRegistro
            });
        }
    }
}