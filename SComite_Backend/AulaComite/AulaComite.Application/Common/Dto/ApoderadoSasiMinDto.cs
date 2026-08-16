using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Common.Dto
{
    /// <summary>
    /// 🛡️ T4.6: Representación mínima de un apoderado proveniente de SASI. Solo expone
    /// los campos estrictamente necesarios (ID, nombre completo y correo), evitando
    /// filtrar datos internos o sensibles del usuario en el endpoint público.
    /// </summary>
    public class ApoderadoSasiMinDto
    {
        public string UsuarioId { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}