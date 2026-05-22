using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class VentasPagosUC : UserControl
    {
        private List<VentaModel> _ventas = new();
        private List<ProductoModel> _productos = new();
        private List<ClienteModel> _clientes = new();
        private List<ClienteModel> _filteredClientes = new();
        private List<VentaModel> _filteredVentasPagos = new(); // For pagos tab venta list
        private List<PagoModel> _pagos = new();
        private VentaModel? _selectedVenta;
        private PagoModel? _selectedPago;
        private bool _isEditing;
        private bool _isUpdatingForm;
        private bool _isNewPago;

        // Debounce timer for client search
        private DispatcherTimer? _clienteSearchTimer;

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
                dgVentas.ItemsSource = _ventas;
                UpdateEmptyState();

                _productos = await VentasService.GetAllProductos();
                cmbProducto.ItemsSource = _productos;

                _clientes = await VentasService.GetAllClientes();
                _filteredClientes = new List<ClienteModel>(_clientes);
                cmbCliente.ItemsSource = _filteredClientes;
                cmbCliente.Text = string.Empty;

                // Load ventas for the pagos tab
                ApplyPagosFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateEmptyState()
        {
            txtEmptyState.Visibility = (_ventas == null || _ventas.Count == 0)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ClearForm()
        {
            _isEditing = false;
            _selectedVenta = null;
            cmbCliente.SelectedIndex = -1;
            cmbCliente.Text = string.Empty;
            rbContado.IsChecked = true;
            txtMeses.Text = "1";
            txtDescuento.Text = "0";
            cmbProducto.SelectedIndex = -1;
            lstProductos.ItemsSource = null;
            txtTotal.Text = "0.00";
            btnEliminarVenta.IsEnabled = false;
            panelMeses.Visibility = Visibility.Collapsed;
            panelCantidad.Visibility = Visibility.Collapsed;
        }

        private void PopulateForm(VentaModel v)
        {
            _isEditing = true;
            _selectedVenta = v;

            // Set cliente
            if (!string.IsNullOrEmpty(v.ClienteCi))
                cmbCliente.SelectedValue = v.ClienteCi;
            else
                cmbCliente.SelectedIndex = -1;

            // Set tipo
            if (v.Tipo == "Plan de pago")
            {
                rbPlanPago.IsChecked = true;
                panelMeses.Visibility = Visibility.Visible;
                txtMeses.Text = v.Meses?.ToString() ?? "1";
            }
            else
            {
                rbContado.IsChecked = true;
                panelMeses.Visibility = Visibility.Collapsed;
                txtMeses.Text = "1";
            }

            // Set descuento
            txtDescuento.Text = v.PorcentajeDescuento?.ToString() ?? "0";

            // Load detalles
            lstProductos.ItemsSource = v.Detalles;
            UpdateTotalDisplay();
            btnEliminarVenta.IsEnabled = true;
        }

        private void UpdateTotalDisplay()
        {
            if (_selectedVenta != null)
            {
                txtTotal.Text = _selectedVenta.Total.ToString("N2");
            }
        }

        private async void dgVentas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgVentas.SelectedItem is VentaModel venta)
            {
                try
                {
                    // Load detalles for this venta
                    venta.Detalles = await VentasService.GetDetallesByVenta(venta.Id);
                    _selectedVenta = venta;
                    PopulateForm(venta);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar detalles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ──────────── TAB SWITCHING ────────────

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            // Null guard: this event fires during XAML parsing before InitializeComponent
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
                // Refresh the pagos tab venta list when switching to it
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
            // else "Todos" - no filter

            var filtered = _ventas.AsEnumerable();

            // Filter by search term (client name)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filtered = filtered.Where(v =>
                    (v.ClienteNombre?.ToLower().Contains(searchTerm) ?? false) ||
                    v.Id.ToString().Contains(searchTerm));
            }

            // Filter by tipo
            if (tipoFilter != null)
            {
                filtered = filtered.Where(v => v.Tipo == tipoFilter);
            }

            _filteredVentasPagos = filtered.ToList();
            dgVentasPagos.ItemsSource = _filteredVentasPagos;
            txtPagosEmpty.Visibility = (_filteredVentasPagos.Count == 0)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void txtPagoSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyPagosFilter();
        }

        private void FiltroTipo_Checked(object sender, RoutedEventArgs e)
        {
            // Null guard: this event fires during XAML parsing before InitializeComponent
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
                txtPagosEmpty.Visibility = Visibility.Visible;
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

                // Show "Generar Pagos" button only for Plan de pago with no pagos yet
                btnGenerarPagos.Visibility = (_selectedVenta.Tipo == "Plan de pago" && (_pagos == null || _pagos.Count == 0))
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                // Check if sum of Pagado pagos >= total → show "Marcar como Pagado" button
                CheckPagoSum();

                // Check if factura already exists for this venta
                var existingFactura = await FacturasService.GetByVentaId(_selectedVenta.Id);
                if (existingFactura != null)
                {
                    btnGenerarFactura.Visibility = Visibility.Collapsed;
                    txtFacturaInfo.Text = $"✅ Factura #{existingFactura.Id} emitida (Total: {existingFactura.TotalDisplay})";
                    txtFacturaInfo.Visibility = Visibility.Visible;
                }
                else
                {
                    // Only show "Generar Factura" button if the venta is fully paid
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

        /// <summary>
        /// Checks if the sum of all "Pagado" pagos meets or exceeds the venta total.
        /// If so, shows the "Marcar como Pagado" button (only if venta is not already Pagado).
        /// </summary>
        private void CheckPagoSum()
        {
            if (_selectedVenta == null || _pagos == null || _pagos.Count == 0)
            {
                btnMarcarPagado.Visibility = Visibility.Collapsed;
                return;
            }

            // Don't show if already Pagado
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

        // ──────────── TIPO CHECKED ────────────

        private void Tipo_Checked(object sender, RoutedEventArgs e)
        {
            // Null guard: this event fires during XAML parsing before InitializeComponent
            if (panelMeses == null) return;

            if (rbPlanPago.IsChecked == true)
            {
                panelMeses.Visibility = Visibility.Visible;
            }
            else
            {
                panelMeses.Visibility = Visibility.Collapsed;
            }
        }

        // ──────────── CLIENT SEARCH (EDITABLE COMBOBOX) ────────────

        private void cmbCliente_Loaded(object sender, RoutedEventArgs e)
        {
            // Find the internal TextBox of the editable ComboBox and subscribe to TextChanged
            if (cmbCliente.Template.FindName("PART_EditableTextBox", cmbCliente) is TextBox tb)
            {
                tb.TextChanged += (s, args) =>
                {
                    // Restart debounce timer on each keystroke
                    if (_clienteSearchTimer == null)
                    {
                        _clienteSearchTimer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(300)
                        };
                        _clienteSearchTimer.Tick += (timerSender, timerArgs) =>
                        {
                            _clienteSearchTimer.Stop();
                            ApplyClienteFilter();
                        };
                    }
                    else
                    {
                        _clienteSearchTimer.Stop();
                    }
                    _clienteSearchTimer.Start();
                };
            }
        }

        private void ApplyClienteFilter()
        {
            var term = cmbCliente.Text.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(term))
            {
                _filteredClientes = new List<ClienteModel>(_clientes);
            }
            else
            {
                _filteredClientes = _clientes
                    .Where(c => (c.Nombre?.ToLower().Contains(term) ?? false)
                             || (c.Apellido?.ToLower().Contains(term) ?? false)
                             || (c.Ci?.ToLower().Contains(term) ?? false)
                             || c.NombreCompleto.ToLower().Contains(term))
                    .ToList();
            }
            cmbCliente.ItemsSource = _filteredClientes;
            cmbCliente.IsDropDownOpen = _filteredClientes.Count > 0;
        }

        // ──────────── PRODUCTOS ────────────

        private void cmbProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProducto.SelectedItem is ProductoModel)
            {
                panelCantidad.Visibility = Visibility.Visible;
                txtCantidad.Text = "1";
            }
            else
            {
                panelCantidad.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnAgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProducto.SelectedItem is ProductoModel prod)
            {
                // Get quantity from input, default to 1
                int cantidad = 1;
                if (!string.IsNullOrWhiteSpace(txtCantidad.Text))
                {
                    int.TryParse(txtCantidad.Text, out cantidad);
                }
                if (cantidad <= 0) cantidad = 1;

                // Check if product already added
                var existing = _selectedVenta?.Detalles.FirstOrDefault(d => d.ProductoId == prod.Id);
                if (existing != null)
                {
                    existing.Cantidad = (existing.Cantidad ?? 0) + cantidad;
                    existing.PrecioUnitario = prod.PrecioVenta;
                }
                else
                {
                    if (_selectedVenta == null)
                    {
                        _selectedVenta = new VentaModel();
                    }
                    _selectedVenta.Detalles.Add(new VentaDetalleModel
                    {
                        ProductoId = prod.Id,
                        ProductoNombre = prod.Nombre,
                        Cantidad = cantidad,
                        PrecioUnitario = prod.PrecioVenta,
                    });
                }

                // Refresh list
                lstProductos.ItemsSource = null;
                if (_selectedVenta != null)
                    lstProductos.ItemsSource = _selectedVenta.Detalles;
                UpdateTotalDisplay();
                cmbProducto.SelectedIndex = -1;
                panelCantidad.Visibility = Visibility.Collapsed;
            }
        }

        private void lstProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstProductos.SelectedItem is VentaDetalleModel det)
            {
                panelCantidad.Visibility = Visibility.Visible;
                _isUpdatingForm = true;
                txtCantidad.Text = det.Cantidad?.ToString() ?? "1";
                _isUpdatingForm = false;
            }
            else
            {
                panelCantidad.Visibility = Visibility.Collapsed;
            }
        }

        private void txtCantidad_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingForm) return;
            if (lstProductos.SelectedItem is VentaDetalleModel det)
            {
                if (int.TryParse(txtCantidad.Text, out int cant) && cant > 0)
                {
                    det.Cantidad = cant;
                    lstProductos.ItemsSource = null;
                    lstProductos.ItemsSource = _selectedVenta?.Detalles;
                    UpdateTotalDisplay();
                }
            }
        }

        private void RemoveProducto_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock tb && tb.DataContext is VentaDetalleModel det)
            {
                _selectedVenta?.Detalles.Remove(det);
                lstProductos.ItemsSource = null;
                lstProductos.ItemsSource = _selectedVenta?.Detalles;
                UpdateTotalDisplay();
            }
        }

        private void CalcularTotal(object sender, TextChangedEventArgs e)
        {
            if (_selectedVenta != null)
            {
                if (decimal.TryParse(txtDescuento.Text, out decimal desc))
                {
                    _selectedVenta.PorcentajeDescuento = desc;
                }
                else
                {
                    _selectedVenta.PorcentajeDescuento = 0;
                }
                UpdateTotalDisplay();
            }
        }

        // ──────────── VENTA CRUD ────────────

        private async void BtnGuardarVenta_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            if (cmbCliente.SelectedValue == null)
            {
                MessageBox.Show("Seleccione un cliente.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedVenta == null || _selectedVenta.Detalles.Count == 0)
            {
                MessageBox.Show("Agregue al menos un producto.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _selectedVenta.ClienteCi = cmbCliente.SelectedValue.ToString();
                _selectedVenta.Fecha = DateTime.Today;
                _selectedVenta.Hora = DateTime.Now.TimeOfDay;
                _selectedVenta.Tipo = rbPlanPago.IsChecked == true ? "Plan de pago" : "Contado";
                _selectedVenta.Estado = "Pendiente";

                if (decimal.TryParse(txtDescuento.Text, out decimal desc))
                    _selectedVenta.PorcentajeDescuento = desc;
                else
                    _selectedVenta.PorcentajeDescuento = 0;

                if (int.TryParse(txtMeses.Text, out int meses) && meses > 0)
                    _selectedVenta.Meses = meses;
                else
                    _selectedVenta.Meses = 1;

                if (_isEditing && _selectedVenta.Id > 0)
                {
                    // Update existing venta
                    await VentasService.UpdateVenta(_selectedVenta);
                    await VentasService.ClearDetalles(_selectedVenta.Id);
                    foreach (var d in _selectedVenta.Detalles)
                    {
                        d.VentaId = _selectedVenta.Id;
                        await VentasService.InsertDetalle(d);
                    }
                    MessageBox.Show("Venta actualizada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Insert new venta
                    int newId = await VentasService.InsertVenta(_selectedVenta);
                    foreach (var d in _selectedVenta.Detalles)
                    {
                        d.VentaId = newId;
                        await VentasService.InsertDetalle(d);
                    }

                    // If Plan de pago, auto-generate pagos
                    if (_selectedVenta.Tipo == "Plan de pago" && _selectedVenta.Meses.HasValue && _selectedVenta.Meses.Value > 1)
                    {
                        await GenerarPagosPlan(newId, _selectedVenta.Total, _selectedVenta.Meses.Value);
                    }

                    MessageBox.Show("Venta creada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ClearForm();
                await LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar venta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    Monto = (i == meses - 1) ? montoPorMes + remainder : montoPorMes, // Last payment gets remainder
                    Metodo = "Efectivo",
                    Estado = "Pendiente",
                };
                await VentasService.InsertPago(pago);
            }
        }

        private async void BtnEliminarVenta_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVenta == null || _selectedVenta.Id <= 0) return;

            var result = MessageBox.Show($"¿Eliminar venta #{_selectedVenta.Id}?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                await VentasService.DeleteVenta(_selectedVenta.Id);
                MessageBox.Show("Venta eliminada.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                await LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelarVenta_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            dgVentas.SelectedItem = null;
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            _selectedVenta = new VentaModel();
            cmbCliente.Focus();
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string term = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(term))
            {
                await LoadData();
                return;
            }

            try
            {
                _ventas = await VentasService.SearchVentas(term);
                dgVentas.ItemsSource = _ventas;
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                // Set metodo
                foreach (ComboBoxItem item in cmbPagoMetodo.Items)
                {
                    if (item.Content.ToString() == pago.Metodo)
                    {
                        cmbPagoMetodo.SelectedItem = item;
                        break;
                    }
                }

                // Set estado
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
            cmbPagoMetodo.SelectedIndex = 0; // Default: Efectivo
            cmbPagoEstado.SelectedIndex = 0; // Default: Pendiente
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

            // Validate monto
            if (!decimal.TryParse(txtPagoMonto.Text, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Ingrese un monto válido.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_isNewPago)
                {
                    // Insert new pago
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
                    // Update existing pago
                    if (_selectedPago == null)
                    {
                        MessageBox.Show("Seleccione un pago para actualizar.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _selectedPago.Monto = monto;
                    _selectedPago.Metodo = (cmbPagoMetodo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Efectivo";
                    string nuevoEstado = (cmbPagoEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pendiente";

                    // If marking as Pagado, set current date/time
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

                // Refresh
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

        // ──────────── FACTURAS ────────────

        private async void BtnGenerarFactura_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVenta == null || _selectedVenta.Id <= 0) return;

            try
            {
                // Check if factura already exists
                var existing = await FacturasService.GetByVentaId(_selectedVenta.Id);
                if (existing != null)
                {
                    MessageBox.Show($"Ya existe una factura para esta venta (ID: {existing.Id}).",
                        "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Get cliente info for the factura
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

                // Refresh the UI
                LoadPagosForSelectedVenta();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar factura: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
