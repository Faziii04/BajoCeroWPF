using System;

namespace ProyectoIntegradorNet10.Models
{
    public class RepartidorModel
    {
        public int Id { get; set; }
        public string? Estado { get; set; }
        public string? Zona { get; set; }
        public string? Licencia { get; set; }
        public string? EmpleadoCi { get; set; }

        // ── Display helpers (not from DB) ──

        /// <summary>Empleado full name, loaded via join.</summary>
        public string? EmpleadoNombre { get; set; }

        public string NombreCompleto => $"{EmpleadoNombre ?? "—"} (Lic: {Licencia ?? "—"})";

        public string EstadoDisplay => Estado ?? "Inactivo";
    }

    public class RepartidorVehiculoModel
    {
        public int RepartidorId { get; set; }
        public string VehiculoPlaca { get; set; } = string.Empty;
        public string? Estado { get; set; }
        public DateTime? FechaHoraAsignacion { get; set; }
        public DateTime? FechaHoraFin { get; set; }

        // ── Display helpers (not from DB) ──

        public string? RepartidorNombre { get; set; }
        public string? RepartidorLicencia { get; set; }
        public string? RepartidorZona { get; set; }

        public string RepartidorDisplay => $"{RepartidorNombre ?? "—"} (Lic: {RepartidorLicencia ?? "—"})";
    }
}
