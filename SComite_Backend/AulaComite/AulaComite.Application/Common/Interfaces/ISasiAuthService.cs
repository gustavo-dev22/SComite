using AulaComite.Application.Common.Dto;
using AulaComite.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Common.Interfaces
{
    public interface ISasiAuthService
    {
        Task<AuthResultDto> AutenticarAsync(LoginRequestDto request);
    }
}
