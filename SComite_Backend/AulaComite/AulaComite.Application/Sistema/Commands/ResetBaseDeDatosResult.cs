namespace AulaComite.Application.Sistema.Commands
{
    /// <summary>
    /// Resultado de la operación de reseteo. Reemplaza el uso de excepciones como
    /// flujo normal de control: los rechazos esperados (entorno, permisos o texto de
    /// confirmación) se comunican por el propio resultado.
    /// </summary>
    public record ResetBaseDeDatosResult(
        bool Exito,
        string Mensaje,
        bool EsErrorDeAutorizacion = false);
}