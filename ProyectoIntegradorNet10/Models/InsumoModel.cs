namespace ProyectoIntegradorNet10.Models
{
    public class InsumoModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? UnidadMedida { get; set; }
        public decimal? PrecioUnitario { get; set; }
        public decimal? CantidadStock { get; set; }

        public string PrecioDisplay => PrecioUnitario?.ToString("N2") ?? "0.00";
        public string StockDisplay => CantidadStock?.ToString("N0") ?? "0";
    }
}
