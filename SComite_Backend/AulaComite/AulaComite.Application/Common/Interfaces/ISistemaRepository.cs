using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Common.Interfaces
{
    public interface ISistemaRepository
    {
        Task<bool> ResetBaseDeDatosAsync();
    }
}
