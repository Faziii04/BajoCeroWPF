namespace ProyectoIntegradorNet10.Models
{
    public class FamiliaModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Url { get; set; }
        public int MiembroCount { get; set; }
    }
}
