using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Domain.Entities
{
    public class LogSistema
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Nivel { get; set; } = "INFO";
        public string Modulo { get; set; } = "GENERAL";
        public string Accion { get; set; } = string.Empty;
        public string? Usuario { get; set; }
        public string? IP { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string? DetalleException { get; set; }
        public int TotalRegistros { get; set; }
    }

    public class PagedResultDto<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalRegistros { get; set; }
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
    }
}
