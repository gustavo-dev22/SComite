using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Estudiantes.Dtos;
using AulaComite.Application.Estudiantes.Queries;
using MediatR;

namespace AulaComite.Application.Estudiantes.Handlers
{
    /// <summary>
    /// 🛡️ M7: Consulta de DETALLE por ID. Devuelve los datos reales SIN enmascarar
    /// (documento y teléfono completos) para que las vistas de edición de usuarios
    /// autenticados puedan editar la ficha del estudiante correctamente.
    /// </summary>
    public class GetEstudianteByIdQueryHandler : IRequestHandler<GetEstudianteByIdQuery, EstudianteDto?>
    {
        private readonly IEstudianteRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GetEstudianteByIdQueryHandler(
            IEstudianteRepository repository,
            IComiteRepository comiteRepository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<EstudianteDto?> Handle(GetEstudianteByIdQuery request, CancellationToken cancellationToken)
        {
            var estudiante = await _repository.ObtenerPorIdAsync(request.EstudianteId);
            if (estudiante == null)
                return null;

            // 🛡️ IDOR mitigación: solo Administrador Global o miembro del comité del aula
            // del estudiante puede ver el detalle (con PII completa) para edición.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, estudiante.AulaId);

            return new EstudianteDto
            {
                Id = estudiante.Id,
                AulaId = estudiante.AulaId,
                TipoDocumento = estudiante.TipoDocumento,
                NumeroDocumento = estudiante.NumeroDocumento,
                Nombres = estudiante.Nombres,
                ApellidoPaterno = estudiante.ApellidoPaterno,
                ApellidoMaterno = estudiante.ApellidoMaterno,
                NombreCompleto = estudiante.NombreCompleto,
                UsuarioIdApoderadoSasi = estudiante.UsuarioIdApoderadoSasi,
                NombreApoderado = estudiante.NombreApoderado,
                TelefonoApoderado = estudiante.TelefonoApoderado,
                Estado = estudiante.Estado,
                FechaRegistro = estudiante.FechaRegistro
            };
        }
    }
}