using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Estudiantes.Commands;
using AulaComite.Application.Estudiantes.Dtos;
using MediatR;

namespace AulaComite.Application.Estudiantes.Handlers
{
    public class CargaMasivaEstudiantesCommandHandler
    : IRequestHandler<CargaMasivaEstudiantesCommand, CargaMasivaResultadoDto>
    {
        private readonly IEstudianteRepository _estudianteRepository;
        private readonly ISasiAuthService _sasiAuthService;

        public CargaMasivaEstudiantesCommandHandler(IEstudianteRepository estudianteRepository, ISasiAuthService sasiAuthService)
        {
            _estudianteRepository = estudianteRepository;
            _sasiAuthService = sasiAuthService;
        }

        public async Task<CargaMasivaResultadoDto> Handle(CargaMasivaEstudiantesCommand request, CancellationToken cancellationToken)
        {
            var resultado = new CargaMasivaResultadoDto
            {
                RegistrosProcesados = request.Estudiantes.Count
            };

            // 1. Catálogo completo de apoderados SASI en memoria
            var apoderadosSasi = (await _sasiAuthService.ObtenerApoderadosAsync()).ToList();

            // 2. Estudiantes ya matriculados en esta aula
            var estudiantesExistentes = (await _estudianteRepository.ObtenerPorAulaAsync(request.AulaId))
                .Select(x => x.NumeroDocumento.Trim())
                .ToHashSet();

            var validosParaInsertar = new List<EstudianteImportacionItemDto>();

            int index = 1;
            foreach (var item in request.Estudiantes)
            {
                index++;

                // Validación A: Datos mínimos del alumno
                if (string.IsNullOrWhiteSpace(item.NumeroDocumento) ||
                    string.IsNullOrWhiteSpace(item.Nombres) ||
                    string.IsNullOrWhiteSpace(item.ApellidoPaterno))
                {
                    resultado.DetallesObservaciones.Add($"Fila {index}: Omitido por datos incompletos (requiere N° Doc, Nombres y Ap. Paterno).");
                    continue;
                }

                // Validación B: Duplicidad de DNI en la misma aula
                if (estudiantesExistentes.Contains(item.NumeroDocumento.Trim()))
                {
                    resultado.DetallesObservaciones.Add($"Fila {index} ({item.NumeroDocumento}): Omitido porque ya está matriculado en esta aula.");
                    continue;
                }

                // 🚀 Validación C: EXISTENCIA OBLIGATORIA EN SASI
                if (!string.IsNullOrWhiteSpace(item.NombreApoderado))
                {
                    var nombreBuscado = item.NombreApoderado.Trim();

                    // Búsqueda por Nombre Completo
                    var apoderadoEncontrado = apoderadosSasi.FirstOrDefault(a =>
                        a.NombreCompleto.Equals(nombreBuscado, StringComparison.OrdinalIgnoreCase) ||
                        a.NombreCompleto.Contains(nombreBuscado, StringComparison.OrdinalIgnoreCase)
                    );

                    if (apoderadoEncontrado != null)
                    {
                        item.UsuarioIdApoderadoSasi = apoderadoEncontrado.UsuarioId;
                        item.NombreApoderado = apoderadoEncontrado.NombreCompleto; // Normalizar nombre
                    }
                    else
                    {
                        // ❌ NO EXISTE EN SASI: SE RECHAZA EL REGISTRO
                        resultado.DetallesObservaciones.Add($"Fila {index} ({item.Nombres} {item.ApellidoPaterno}): Rechazado porque el apoderado '{nombreBuscado}' NO existe registrado en el SASI.");
                        continue; // Pasa a la siguiente fila sin agregar a validosParaInsertar
                    }
                }

                validosParaInsertar.Add(item);
            }

            // 3. Inserción en la base de datos
            if (validosParaInsertar.Any())
            {
                resultado.RegistrosInsertados = await _estudianteRepository.CargaMasivaEstudiantesAsync(request.AulaId, validosParaInsertar);
            }

            resultado.RegistrosOmitidos = resultado.RegistrosProcesados - resultado.RegistrosInsertados;
            return resultado;
        }
    }
}
