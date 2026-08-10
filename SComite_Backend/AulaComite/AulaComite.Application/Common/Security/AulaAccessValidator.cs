using AulaComite.Application.Common.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AulaComite.Application.Common.Security
{
    /// <summary>
    /// Guard de autorización: verifica que el Aula sobre la que se pretende operar
    /// pertenezca al usuario autenticado (asignado vía Comité de Aula) o bien que el
    /// usuario sea un Administrador Global. Esto previene que un usuario modifique o
    /// elimine recursos de Aulas/Institución ajenas.
    /// </summary>
    public static class AulaAccessValidator
    {
        public static async Task ValidarAccesoAulaAsync(
            IComiteRepository comiteRepository,
            IUserContextService userContext,
            int? aulaId)
        {
            if (!aulaId.HasValue || aulaId.Value <= 0)
                throw new UnauthorizedAccessException("No se pudo determinar el aula del recurso. Acceso denegado.");

            // Administrador Global puede operar sobre todas las aulas.
            if (userContext.EsAdministradorGlobal())
                return;

            var usuarioId = userContext.ObtenerUsuarioId();
            if (string.IsNullOrEmpty(usuarioId))
                throw new UnauthorizedAccessException("No se pudo identificar al usuario autenticado. Acceso denegado.");

            var aulasAsignadas = await comiteRepository.ObtenerAulaIdsPorUsuarioAsync(usuarioId);
            if (!aulasAsignadas.Contains(aulaId.Value))
                throw new UnauthorizedAccessException("El aula especificada no está asignada a su usuario. Acceso denegado.");
        }
    }
}