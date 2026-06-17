namespace ProyectoIntegradorNet10.Models
{
    public class DetalleOrdenModel
    {
        public int OrdenId { get; set; }
        public int InsumoId { get; set; }
        public decimal? Cantidad { get; set; }

        // Display helpers
        public string? InsumoNombre { get; set; }
        public decimal? InsumoPrecio { get; set; }
        public string SubtotalDisplay => ((Cantidad.GetValueOrDefault() * InsumoPrecio.GetValueOrDefault())).ToString("N2");
    }
}
