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

        public GetEstudiantesPorAulaQueryHandler(IEstudianteRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<EstudianteDto>> Handle(GetEstudiantesPorAulaQuery request, CancellationToken cancellationToken)
        {
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