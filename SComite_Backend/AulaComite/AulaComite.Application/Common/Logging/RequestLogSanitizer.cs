using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace AulaComite.Application.Common.Logging
{
    public static class RequestLogSanitizer
    {
        private static readonly string[] PalabrasSensibles =
        {
            "password", "contraseña", "pass", "pwd",
            "clave", "secret", "token", "apikey", "api_key",
            "authorization", "bearer", "credencial", "session", "cookie",
            "dni", "documento", "numero_documento", "numdocumento",
            "telefono", "celular", "movil", "correo", "email", "mail",
            "apoderado", "nombreapoderado", "usuarioapoderado", "direccion"
        };

        public static object? Sanitizar(object? value)
        {
            if (value == null) return null;
            if (value is string s) return s;

            // 🚀 COLOCAR AQUÍ: Debe evaluarse ANTES de IEnumerable para evitar iterar los bytes
            if (value is Stream || value is byte[])
            {
                return "[Archivo / Binary Data]";
            }

            if (value is IEnumerable enumerable)
            {
                var lista = new List<object?>();
                foreach (var item in enumerable)
                {
                    lista.Add(Sanitizar(item));
                }
                return lista;
            }

            var type = value.GetType();

            if (type.IsPrimitive || type.IsEnum || type == typeof(decimal) || type == typeof(DateTime))
            {
                return value;
            }

            var resultado = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;

                var propValue = prop.GetValue(value);
                resultado[prop.Name] = EsSensible(prop.Name) ? "[OCULTO]" : Sanitizar(propValue);
            }

            return resultado;
        }

        private static bool EsSensible(string nombrePropiedad)
        {
            var normalizado = nombrePropiedad.ToLower(CultureInfo.InvariantCulture);
            return PalabrasSensibles.Any(p => normalizado.Contains(p, StringComparison.OrdinalIgnoreCase));
        }
    }
}