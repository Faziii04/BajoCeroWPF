using System;
using System.Collections.Generic;

namespace ProyectoIntegradorNet10.Models
{
    public class ProduccionModel
    {
        public int Id { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public decimal? CostoTotal { get; set; }
        public string? Estado { get; set; } // "Planificado", "En proceso", "Completado", "Cancelado"

        // ── Display helpers ──
        public string FechaInicioDisplay => FechaInicio.ToString("dd/MM/yyyy HH:mm");
        public string FechaFinDisplay => FechaFin?.ToString("dd/MM/yyyy HH:mm") ?? "—";
        public string CostoTotalDisplay => CostoTotal?.ToString("N2") ?? "0.00";
        public string EstadoDisplay => Estado ?? "Planificado";

        // ── Detail collections (loaded on demand) ──
        public List<ProduccionInsumoModel> Insumos { get; set; } = new();
        public List<ProduccionProductoModel> Productos { get; set; } = new();
    }

    /// <summary>
    /// Row from the insumo_produccion pivot table.
    /// Tracks which raw materials (insumos) are consumed in a production run.
    /// </summary>
    public class ProduccionInsumoModel
    {
        public int ProduccionId { get; set; }
        public int InsumoId { get; set; }
        public decimal? Cantidad { get; set; }

        // Display helpers (joined from DB, not direct columns)
        public string? InsumoNombre { get; set; }
        public decimal? InsumoPrecio { get; set; }
        public string? UnidadMedida { get; set; }
        public decimal? StockDisponible { get; set; }

        public decimal Subtotal => (Cantidad ?? 0) * (InsumoPrecio ?? 0);
        public string SubtotalDisplay => Subtotal.ToString("N2");
        public string CantidadDisplay => Cantidad?.ToString("N2") ?? "0";
        public string StockDisponibleDisplay => StockDisponible?.ToString("N0") ?? "?";
    }

    /// <summary>
    /// Row from the produccion_producto pivot table.
    /// Tracks which finished products are manufactured in a production run.
    /// </summary>
    public class ProduccionProductoModel
    {
        public int ProductoId { get; set; }
        public int ProduccionId { get; set; }
        public decimal? Cantidad { get; set; }

        // Display helpers (joined from DB, not direct columns)
        public string? ProductoNombre { get; set; }
        public decimal? PrecioVenta { get; set; }

        public string CantidadDisplay => Cantidad?.ToString("N0") ?? "0";
        public string PrecioVentaDisplay => PrecioVenta?.ToString("N2") ?? "0.00";
    }
}
