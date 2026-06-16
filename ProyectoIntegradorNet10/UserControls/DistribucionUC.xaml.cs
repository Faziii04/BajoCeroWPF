using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class DistribucionUC : UserControl
    {
        private List<VentaModel> _ventas = new();
        private string _activeEstado = "Pedido";

        public DistribucionUC()
        {
            InitializeComponent();
        }

        private async void DistribucionUC_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= DistribucionUC_Loaded;
            await LoadVentas();
        }

        private async System.Threading.Tasks.Task LoadVentas()
        {
            try
            {
                _ventas = await VentasService.GetVentasByEstado(_activeEstado);

                // Apply date filter client-side
                DateTime? desde = dpFechaDesde?.SelectedDate;
                DateTime? hasta = dpFechaHasta?.SelectedDate;

                var filtered = _ventas.AsEnumerable();
                if (desde.HasValue)
                    filtered = filtered.Where(v => v.FechaEntrega >= desde.Value);
                if (hasta.HasValue)
                    filtered = filtered.Where(v => v.FechaEntrega <= hasta.Value);

                var result = filtered.ToList();

                dgVentas.ItemsSource = result;
                int count = result.Count;
                int totalCount = _ventas.Count;
                txtEmptyState.Text = $"No hay ventas en estado \"{_activeEstado}\".";
                txtVentasCount.Text = count > 0
                    ? $"{count} venta{(count != 1 ? "s" : "")} en {_activeEstado}"
                    : "";
                panelEmptyState.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ventas: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ──────────── ESTADO FILTER CHIPS ────────────

        private void SetActiveChip(string estado)
        {
            _activeEstado = estado;

            var chips = new[] { chipPedido, chipEnRuta, chipIncidencia };
            foreach (var chip in chips)
            {
                chip.Background = TryFindResource("GridRowHoverBrush") as Brush
                    ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3F4B"));
                if (chip.Child is TextBlock tb)
                    tb.Foreground = TryFindResource("NavTextColor") as Brush ?? Brushes.White;
            }

            Border activeChip = estado switch
            {
                "Pedido" => chipPedido,
                "En ruta" => chipEnRuta,
                "Incidencia" => chipIncidencia,
                _ => chipPedido,
            };
            activeChip.Background = TryFindResource("AcentoBrush") as Brush
                ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6C63FF"));
            if (activeChip.Child is TextBlock activeTb)
                activeTb.Foreground = Brushes.White;
        }

        private async void ChipEstado_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string estado && estado != _activeEstado)
            {
                SetActiveChip(estado);
                await LoadVentas();
            }
        }

        // ──────────── DATE FILTERS ────────────

        private async void FiltroFecha_Changed(object sender, SelectionChangedEventArgs e)
        {
            await LoadVentas();
        }

        private async void BtnHoy_Click(object sender, RoutedEventArgs e)
        {
            dpFechaDesde.SelectedDate = DateTime.Today;
            dpFechaHasta.SelectedDate = DateTime.Today;
            await LoadVentas();
        }

        // ──────────── DATA GRID ────────────

        private void dgVentas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Preview only — all actions are in PWDistribucion
        }

        private void dgVentas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgVentas.SelectedItem is VentaModel venta)
                OpenPopup(venta);
        }

        private void OpenPopup(VentaModel venta)
        {
            var popup = new PopWindows.PWDistribucion
            {
                Owner = Window.GetWindow(this),
                EditVenta = venta
            };
            popup.OnDataChanged += async () => await LoadVentas();
            popup.ShowDialog();
        }

        // ──────────── TOOLBAR ────────────

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            await LoadVentas();
        }
    }
}
