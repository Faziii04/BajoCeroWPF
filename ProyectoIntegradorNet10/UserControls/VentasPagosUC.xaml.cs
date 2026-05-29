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
        private List<VentaModel> _filteredVentasPagos = new(); // For pagos tab venta list
        private List<PagoModel> _pagos = new();
        private VentaModel? _selectedVenta;
        private PagoModel? _selectedPago;
        private bool _isNewPago;

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
                ApplyPagosFilter();
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

        // ──────────── TAB SWITCHING ────────────

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (gridVentasTab == null || gridPagosTab == null) return;

            if (rbVentas.IsChecked == true)
            {
                gridVentasTab.Visibility = Visibility.Visible;
                gridPagosTab.Visibility = Visibility.Collapsed;
            }
            else
            {
                gridVentasTab.Visibility = Visibility.Collapsed;
                gridPagosTab.Visibility = Visibility.Visible;
                ApplyPagosFilter();
            }
        }

        // ──────────── PAGOS TAB: FILTERS ────────────

        private void ApplyPagosFilter()
        {
            string searchTerm = txtPagoSearch?.Text?.Trim().ToLower() ?? "";
            string? tipoFilter = null;

            if (rbFiltroContado?.IsChecked == true)
                tipoFilter = "Contado";
            else if (rbFiltroPlan?.IsChecked == true)
                tipoFilter = "Plan de pago";

            var filtered = _ventas.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filtered = filtered.Where(v =>
                    (v.ClienteNombre?.ToLower().Contains(searchTerm) ?? false) ||
                    v.Id.ToString().Contains(searchTerm));
            }

            if (tipoFilter != null)
            {
                filtered = filtered.Where(v => v.Tipo == tipoFilter);
            }

            _filteredVentasPagos = filtered.ToList();
            dgVentasPagos.ItemsSource = _filteredVentasPagos;
            if (txtPagosEmpty != null)
            {
                txtPagosEmpty.Visibility = (_filteredVentasPagos.Count == 0)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void txtPagoSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyPagosFilter();
        }

        private void FiltroTipo_Checked(object sender, RoutedEventArgs e)
        {
            if (dgVentasPagos == null) return;
            ApplyPagosFilter();
        }

        private async void dgVentasPagos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgVentasPagos.SelectedItem is VentaModel venta)
            {
                try
                {
                    venta.Detalles = await VentasService.GetDetallesByVenta(venta.Id);
                    _selectedVenta = venta;
                    LoadPagosForSelectedVenta();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar venta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnRefrescarPagos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ventas = await VentasService.GetAllVentas();
                ApplyPagosFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al refrescar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadPagosForSelectedVenta()
        {
            if (_selectedVenta == null)
            {
                txtPagoVentaInfo.Text = "Seleccione una venta";
                txtPagoVentaTotal.Text = "";
                dgPagos.ItemsSource = null;
                btnGenerarPagos.Visibility = Visibility.Collapsed;
                btnMarcarPagado.Visibility = Visibility.Collapsed;
                btnGenerarFactura.Visibility = Visibility.Collapsed;
                txtFacturaInfo.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                txtPagoVentaInfo.Text = $"Venta #{_selectedVenta.Id} - {_selectedVenta.ClienteNombre}";
                txtPagoVentaTotal.Text = $"Total: {_selectedVenta.Total:N2} | Tipo: {_selectedVenta.Tipo} | Estado: {_selectedVenta.Estado}";

                _pagos = await VentasService.GetPagosByVenta(_selectedVenta.Id);
                dgPagos.ItemsSource = _pagos;

                btnGenerarPagos.Visibility = (_selectedVenta.Tipo == "Plan de pago" && (_pagos == null || _pagos.Count == 0))
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                CheckPagoSum();

                var existingFactura = await FacturasService.GetByVentaId(_selectedVenta.Id);
                if (existingFactura != null)
                {
                    btnGenerarFactura.Visibility = Visibility.Collapsed;
                    txtFacturaInfo.Text = $"✅ Factura #{existingFactura.Id} emitida (Total: {existingFactura.TotalDisplay})";
                    txtFacturaInfo.Visibility = Visibility.Visible;
                }
                else
                {
                    btnGenerarFactura.Visibility = (_selectedVenta.Estado == "Pagado")
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                    txtFacturaInfo.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar pagos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckPagoSum()
        {
            if (_selectedVenta == null || _pagos == null || _pagos.Count == 0)
            {
                btnMarcarPagado.Visibility = Visibility.Collapsed;
                return;
            }

            if (_selectedVenta.Estado == "Pagado")
            {
                btnMarcarPagado.Visibility = Visibility.Collapsed;
                return;
            }

            decimal totalPagado = 0;
            foreach (var p in _pagos)
            {
                if (p.Estado == "Pagado")
                    totalPagado += p.Monto;
            }

            btnMarcarPagado.Visibility = (totalPagado >= _selectedVenta.Total)
                ? Visibility.Visible
                : Visibility.Collapsed;
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

        // ──────────── PAGOS ────────────

        private void ClearPagoForm()
        {
            _isNewPago = false;
            _selectedPago = null;
            dgPagos.SelectedItem = null;
            txtPagoSelectedInfo.Text = "Seleccione un pago para gestionar";
            txtPagoMonto.Text = "";
            cmbPagoMetodo.SelectedIndex = -1;
            cmbPagoEstado.SelectedIndex = -1;
            txtBtnGuardarPago.Text = "Guardar Pago";
        }

        private void dgPagos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPagos.SelectedItem is PagoModel pago)
            {
                _isNewPago = false;
                _selectedPago = pago;
                txtPagoSelectedInfo.Text = $"Pago #{pago.PagoId} - {pago.MontoDisplay} - {pago.FechaDisplay}";
                txtPagoMonto.Text = pago.MontoDisplay;
                txtBtnGuardarPago.Text = "Actualizar Pago";

                foreach (ComboBoxItem item in cmbPagoMetodo.Items)
                {
                    if (item.Content.ToString() == pago.Metodo)
                    {
                        cmbPagoMetodo.SelectedItem = item;
                        break;
                    }
                }

                foreach (ComboBoxItem item in cmbPagoEstado.Items)
                {
                    if (item.Content.ToString() == pago.Estado)
                    {
                        cmbPagoEstado.SelectedItem = item;
                        break;
                    }
                }
            }
            else if (!_isNewPago)
            {
                ClearPagoForm();
            }
        }

        private void BtnNuevoPago_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVenta == null || _selectedVenta.Id <= 0)
            {
                MessageBox.Show("Seleccione una venta primero.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _isNewPago = true;
            _selectedPago = null;
            dgPagos.SelectedItem = null;
            txtPagoSelectedInfo.Text = $"Nuevo pago para Venta #{_selectedVenta.Id}";
            txtPagoMonto.Text = "";
            cmbPagoMetodo.SelectedIndex = 0;
            cmbPagoEstado.SelectedIndex = 0;
            txtBtnGuardarPago.Text = "Guardar Pago";
            txtPagoMonto.Focus();
        }

        private async void BtnGuardarPago_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVenta == null || _selectedVenta.Id <= 0)
            {
                MessageBox.Show("Seleccione una venta primero.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtPagoMonto.Text, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Ingrese un monto válido.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_isNewPago)
                {
                    string estado = (cmbPagoEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pendiente";
                    var nuevoPago = new PagoModel
                    {
                        VentaId = _selectedVenta.Id,
                        Fecha = estado == "Pagado" ? DateTime.Today : DateTime.Today,
                        Hora = estado == "Pagado" ? DateTime.Now.TimeOfDay : new TimeSpan(0, 0, 0),
                        Monto = monto,
                        Metodo = (cmbPagoMetodo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Efectivo",
                        Estado = estado,
                    };

                    await VentasService.InsertPago(nuevoPago);
                    await UpdateVentaEstadoFromPagos(_selectedVenta.Id);
                    MessageBox.Show("Pago creado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    if (_selectedPago == null)
                    {
                        MessageBox.Show("Seleccione un pago para actualizar.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _selectedPago.Monto = monto;
                    _selectedPago.Metodo = (cmbPagoMetodo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Efectivo";
                    string nuevoEstado = (cmbPagoEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pendiente";

                    if (nuevoEstado == "Pagado" && _selectedPago.Estado != "Pagado")
                    {
                        _selectedPago.Fecha = DateTime.Today;
                        _selectedPago.Hora = DateTime.Now.TimeOfDay;
                    }

                    _selectedPago.Estado = nuevoEstado;

                    await VentasService.UpdatePago(_selectedPago);
                    await UpdateVentaEstadoFromPagos(_selectedPago.VentaId);
                    MessageBox.Show("Pago actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ClearPagoForm();
                LoadPagosForSelectedVenta();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar pago: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task UpdateVentaEstadoFromPagos(int ventaId)
        {
            var pagos = await VentasService.GetPagosByVenta(ventaId);
            if (pagos == null || pagos.Count == 0) return;

            bool allPagados = pagos.All(p => p.Estado == "Pagado");
            bool anyVencido = pagos.Any(p => p.Estado == "Vencido");

            var venta = await VentasService.GetVentaById(ventaId);
            if (venta == null) return;

            if (allPagados)
                venta.Estado = "Pagado";
            else if (anyVencido)
                venta.Estado = "Vencido";
            else
                venta.Estado = "Pendiente";

            await VentasService.UpdateVenta(venta);
        }

        private async void BtnEliminarPago_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPago == null) return;

            var result = MessageBox.Show($"¿Eliminar pago #{_selectedPago.PagoId}?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                await VentasService.DeletePago(_selectedPago.PagoId);
                MessageBox.Show("Pago eliminado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                _selectedPago = null;
                LoadPagosForSelectedVenta();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar pago: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnMarcarPagado_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVenta == null || _selectedVenta.Id <= 0) return;

            var result = MessageBox.Show(
                $"¿Marcar venta #{_selectedVenta.Id} como Pagado?\n" +
                $"Los pagos suman el total requerido.",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _selectedVenta.Estado = "Pagado";
                await VentasService.UpdateVenta(_selectedVenta);
                MessageBox.Show("Venta marcada como Pagado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadPagosForSelectedVenta();
                await LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar venta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGenerarPagos_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVenta == null || _selectedVenta.Id <= 0) return;
            if (_selectedVenta.Tipo != "Plan de pago") return;

            int meses = _selectedVenta.Meses ?? 1;
            if (meses <= 1)
            {
                MessageBox.Show("El plan de pago requiere más de 1 mes.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                await GenerarPagosPlan(_selectedVenta.Id, _selectedVenta.Total, meses);
                MessageBox.Show($"Se generaron {meses} pagos para el plan.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadPagosForSelectedVenta();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar pagos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task GenerarPagosPlan(int ventaId, decimal total, int meses)
        {
            decimal montoPorMes = Math.Round(total / meses, 2);
            decimal remainder = total - (montoPorMes * meses);
            DateTime baseDate = DateTime.Today;

            for (int i = 0; i < meses; i++)
            {
                var pago = new PagoModel
                {
                    VentaId = ventaId,
                    Fecha = baseDate.AddMonths(i + 1),
                    Hora = new TimeSpan(0, 0, 0),
                    Monto = (i == meses - 1) ? montoPorMes + remainder : montoPorMes,
                    Metodo = "Efectivo",
                    Estado = "Pendiente",
                };
                await VentasService.InsertPago(pago);
            }
        }

        // ──────────── FACTURAS ────────────

        private async void BtnGenerarFactura_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVenta == null || _selectedVenta.Id <= 0) return;

            try
            {
                var existing = await FacturasService.GetByVentaId(_selectedVenta.Id);
                if (existing != null)
                {
                    MessageBox.Show($"Ya existe una factura para esta venta (ID: {existing.Id}).",
                        "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string? nombreCompleto = null;
                string? nit = null;
                if (!string.IsNullOrEmpty(_selectedVenta.ClienteCi))
                {
                    var cliente = await ClientesService.GetByCi(_selectedVenta.ClienteCi);
                    if (cliente != null)
                    {
                        nombreCompleto = cliente.NombreCompleto;
                        nit = cliente.Nit;
                    }
                }

                var factura = new FacturaModel
                {
                    VentaId = _selectedVenta.Id,
                    Subtotal = _selectedVenta.Total,
                    Total = _selectedVenta.Total,
                    Descuento = _selectedVenta.PorcentajeDescuento,
                    FechaEmision = DateTime.Now,
                    NombreCompleto = nombreCompleto,
                    Nit = nit,
                    DescuentoTipo = _selectedVenta.PorcentajeDescuento.HasValue && _selectedVenta.PorcentajeDescuento.Value > 0 ? "Porcentaje" : null,
                };

                await FacturasService.Insert(factura);
                MessageBox.Show($"Factura generada correctamente para Venta #{_selectedVenta.Id}.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadPagosForSelectedVenta();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar factura: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
