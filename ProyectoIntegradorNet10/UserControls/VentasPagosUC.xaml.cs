using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
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
        private List<ClienteModel> _allClientes = new();
        private List<ClienteModel> _filteredClientes = new();
        private DispatcherTimer? _clienteFilterTimer;

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

                // Load clients for the filter ComboBox
                _allClientes = await ClientesService.GetAll();
                _filteredClientes = new List<ClienteModel>(_allClientes);
                PopulateClienteFilter();

                ApplyVentasFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ──────────── CLIENTE FILTER COMBOBOX ────────────

        private void PopulateClienteFilter()
        {
            if (cmbFiltroCliente == null) return;

            // Keep the "Todos los clientes" item at index 0, then add clients
            var items = new List<object> { "Todos los clientes" };
            items.AddRange(_filteredClientes);
            cmbFiltroCliente.ItemsSource = items;
            cmbFiltroCliente.SelectedIndex = 0;
        }

        private void CmbFiltroCliente_Loaded(object sender, RoutedEventArgs e)
        {
            // Wire up TextChanged on the editable TextBox inside the ComboBox template
            if (cmbFiltroCliente.Template.FindName("PART_EditableTextBox", cmbFiltroCliente) is TextBox tb)
            {
                tb.TextChanged += (s, args) =>
                {
                    if (_clienteFilterTimer == null)
                    {
                        _clienteFilterTimer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(300)
                        };
                        _clienteFilterTimer.Tick += (timerSender, timerArgs) =>
                        {
                            _clienteFilterTimer.Stop();
                            ApplyClienteFilterDropdown();
                        };
                    }
                    else
                    {
                        _clienteFilterTimer.Stop();
                    }
                    _clienteFilterTimer.Start();
                };
            }
        }

        private void ApplyClienteFilterDropdown()
        {
            if (cmbFiltroCliente == null || _allClientes == null) return;

            string term = cmbFiltroCliente.Text?.Trim().ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(term) || term == "todos los clientes")
            {
                _filteredClientes = new List<ClienteModel>(_allClientes);
            }
            else
            {
                _filteredClientes = _allClientes
                    .Where(c => (c.NombreCompleto?.ToLower().Contains(term) ?? false)
                             || (c.Ci?.ToLower().Contains(term) ?? false))
                    .ToList();
            }

            // Preserve "Todos los clientes" at top
            var items = new List<object> { "Todos los clientes" };
            items.AddRange(_filteredClientes);
            cmbFiltroCliente.ItemsSource = items;
            cmbFiltroCliente.IsDropDownOpen = _filteredClientes.Count > 0;
        }

        // ──────────── VENTAS TAB: FILTERS ────────────

        private void ApplyVentasFilter()
        {
            // Guard against calls during XAML initialization
            if (dgVentas == null) return;

            var filtered = _ventas.AsEnumerable();

            // Cliente filter
            if (cmbFiltroCliente?.SelectedItem != null)
            {
                string? selectedClientCi = null;

                if (cmbFiltroCliente.SelectedItem is ClienteModel cliente)
                {
                    selectedClientCi = cliente.Ci;
                }

                if (selectedClientCi != null)
                {
                    filtered = filtered.Where(v => v.ClienteCi == selectedClientCi);
                }
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
                string? tipo = tipoItem.Content.ToString();
                if (!string.IsNullOrEmpty(tipo))
                {
                    filtered = filtered.Where(v => v.Tipo == tipo);
                }
            }

            // Estado (delivery status) filter
            if (cmbFiltroEstado?.SelectedItem is ComboBoxItem estadoItem && estadoItem.Content.ToString() != "Todos")
            {
                string? estado = estadoItem.Content.ToString();
                if (!string.IsNullOrEmpty(estado))
                {
                    filtered = filtered.Where(v => string.Equals(v.Estado, estado, StringComparison.OrdinalIgnoreCase));
                }
            }

            // Delivery filter
            if (cmbFiltroDelivery?.SelectedItem is ComboBoxItem deliveryItem && deliveryItem.Content.ToString() != "Todos")
            {
                bool deliveryVal = deliveryItem.Content.ToString() == "Sí";
                filtered = filtered.Where(v => v.Delivery == deliveryVal);
            }

            // Pagado filter
            if (cmbFiltroPagado?.SelectedItem is ComboBoxItem pagadoItem && pagadoItem.Content.ToString() != "Todos")
            {
                bool pagadoVal = pagadoItem.Content.ToString() == "Sí";
                filtered = filtered.Where(v => v.Pagado == pagadoVal);
            }

            // Entregado filter
            if (cmbFiltroEntregado?.SelectedItem is ComboBoxItem entregadoItem && entregadoItem.Content.ToString() != "Todos")
            {
                bool entregadoVal = entregadoItem.Content.ToString() == "Sí";
                filtered = filtered.Where(v => v.Entregado == entregadoVal);
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

        // ──────────── HOY (TODAY) BUTTON ────────────

        private void BtnHoy_Click(object sender, RoutedEventArgs e)
        {
            if (dpFechaDesde != null)
                dpFechaDesde.SelectedDate = DateTime.Today;
            if (dpFechaHasta != null)
                dpFechaHasta.SelectedDate = DateTime.Today;
        }
    }
}
