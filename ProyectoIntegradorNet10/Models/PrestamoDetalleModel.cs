namespace ProyectoIntegradorNet10.Models
{
    public class PrestamoDetalleModel
    {
        public string ClienteCi { get; set; } = string.Empty;
        public int ProductoId { get; set; }
        public int PrestamoId { get; set; }
        public int? Cantidad { get; set; }
        public decimal? ValorReposicion { get; set; }

        // ─── Display helpers (joined from DB, not direct columns) ───
        public string? ProductoNombre { get; set; }
        public decimal? ProductoPrecio { get; set; }

        public string SubtotalDisplay => (Cantidad.GetValueOrDefault() * ValorReposicion.GetValueOrDefault()).ToString("N2");
    }
}
