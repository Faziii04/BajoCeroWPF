using System;
using System.Collections.Generic;

namespace ProyectoIntegradorNet10.Models
{
    public class FacturaModel
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Total { get; set; }
        public decimal? Descuento { get; set; }
        public DateTime FechaEmision { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Nit { get; set; }
        public string? DescuentoTipo { get; set; }

        // Display helpers
        public string FechaDisplay => FechaEmision.ToString("dd/MM/yyyy HH:mm");
        public string TotalDisplay => (Total ?? 0).ToString("N2");
        public string SubtotalDisplay => (Subtotal ?? 0).ToString("N2");

        // Detail info (loaded on selection)
        public string? VentaTipo { get; set; }
        public string? VentaEstado { get; set; }
        public string? ClienteNombre { get; set; }
        public List<string> ProductosList { get; set; } = new();
        public List<string> PagosList { get; set; } = new();
        public decimal TotalPagado { get; set; }
        public string TotalPagadoDisplay => TotalPagado.ToString("N2");
    }
}
