using System;
using System.Collections.Generic;

namespace ProyectoIntegradorNet10.Models
{
    public class VentaModel
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
        public string? Tipo { get; set; }          // 'Contado' or 'Plan de pago'
        public string? Estado { get; set; }         // 'Pendiente', 'Pagado', 'Anulado'
        public decimal? PorcentajeDescuento { get; set; }
        public int? RepartidorId { get; set; }
        public string? ClienteCi { get; set; }

        // Display helpers
        public string? ClienteNombre { get; set; }
        public string? RepartidorNombre { get; set; }
        public List<VentaDetalleModel> Detalles { get; set; } = new();
        public int? Meses { get; set; } // For "Plan de pago"

        // Raw monto from database (persisted value)
        public decimal? MontoFromDb { get; set; }

        public decimal Total
        {
            get
            {
                // If we have detalles loaded, compute from them
                if (Detalles.Count > 0)
                {
                    decimal subtotal = 0;
                    foreach (var d in Detalles)
                        subtotal += (d.Cantidad ?? 0) * (d.PrecioUnitario ?? 0);
                    if (PorcentajeDescuento.HasValue && PorcentajeDescuento.Value > 0)
                        subtotal -= subtotal * (PorcentajeDescuento.Value / 100m);
                    return subtotal;
                }
                // Otherwise use the persisted monto from DB
                return MontoFromDb ?? 0;
            }
        }
        public string FechaDisplay => Fecha.ToString("dd/MM/yyyy");
        public string HoraDisplay => Hora.ToString(@"hh\:mm");
        public string TotalDisplay => Total.ToString("N2");
    }

    public class VentaDetalleModel
    {
        public int ProductoId { get; set; }
        public int VentaId { get; set; }
        public int? Cantidad { get; set; }
        public decimal? PrecioUnitario { get; set; }

        // Display helpers
        public string? ProductoNombre { get; set; }
        public decimal Subtotal => (Cantidad ?? 0) * (PrecioUnitario ?? 0);
        public string SubtotalDisplay => Subtotal.ToString("N2");
    }

    public class PagoModel
    {
        public int PagoId { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
        public decimal Monto { get; set; }
        public string? Metodo { get; set; }   // 'Efectivo', 'Transferencia', 'QR'
        public string? Estado { get; set; }   // 'Pendiente', 'Pagado', 'Vencido'
        public int VentaId { get; set; }

        public string FechaDisplay => Fecha.ToString("dd/MM/yyyy");
        public string HoraDisplay => Hora.ToString(@"hh\:mm");
        public string MontoDisplay => Monto.ToString("N2");
    }
}
