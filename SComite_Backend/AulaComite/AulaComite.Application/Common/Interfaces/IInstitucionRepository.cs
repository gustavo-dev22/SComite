using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IInstitucionRepository
    {
        Task<InstitucionEducativa?> ObtenerAsync();
        Task<bool> GuardarAsync(InstitucionEducativa entidad);
    }
}
