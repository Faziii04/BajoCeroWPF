using System;

namespace ProyectoIntegradorNet10.Models
{
    public class ClienteModel
    {
        public string Ci { get; set; } = string.Empty;
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Direccion { get; set; }
        public string? Nit { get; set; }
        public string? Telefono { get; set; }
        public string? Url { get; set; }

        // Display helper (not from DB)
        public string NombreCompleto => $"{Nombre ?? ""} {Apellido ?? ""}".Trim();
    }
}
