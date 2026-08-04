using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ISasiAuthService _sasiAuthService;
        private readonly IValidator<LoginRequestDto> _loginValidator;

        public AuthController(ISasiAuthService sasiAuthService, IValidator<LoginRequestDto> loginValidator)
        {
            _sasiAuthService = sasiAuthService;
            _loginValidator = loginValidator;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("LoginLimiter")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            // Validación explícita para endurecer el login contra peticiones malformadas
            var validationResult = await _loginValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errores = validationResult.Errors
                    .Select(e => new { campo = e.PropertyName, mensaje = e.ErrorMessage });
                return BadRequest(new { mensaje = "La solicitud de inicio de sesión no es válida.", errores });
            }

            var result = await _sasiAuthService.AutenticarAsync(request);

            if (!result.Exito)
            {
                return BadRequest(new { mensaje = result.Mensaje });
            }

            return Ok(result);
        }
    }
}