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
        private List<RepartidorModel> _repartidores = new();
        private string _activeEstado = "Pedido";
        private bool _filterSinRepartidor = false;

        public DistribucionUC()
        {
            InitializeComponent();
        }

        private async void DistribucionUC_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= DistribucionUC_Loaded;
            await LoadRepartidores();
            await LoadVentas();
        }

        private async System.Threading.Tasks.Task LoadRepartidores()
        {
            try
            {
                _repartidores = await RepartidorService.GetAll();

                cmbFiltroRepartidor.Items.Clear();
                cmbFiltroRepartidor.Items.Add(new ComboBoxItem
                {
                    Content = "Todos los repartidores",
                    Tag = -1,
                    IsSelected = true
                });

                foreach (var r in _repartidores)
                {
                    cmbFiltroRepartidor.Items.Add(new ComboBoxItem
                    {
                        Content = r.EmpleadoNombre ?? $"Repartidor #{r.Id}",
                        Tag = r.Id
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar repartidores: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadVentas()
        {
            try
            {
                _ventas = await VentasService.GetVentasByEstado(_activeEstado);

                DateTime? desde = dpFechaDesde?.SelectedDate;
                DateTime? hasta = dpFechaHasta?.SelectedDate;
                string? clienteText = txtBuscarCliente?.Text?.Trim().ToLower();
                int? repartidorFilterId = GetSelectedRepartidorId();

                var filtered = _ventas.AsEnumerable();

                // Date range filter
                if (desde.HasValue)
                    filtered = filtered.Where(v => v.FechaEntrega >= desde.Value);
                if (hasta.HasValue)
                    filtered = filtered.Where(v => v.FechaEntrega <= hasta.Value);

                // Sin repartidor filter
                if (_filterSinRepartidor)
                    filtered = filtered.Where(v => v.RepartidorId == null);

                // Cliente text search
                if (!string.IsNullOrEmpty(clienteText))
                    filtered = filtered.Where(v =>
                        v.ClienteNombre != null &&
                        v.ClienteNombre.ToLower().Contains(clienteText));

                // Repartidor filter
                if (repartidorFilterId.HasValue)
                    filtered = filtered.Where(v => v.RepartidorId == repartidorFilterId.Value);

                var result = filtered.ToList();

                dgVentas.ItemsSource = result;
                int count = result.Count;

                string estadoLabel = _filterSinRepartidor
                    ? "sin repartidor"
                    : $"\"{_activeEstado}\"";

                txtEmptyState.Text = $"No hay ventas en estado {estadoLabel}.";
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

        /// <summary>Returns the selected repartidor ID, or null if "Todos" is selected.</summary>
        private int? GetSelectedRepartidorId()
        {
            if (cmbFiltroRepartidor?.SelectedItem is ComboBoxItem item && item.Tag is int id && id > 0)
                return id;
            return null;
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

        // ──────────── ADVANCED FILTERS ────────────

        /// <summary>Toggles the "Sin repartidor" chip on/off.</summary>
        private async void ChipSinRepartidor_Click(object sender, MouseButtonEventArgs e)
        {
            _filterSinRepartidor = !_filterSinRepartidor;

            Brush activeBg = TryFindResource("AcentoBrush") as Brush
                ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6C63FF"));
            Brush inactiveBg = TryFindResource("GridRowHoverBrush") as Brush
                ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3F4B"));

            chipSinRepartidor.Background = _filterSinRepartidor ? activeBg : inactiveBg;
            iconSinRepartidor.Text = _filterSinRepartidor ? "☑" : "◻";
            iconSinRepartidor.Foreground = _filterSinRepartidor ? Brushes.White : (TryFindResource("NavTextColor") as Brush ?? Brushes.White);
            if (chipSinRepartidor.Child is StackPanel sp && sp.Children[1] is TextBlock tb)
                tb.Foreground = _filterSinRepartidor ? Brushes.White : (TryFindResource("NavTextColor") as Brush ?? Brushes.White);

            await LoadVentas();
        }

        /// <summary>Filters by client name as the user types.</summary>
        private async void FiltroCliente_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadVentas();
        }

        /// <summary>Filters by selected repartidor.</summary>
        private async void FiltroRepartidor_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Ignore initial population
            if (cmbFiltroRepartidor.IsLoaded)
                await LoadVentas();
        }

        // ──────────── SINGLE-CLICK → OPEN POPUP ────────────

        private void dgVentas_Seleccionado(object sender, SelectionChangedEventArgs e)
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
