using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ISasiAuthService _sasiAuthService;

        public AuthController(ISasiAuthService sasiAuthService)
        {
            _sasiAuthService = sasiAuthService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _sasiAuthService.AutenticarAsync(request);

            if (!result.Exito)
            {
                return BadRequest(new { mensaje = result.Mensaje });
            }

            return Ok(result);
        }
    }
}
