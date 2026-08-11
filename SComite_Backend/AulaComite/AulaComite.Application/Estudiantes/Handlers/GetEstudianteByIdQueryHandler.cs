using AulaComite.Application.Common.Interfaces;
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

        public GetEstudianteByIdQueryHandler(IEstudianteRepository repository)
        {
            _repository = repository;
        }

        public async Task<EstudianteDto?> Handle(GetEstudianteByIdQuery request, CancellationToken cancellationToken)
        {
            var estudiante = await _repository.ObtenerPorIdAsync(request.EstudianteId);
            if (estudiante == null)
                return null;

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