using AulaComite.Application.Apoderado.Dtos;
using AulaComite.Application.Common.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace AulaComite.Application.Common.Security
{
    /// <summary>
    /// Guard de autorización de objetos para el módulo Apoderado.
    /// Verifica que el EstudianteId consultado pertenezca a los hijos
    /// del usuario autenticado antes de exponer cualquier dato.
    /// </summary>
    public static class ApoderadoAccessValidator
    {
        public static async Task<bool> EsEstudianteDelApoderadoAsync(
            IApoderadoRepository repository,
            IUserContextService userContextService,
            int estudianteId,
            int anioLectivo)
        {
            var usuario = userContextService.ObtenerUsuarioActual();

            if (string.IsNullOrEmpty(usuario) || usuario == "Anónimo")
            {
                return false;
            }

            var hijos = await repository.ObtenerHijosApoderadoAsync(usuario, anioLectivo);

            return hijos.Any(h => h.EstudianteId == estudianteId);
        }
    }
}