using System;

namespace ProyectoIntegradorNet10.Models
{
    public class OrdenCompraModel
    {
        public int Id { get; set; }
        public DateTime FechaPedido { get; set; }
        public TimeSpan HoraPedido { get; set; }
        public string? Estado { get; set; }
        public decimal? Monto { get; set; }
        public int ProveedorId { get; set; } // maps to proveedor column

        public string? ProveedorNombre { get; set; }

        // New columns
        public DateTime? FechaLlegada { get; set; }
        public TimeSpan? HoraLlegada { get; set; }

        public string FechaPedidoDisplay => FechaPedido.ToString("dd/MM/yyyy");
        public string HoraPedidoDisplay => HoraPedido.ToString(@"hh\:mm");
        public string FechaLlegadaDisplay => FechaLlegada?.ToString("dd/MM/yyyy") ?? "";
        public string HoraLlegadaDisplay => HoraLlegada?.ToString(@"hh\:mm") ?? "";
        public string MontoDisplay => Monto?.ToString("N2") ?? "0.00";
    }
}
