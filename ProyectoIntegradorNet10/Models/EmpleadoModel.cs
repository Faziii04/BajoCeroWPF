using System;

namespace ProyectoIntegradorNet10.Models
{
    public class EmpleadoModel
    {
        public string Ci { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string Correo { get; set; } = string.Empty;
        public string? Area { get; set; }
        public string? Telefono { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Turno { get; set; }
    }

    public class RolModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    public class EmpleadoRolModel
    {
        public string EmpleadoCi { get; set; } = string.Empty;
        public int RolId { get; set; }
        public string? Estado { get; set; }
        public DateTime? FechaHoraAsignacion { get; set; }
        public DateTime? FechaHoraFin { get; set; }

        // Display helpers (not from DB)
        public string? RolNombre { get; set; }
    }
}
