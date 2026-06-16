using System;

namespace ProyectoIntegradorNet10.Models
{
    public class IncidenciaModel
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public bool Resuelto { get; set; }
        public string? Notas { get; set; }
        public int? VentaId { get; set; }

        // Display helpers
        public string FechaDisplay => Fecha.ToString("dd/MM/yyyy");
        public string HoraDisplay => Hora.ToString(@"hh\:mm");
        public string ResueltoDisplay => Resuelto ? "✅ Resuelto" : "❌ Pendiente";
    }
}
