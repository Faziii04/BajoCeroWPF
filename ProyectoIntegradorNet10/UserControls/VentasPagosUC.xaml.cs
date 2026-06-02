using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.PopWindows;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class VentasPagosUC : UserControl
    {
        private List<VentaModel> _ventas = new();
        private List<VentaModel> _filteredVentas = new();
        private VentaModel? _selectedVenta;

        public VentasPagosUC()
        {
            InitializeComponent();
        }

        private async void VentasPagosUC_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            try
            {
                _ventas = await VentasService.GetAllVentas();
                ApplyVentasFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ──────────── VENTAS TAB: FILTERS ────────────

        private void ApplyVentasFilter()
        {
            // Guard against calls during XAML initialization
            if (dgVentas == null) return;

            string searchTerm = txtBuscar?.Text?.Trim().ToLower() ?? "";

            var filtered = _ventas.AsEnumerable();

            // Text search
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filtered = filtered.Where(v =>
                    (v.ClienteNombre?.ToLower().Contains(searchTerm) ?? false) ||
                    v.Id.ToString().Contains(searchTerm));
            }

            // Date range filter
            if (dpFechaDesde?.SelectedDate != null)
            {
                var desde = dpFechaDesde.SelectedDate.Value;
                filtered = filtered.Where(v => v.Fecha >= desde);
            }
            if (dpFechaHasta?.SelectedDate != null)
            {
                var hasta = dpFechaHasta.SelectedDate.Value.AddDays(1);
                filtered = filtered.Where(v => v.Fecha < hasta);
            }

            // Tipo filter
            if (cmbFiltroTipo?.SelectedItem is ComboBoxItem tipoItem && tipoItem.Content.ToString() != "Todos")
            {
                string tipo = tipoItem.Content.ToString();
                filtered = filtered.Where(v => v.Tipo == tipo);
            }

            _filteredVentas = filtered.ToList();
            dgVentas.ItemsSource = _filteredVentas;
            UpdateEmptyState();
        }

        private void Filtro_Changed(object sender, RoutedEventArgs e)
        {
            ApplyVentasFilter();
        }

        private void UpdateEmptyState()
        {
            if (txtEmptyState != null)
            {
                txtEmptyState.Visibility = (_filteredVentas == null || _filteredVentas.Count == 0)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        // ──────────── VENTAS TAB: SELECTION & DOUBLE-CLICK ────────────

        private async void dgVentas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgVentas.SelectedItem is VentaModel venta)
            {
                try
                {
                    venta.Detalles = await VentasService.GetDetallesByVenta(venta.Id);
                    _selectedVenta = venta;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar detalles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void dgVentas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgVentas.SelectedItem is VentaModel venta)
            {
                try
                {
                    // Load full venta with detalles
                    venta.Detalles = await VentasService.GetDetallesByVenta(venta.Id);
                    var fullVenta = await VentasService.GetVentaById(venta.Id);
                    if (fullVenta != null)
                    {
                        fullVenta.Detalles = venta.Detalles;
                        fullVenta.Meses = venta.Meses;

                        var popup = new PWVentas
                        {
                            Owner = Window.GetWindow(this),
                            EditVenta = fullVenta
                        };

                        popup.OnDataChanged += async () =>
                        {
                            await LoadData();
                        };

                        popup.ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar venta: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ──────────── NUEVA VENTA (POPUP) ────────────

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var popup = new PWVentas
            {
                Owner = Window.GetWindow(this)
            };

            popup.OnDataChanged += async () =>
            {
                await LoadData();
            };

            popup.ShowDialog();
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyVentasFilter();
        }
    }
}
