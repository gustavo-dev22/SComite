namespace AulaComite.Application.Common.Dto
{
    // Datos que envía el frontend en /Auth/sso para canjear el authorization code
    // devuelto por SASI (flujo authorization_code + PKCE).
    public class SsoLoginRequestDto
    {
        public string Code { get; set; } = string.Empty;
        public string CodeVerifier { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
    }
}
