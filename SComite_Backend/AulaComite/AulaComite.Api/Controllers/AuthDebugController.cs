using System.IdentityModel.Tokens.Jwt;
using AulaComite.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AulaComite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthDebugController : ControllerBase
{
    private readonly IUserContextService _userContextService;

    public AuthDebugController(IUserContextService userContextService)
    {
        _userContextService = userContextService;
    }

    [HttpGet("inspect-token")]
    public IActionResult InspectHeaderAndToken()
    {
        // 1. Verificar si viene el Header 'Authorization'
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader))
        {
            return Ok(new
            {
                estado = "ERROR_HEADER_FALTANTE",
                mensaje = "El Frontend NO está enviando el encabezado 'Authorization' en la petición HTTP.",
                ipCliente = _userContextService.ObtenerIpCliente(),
                usuarioResultado = _userContextService.ObtenerUsuarioActual()
            });
        }

        // 2. Extraer el token
        string token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader.Substring(7).Trim()
            : authHeader;

        // 3. Decodificar todas las Claims que contiene el Token JWT entregado por SASI
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                return Ok(new { estado = "TOKEN_INVALIDO", mensaje = "El formato de la cadena enviada no es un JWT válido." });
            }

            var jwtToken = handler.ReadJwtToken(token);
            var claims = jwtToken.Claims.Select(c => new { Tipo = c.Type, Valor = c.Value }).ToList();

            return Ok(new
            {
                estado = "TOKEN_LEIDO_EXITOSAMENTE",
                usuarioResultadoServicio = _userContextService.ObtenerUsuarioActual(),
                ipClienteResultadoServicio = _userContextService.ObtenerIpCliente(),
                claimsEncontradasEnJwt = claims
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { estado = "ERROR_LECTURA", mensaje = ex.Message });
        }
    }
}
