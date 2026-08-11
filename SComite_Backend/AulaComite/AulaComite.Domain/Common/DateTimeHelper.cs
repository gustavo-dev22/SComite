using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Domain.Common
{
    /// <summary>
    /// Helper centralizado de fechas del dominio. Expone la hora oficial de Perú
    /// (UTC-5, "America/Lima") para que todas las entidades, handlers y servicios
    /// del backend registren fechas sincronizadas con la hora local real.
    /// </summary>
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo ZonaHorariaPeru = ResolverZonaHorariaPeru();

        private static TimeZoneInfo ResolverZonaHorariaPeru()
        {
            // IANA (Linux/contenerdores y .NET 6+) y su equivalente Windows como respaldo.
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
        }

        /// <summary>
        /// Devuelve la fecha y hora actual expresada en la zona horaria de Perú (UTC-5).
        /// </summary>
        public static DateTime ObtenerHoraPeru()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaHorariaPeru);
        }
    }
}