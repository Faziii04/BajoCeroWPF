using System;

namespace ProyectoIntegradorNet10.Models
{
    public class InventarioModel
    {
        public int ProductoId { get; set; }
        public int DepositoId { get; set; }
        public decimal? Cantidad { get; set; }

        // Display helpers
        public string ProductoNombre { get; set; } = string.Empty;
        public string DepositoNombre { get; set; } = string.Empty;
        public string CantidadDisplay => Cantidad?.ToString("N0") ?? "0";
    }
}
