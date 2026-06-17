namespace ProyectoIntegradorNet10.Models
{
    public class ProveedorModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Descripcion { get; set; }
    }
}
