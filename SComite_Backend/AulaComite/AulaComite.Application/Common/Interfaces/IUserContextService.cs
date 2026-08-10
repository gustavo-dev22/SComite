using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IUserContextService
    {
        string ObtenerUsuarioActual();
        string ObtenerIpCliente();
        string? ObtenerUsuarioId();
        bool EsAdministradorGlobal();
    }
}
