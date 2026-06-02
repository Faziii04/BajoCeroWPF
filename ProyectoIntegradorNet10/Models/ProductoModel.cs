using System;

namespace ProyectoIntegradorNet10.Models
{
    public class ProductoModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal? PrecioVenta { get; set; }
        public string? Estado { get; set; }
        public string? Url { get; set; }
        public decimal StockTotal { get; set; }

        public string PrecioDisplay => PrecioVenta?.ToString("N2") ?? "0.00";
        public string StockDisplay => StockTotal.ToString("N0");
    }
}
