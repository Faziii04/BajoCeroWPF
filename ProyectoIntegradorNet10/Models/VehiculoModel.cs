using System;
using System.Windows.Media;

namespace ProyectoIntegradorNet10.Models
{
    public class VehiculoModel
    {
        public string Placa { get; set; } = string.Empty;
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? Tipo { get; set; }
        public decimal? Kilometraje { get; set; }
        public DateTime? SoatVencimiento { get; set; }
        public DateTime? UltimaActualizacion { get; set; }

        // ── Computed display properties ──

        public string MarcaModelo => $"{Marca ?? ""} {Modelo ?? ""}".Trim();

        public string KilometrajeDisplay
            => Kilometraje.HasValue ? $"{Kilometraje.Value:N0} km" : "—";

        public string SoatVencimientoDisplay
            => SoatVencimiento.HasValue ? SoatVencimiento.Value.ToString("dd/MM/yyyy") : "Sin registro";

        /// <summary>
        /// Returns "Vigente", "Por vencer", or "Vencido" based on SoatVencimiento.
        /// </summary>
        public string SoatEstado
        {
            get
            {
                if (!SoatVencimiento.HasValue)
                    return "Vencido";

                var hoy = DateTime.Today;
                var diff = (SoatVencimiento.Value.Date - hoy).Days;

                if (diff < 0)
                    return "Vencido";
                if (diff <= 30)
                    return "Por vencer";
                return "Vigente";
            }
        }

        /// <summary>
        /// Foreground brush for the SOAT status badge.
        /// </summary>
        public Brush SoatColorBrush
        {
            get
            {
                // These brushes are defined in DarkTheme.xaml / LightTheme.xaml
                // as DynamicResource; but for computed properties we return fallback colors.
                // The XAML bindings will use converters or triggers instead.
                return SoatEstado switch
                {
                    "Vigente" => new SolidColorBrush(Color.FromRgb(46, 204, 113)),    // green
                    "Por vencer" => new SolidColorBrush(Color.FromRgb(241, 196, 15)),  // amber
                    _ => new SolidColorBrush(Color.FromRgb(231, 76, 60)),              // red
                };
            }
        }

        /// <summary>
        /// Background brush for the SOAT status badge.
        /// </summary>
        public Brush SoatBadgeBgBrush
        {
            get
            {
                return SoatEstado switch
                {
                    "Vigente" => new SolidColorBrush(Color.FromArgb(40, 46, 204, 113)),
                    "Por vencer" => new SolidColorBrush(Color.FromArgb(40, 241, 196, 15)),
                    _ => new SolidColorBrush(Color.FromArgb(40, 231, 76, 60)),
                };
            }
        }
    }
}
