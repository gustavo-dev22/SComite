using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Estudiantes.Dtos;
using AulaComite.Application.Estudiantes.Queries;
using MediatR;

namespace AulaComite.Application.Estudiantes.Handlers
{
    public class GetEstudiantesPorAulaQueryHandler : IRequestHandler<GetEstudiantesPorAulaQuery, IEnumerable<EstudianteDto>>
    {
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
            return estudiantes.Select(e => new EstudianteDto
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