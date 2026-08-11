using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AulaComite.Application.Common.Dto;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
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

            // 1. Catálogo completo de apoderados SASI precargado en índices en memoria
            //    (O(n)): búsqueda instantánea por nombre completo (Diccionario) y por token
            //    (primera palabra) para la coincidencia parcial, evitando el escaneo O(n·m).
            var apoderadosSasi = (await _sasiAuthService.ObtenerApoderadosAsync()).ToList();

            var apoderadosPorNombre = new Dictionary<string, UsuarioSasiDto>(StringComparer.OrdinalIgnoreCase);
            var apoderadosPorToken = new Dictionary<string, List<UsuarioSasiDto>>(StringComparer.OrdinalIgnoreCase);

            foreach (var apoderado in apoderadosSasi)
            {
                var nombre = apoderado.NombreCompleto?.Trim();
                if (string.IsNullOrWhiteSpace(nombre)) continue;

                apoderadosPorNombre.TryAdd(nombre, apoderado);

                foreach (var token in nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!apoderadosPorToken.TryGetValue(token, out var lista))
                    {
                        lista = new List<UsuarioSasiDto>();
                        apoderadosPorToken[token] = lista;
                    }
                    lista.Add(apoderado);
                }
            }

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

                // 🛡️ M7: Rechazo de filas con datos previamente enmascarados (asteriscos)
                if (PiiMasker.EsDatoEnmascarado(item.NumeroDocumento) ||
                    PiiMasker.EsDatoEnmascarado(item.TelefonoApoderado))
                {
                    resultado.DetallesObservaciones.Add($"Fila {index}: Omitido porque los datos enviados contienen formato enmascarado inválido.");
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

                    // Búsqueda O(1) por Nombre Completo normalizado
                    var apoderadoEncontrado = apoderadosPorNombre.GetValueOrDefault(nombreBuscado);

                    // Fallback de coincidencia parcial usando el índice por token (O(1) al
                    // listado de candidatos) en lugar de recorrer todo el catálogo.
                    if (apoderadoEncontrado == null)
                    {
                        var primerToken = nombreBuscado.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                        if (primerToken is not null &&
                            apoderadosPorToken.TryGetValue(primerToken, out var candidatos))
                        {
                            apoderadoEncontrado = candidatos.FirstOrDefault(a =>
                                a.NombreCompleto.Contains(nombreBuscado, StringComparison.OrdinalIgnoreCase));
                        }
                    }

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
