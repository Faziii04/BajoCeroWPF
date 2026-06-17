using System;

namespace ProyectoIntegradorNet10.Models
{
    public class PrestamoModel
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string? Estado { get; set; }

        // ─── Display helpers (joined from DB, not direct columns) ───
        public string? ClienteNombre { get; set; }
        public decimal ValorTotal { get; set; }

        public string FechaDisplay => Fecha.ToString("dd/MM/yyyy HH:mm");
        public string ValorTotalDisplay => ValorTotal.ToString("N2");
    }
}
