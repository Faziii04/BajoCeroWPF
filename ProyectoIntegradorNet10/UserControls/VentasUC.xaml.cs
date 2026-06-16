using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class VentasUC : UserControl
    {
        private List<ProductoModel> _allProductos = new();
        private List<ProductoModel> _filteredProductos = new();
        private List<ClienteModel> _clientes = new();
        private List<ClienteModel> _filteredClientes = new();
        private List<VentaDetalleModel> _detalles = new();
        private List<FamiliaModel> _familiasVenta = new();
        private int? _activeFamiliaFilterVenta;
        private string _familiaSearchTermVenta = "";
        private ProductoModel? _selectedProducto;
        private DispatcherTimer? _clienteSearchTimer;
        private bool _isReadOnly;

        /// <summary>
        /// If set, opens in edit mode for this venta.
        /// </summary>
        public VentaModel? EditVenta { get; set; }

        /// <summary>
        /// Raised when data changes so the parent can refresh.
        /// </summary>
        public event Action? OnDataChanged;

        public VentasUC()
        {
            InitializeComponent();
            this.Loaded += VentasUC_Loaded;
        }

        private async void VentasUC_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= VentasUC_Loaded;
            await LoadData();
            SetupClienteSearch();

            if (EditVenta != null)
            {
                PopulateForm(EditVenta);
            }
        }

        public async System.Threading.Tasks.Task RefreshData()
        {
            await LoadData();
            if (EditVenta != null)
            {
                // Refresh detalles from DB
                try
                {
                    EditVenta.Detalles = await VentasService.GetDetallesByVenta(EditVenta.Id);
                    _detalles = EditVenta.Detalles.ToList();
                    RefreshProductList();
                }
                catch { }
            }
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            try
            {
                _allProductos = (await VentasService.GetAllProductos())
                    .Where(p => string.Equals(p.Estado, "Activo", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                _filteredProductos = new List<ProductoModel>(_allProductos);
                icProductos.ItemsSource = _filteredProductos;

                // Load families for filter
                try
                {
                    _familiasVenta = await ProductoFamiliaService.GetAll();
                }
                catch { _familiasVenta = new(); }
                RenderFamiliaChipsVenta();

                _clientes = await VentasService.GetAllClientes();
                _filteredClientes = new List<ClienteModel>(_clientes);
                cmbCliente.ItemsSource = _clientes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupClienteSearch()
        {
            if (cmbCliente.Template.FindName("PART_EditableTextBox", cmbCliente) is TextBox tb)
            {
                tb.TextChanged += (s, args) =>
                {
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
            // Get text directly from the editable TextBox to avoid ComboBox auto-complete interference
            string term = "";
            if (cmbCliente.Template.FindName("PART_EditableTextBox", cmbCliente) is TextBox tb)
                term = tb.Text.Trim().ToLower();
            else
                term = cmbCliente.Text.Trim().ToLower();

            // Use ICollectionView filtering so we don't replace ItemsSource
            // and lose the selection state
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(cmbCliente.ItemsSource);
            if (view != null)
            {
                view.Filter = item =>
                {
                    if (string.IsNullOrWhiteSpace(term))
                        return true;

                    if (item is ClienteModel c)
                    {
                        return (c.Nombre?.ToLower().Contains(term) ?? false)
                            || (c.Apellido?.ToLower().Contains(term) ?? false)
                            || (c.Ci?.ToLower().Contains(term) ?? false)
                            || c.NombreCompleto.ToLower().Contains(term);
                    }
                    return true;
                };
            }
        }

        private void CmbCliente_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // When an item is selected in the dropdown, ensure the Text is synced
            // (DisplayMemberPath handles this, but be explicit for robustness)
            if (cmbCliente.SelectedItem is ClienteModel cliente)
            {
                cmbCliente.Text = cliente.NombreCompleto;
            }
        }

        // ──────────── POPULATE FORM (EDIT MODE) ────────────

        public void PopulateForm(VentaModel venta)
        {
            // Read-only lock when paid (but Pagado checkbox stays enabled to allow toggling)
            _isReadOnly = venta.Pagado;

            if (_isReadOnly)
            {
                SetReadOnlyMode(true);
            }

            // Set cliente
            if (!string.IsNullOrEmpty(venta.ClienteCi))
            {
                cmbCliente.SelectedValue = venta.ClienteCi;
                cmbCliente.Text = venta.ClienteNombre ?? "";
            }

            // Set tipo
            if (string.Equals(venta.Tipo, "Plan de pago", StringComparison.OrdinalIgnoreCase))
            {
                rbPlanPago.IsChecked = true;
                panelMeses.Visibility = Visibility.Visible;
                txtMeses.Text = venta.Meses?.ToString() ?? "1";
            }
            else
            {
                rbContado.IsChecked = true;
                panelMeses.Visibility = Visibility.Collapsed;
                txtMeses.Text = "1";
            }

            // Set descuento
            txtDescuento.Text = venta.PorcentajeDescuento?.ToString() ?? "0";

            // Set NIT
            txtNit.Text = venta.Nit ?? "";

            // Set Fecha Entrega / Hora Entrega
            if (venta.FechaEntrega.HasValue)
                dpFechaEntrega.SelectedDate = venta.FechaEntrega.Value;
            else
                dpFechaEntrega.SelectedDate = null;
            txtHoraEntrega.Text = venta.HoraEntrega?.ToString(@"hh\:mm") ?? "";

            // Set estado (delivery status)
            string estado = venta.Estado ?? "Pedido";
            foreach (ComboBoxItem item in cmbEstado.Items)
            {
                if (string.Equals(item.Content.ToString(), estado, StringComparison.OrdinalIgnoreCase))
                {
                    cmbEstado.SelectedItem = item;
                    break;
                }
            }

            // Set pagado
            chkPagado.IsChecked = venta.Pagado;

            // Set delivery
            chkDelivery.IsChecked = venta.Delivery;

            // Set entregado
            chkEntregado.IsChecked = venta.Entregado;

            // Update "Marcar Completado" button visibility
            UpdateMarcarCompletadoVisibility();

            // Load detalles
            _detalles = venta.Detalles.ToList();
            RefreshProductList();

            // Hide product gallery in edit mode
            if (_isReadOnly)
            {
                var scrollViewer = icProductos.Parent as ScrollViewer;
                if (scrollViewer != null) scrollViewer.Visibility = Visibility.Collapsed;
                txtBuscarProducto.Visibility = Visibility.Collapsed;
            }
        }

        private void SetReadOnlyMode(bool readOnly)
        {
            cmbCliente.IsEnabled = !readOnly;
            rbContado.IsEnabled = !readOnly;
            rbPlanPago.IsEnabled = !readOnly;
            txtMeses.IsReadOnly = readOnly;
            txtDescuento.IsReadOnly = readOnly;
            txtCantidad.IsReadOnly = readOnly;
            txtNit.IsReadOnly = readOnly;
            cmbEstado.IsEnabled = !readOnly;
            chkPagado.IsEnabled = true;     // Always allow toggling Pagado
            chkDelivery.IsEnabled = !readOnly;
            chkEntregado.IsEnabled = !readOnly;
            btnAgregarProducto.Visibility = readOnly ? Visibility.Collapsed : Visibility.Collapsed;
            btnGuardar.Visibility = Visibility.Visible;  // Always visible to allow saving Pagado changes
        }

        // ──────────── PRODUCT GALLERY ────────────

        private void txtBuscarProducto_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isReadOnly) return;
            var term = txtBuscarProducto.Text.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(term))
            {
                _filteredProductos = new List<ProductoModel>(_allProductos);
            }
            else
            {
                _filteredProductos = _allProductos
                    .Where(p => p.Nombre.ToLower().Contains(term))
                    .ToList();
            }

            // Apply family filter if active
            ApplyFamiliaFilterVenta();

            icProductos.ItemsSource = _filteredProductos;
        }

        // ════════════════════════════════════════════════════════════════
        // FAMILY FILTER METHODS
        // ════════════════════════════════════════════════════════════════

        private void RenderFamiliaChipsVenta()
        {
            // Remove old chips (keep only the "Todas" chip)
            var toRemove = new List<UIElement>();
            foreach (UIElement child in pnlFiltroFamiliasVenta.Children)
            {
                if (child != chipTodasVenta)
                    toRemove.Add(child);
            }
            foreach (var child in toRemove)
                pnlFiltroFamiliasVenta.Children.Remove(child);

            // Filter families by search term
            var filtered = string.IsNullOrWhiteSpace(_familiaSearchTermVenta)
                ? _familiasVenta
                : _familiasVenta.Where(f =>
                    f.Nombre.Contains(_familiaSearchTermVenta, StringComparison.OrdinalIgnoreCase) ||
                    (f.Descripcion?.Contains(_familiaSearchTermVenta, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();

            foreach (var f in filtered)
            {
                bool isActive = _activeFamiliaFilterVenta == f.Id;

                var chip = new Border
                {
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(10, 3, 10, 3),
                    Margin = new Thickness(0, 0, 4, 3),
                    Cursor = Cursors.Hand,
                    Tag = f.Id,
                };

                if (isActive)
                    chip.Background = TryFindResource("AcentoBrush") as Brush
                        ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6C63FF"));
                else
                    chip.Background = TryFindResource("GridRowHoverBrush") as Brush
                        ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3F4B"));

                chip.MouseLeftButtonDown += ChipFamiliaVenta_Click;

                chip.Child = new TextBlock
                {
                    Text = f.Nombre,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = isActive ? Brushes.White
                        : (TryFindResource("NavTextColor") as Brush ?? Brushes.White),
                };

                pnlFiltroFamiliasVenta.Children.Add(chip);
            }

            UpdateTodasChipVentaState();
        }

        private void UpdateTodasChipVentaState()
        {
            if (_activeFamiliaFilterVenta == null)
                chipTodasVenta.Background = TryFindResource("AcentoBrush") as Brush;
            else
                chipTodasVenta.Background = TryFindResource("GridRowHoverBrush") as Brush
                    ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3F4B"));
        }

        private void ChipFamiliaVenta_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is int tag)
            {
                int? familiaId = tag == -1 ? null : tag;

                if (_activeFamiliaFilterVenta == familiaId && familiaId != null)
                    familiaId = null;

                _activeFamiliaFilterVenta = familiaId;
                RenderFamiliaChipsVenta();
                ApplyFamiliaFilterVenta();
                icProductos.ItemsSource = _filteredProductos;
            }
        }

        private void TxtBuscarFamiliaVenta_TextChanged(object sender, TextChangedEventArgs e)
        {
            _familiaSearchTermVenta = txtBuscarFamiliaVenta.Text.Trim();
            RenderFamiliaChipsVenta();
        }

        /// <summary>
        /// Filters _filteredProductos based on the active family filter.
        /// </summary>
        private async void ApplyFamiliaFilterVenta()
        {
            if (_activeFamiliaFilterVenta == null) return;

            try
            {
                var familiaProductos = await ProductoFamiliaService.GetProductosByFamilia(_activeFamiliaFilterVenta.Value);
                var familiaIds = new HashSet<int>(familiaProductos.Select(p => p.Id));
                _filteredProductos = _filteredProductos.Where(p => familiaIds.Contains(p.Id)).ToList();
            }
            catch { }
        }

        private void ProductCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isReadOnly) return;
            if (sender is FrameworkElement element && element.DataContext is ProductoModel prod)
            {
                if (prod.StockTotal <= 0)
                {
                    MessageBox.Show($"El producto \"{prod.Nombre}\" no tiene stock disponible.",
                        "Stock agotado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _selectedProducto = prod;
                txtSelectedProductName.Text = prod.Nombre;
                txtSelectedProductPrice.Text = $"Bs {prod.PrecioDisplay} | Stock: {prod.StockDisplay}";
                txtCantidad.Text = "1";
                panelCantidad.Visibility = Visibility.Visible;
                btnAgregarProducto.Visibility = Visibility.Visible;
            }
        }

        private void BtnCantidadMenos_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly) return;
            if (int.TryParse(txtCantidad.Text, out int cant) && cant > 1)
                txtCantidad.Text = (cant - 1).ToString();
        }

        private void BtnCantidadMas_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly) return;
            if (int.TryParse(txtCantidad.Text, out int cant))
                txtCantidad.Text = (cant + 1).ToString();
        }

        private void BtnAgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly || _selectedProducto == null) return;

            int cantidad = 1;
            if (!string.IsNullOrWhiteSpace(txtCantidad.Text))
                int.TryParse(txtCantidad.Text, out cantidad);
            if (cantidad <= 0) cantidad = 1;

            // Stock validation
            if (_selectedProducto.StockTotal <= 0)
            {
                MessageBox.Show($"El producto \"{_selectedProducto.Nombre}\" no tiene stock disponible.",
                    "Stock agotado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Calculate total quantity (existing + new)
            var existing = _detalles.FirstOrDefault(d => d.ProductoId == _selectedProducto.Id);
            int currentInCart = existing?.Cantidad ?? 0;
            int totalCantidad = currentInCart + cantidad;

            if (totalCantidad > _selectedProducto.StockTotal)
            {
                MessageBox.Show(
                    $"Stock insuficiente para \"{_selectedProducto.Nombre}\".\n" +
                    $"Solicitado: {totalCantidad} | Disponible: {_selectedProducto.StockTotal:N0}",
                    "Stock insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (existing != null)
            {
                existing.Cantidad = totalCantidad;
                existing.PrecioUnitario = _selectedProducto.PrecioVenta;
            }
            else
            {
                _detalles.Add(new VentaDetalleModel
                {
                    ProductoId = _selectedProducto.Id,
                    ProductoNombre = _selectedProducto.Nombre,
                    Cantidad = cantidad,
                    PrecioUnitario = _selectedProducto.PrecioVenta,
                });
            }

            RefreshProductList();
            _selectedProducto = null;
            panelCantidad.Visibility = Visibility.Collapsed;
            btnAgregarProducto.Visibility = Visibility.Collapsed;
        }

        private void RemoveProducto_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isReadOnly) return;
            if (sender is TextBlock tb && tb.DataContext is VentaDetalleModel det)
            {
                _detalles.Remove(det);
                RefreshProductList();
            }
        }

        private void RefreshProductList()
        {
            lstProductos.ItemsSource = null;
            lstProductos.ItemsSource = _detalles;
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            if (txtTotal == null || txtItemsCount == null) return;

            decimal subtotal = 0;
            foreach (var d in _detalles)
                subtotal += (d.Cantidad ?? 0) * (d.PrecioUnitario ?? 0);

            if (txtDescuento != null && decimal.TryParse(txtDescuento.Text, out decimal desc) && desc > 0)
                subtotal -= subtotal * (desc / 100m);

            txtTotal.Text = $"Bs {subtotal:N2}";
            txtItemsCount.Text = $"{_detalles.Count} producto{(_detalles.Count != 1 ? "s" : "")}";
        }

        private void OnFormChanged(object sender, RoutedEventArgs e) => UpdateTotal();

        private void Tipo_Checked(object sender, RoutedEventArgs e)
        {
            if (panelMeses == null) return;
            panelMeses.Visibility = rbPlanPago.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // ──────────── SAVE ────────────

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            bool isEditing = EditVenta != null && EditVenta.Id > 0;

            // If creating new, require cliente and products
            if (!isEditing)
            {
                if (cmbCliente.SelectedValue == null)
                {
                    MessageBox.Show("Seleccione un cliente.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_detalles.Count == 0)
                {
                    MessageBox.Show("Agregue al menos un producto a la venta.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Stock validation: verify all products have sufficient stock
                for (int i = 0; i < _detalles.Count; i++)
                {
                    var detalle = _detalles[i];
                    var producto = _allProductos.FirstOrDefault(p => p.Id == detalle.ProductoId);
                    if (producto == null) continue;

                    if (producto.StockTotal <= 0)
                    {
                        MessageBox.Show($"El producto \"{detalle.ProductoNombre}\" no tiene stock disponible.",
                            "Stock agotado", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if ((detalle.Cantidad ?? 0) > producto.StockTotal)
                    {
                        MessageBox.Show(
                            $"Stock insuficiente para \"{detalle.ProductoNombre}\".\n" +
                            $"Solicitado: {detalle.Cantidad:N0} | Disponible: {producto.StockTotal:N0}",
                            "Stock insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            try
            {
                // Save original button content so we can restore it after saving
                var originalContent = btnGuardar.Content;

                btnGuardar.IsEnabled = false;
                btnGuardar.Content = new TextBlock
                {
                    Text = "Guardando...",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (isEditing)
                {
                    // ─── UPDATE existing venta ───
                    EditVenta!.Estado = (cmbEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pedido";
                    EditVenta.Pagado = chkPagado.IsChecked == true;
                    EditVenta.Entregado = chkEntregado.IsChecked == true;
                    EditVenta.Nit = txtNit.Text.Trim();
                    EditVenta.Delivery = chkDelivery.IsChecked == true;
                    EditVenta.FechaEntrega = dpFechaEntrega.SelectedDate;
                    if (TimeSpan.TryParse(txtHoraEntrega.Text.Trim(), out var horaEntrega))
                        EditVenta.HoraEntrega = horaEntrega;

                    if (decimal.TryParse(txtDescuento.Text, out decimal desc))
                        EditVenta.PorcentajeDescuento = desc;
                    if (int.TryParse(txtMeses.Text, out int meses) && meses > 0)
                        EditVenta.Meses = meses;

                    await VentasService.UpdateVenta(EditVenta);

                    MessageBox.Show("Venta actualizada correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // ─── INSERT new venta ───
                    var venta = new VentaModel
                    {
                        ClienteCi = cmbCliente.SelectedValue.ToString(),
                        Fecha = DateTime.Today,
                        Hora = DateTime.Now.TimeOfDay,
                        Tipo = rbPlanPago.IsChecked == true ? "Plan de pago" : "Contado",
                        Estado = (cmbEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pedido",
                        Pagado = chkPagado.IsChecked == true,
                        Entregado = chkEntregado.IsChecked == true,
                        Nit = txtNit.Text.Trim(),
                        Delivery = chkDelivery.IsChecked == true,
                        FechaEntrega = dpFechaEntrega.SelectedDate,
                        HoraEntrega = TimeSpan.TryParse(txtHoraEntrega.Text.Trim(), out var he) ? he : null,
                    };

                    if (decimal.TryParse(txtDescuento.Text, out decimal desc))
                        venta.PorcentajeDescuento = desc;
                    else
                        venta.PorcentajeDescuento = 0;

                    if (int.TryParse(txtMeses.Text, out int meses) && meses > 0)
                        venta.Meses = meses;
                    else
                        venta.Meses = 1;

                    int newId = await VentasService.InsertVenta(venta);

                    foreach (var d in _detalles)
                    {
                        d.VentaId = newId;
                        await VentasService.InsertDetalle(d);
                    }

                    if (venta.Tipo == "Plan de pago" && venta.Meses.HasValue && venta.Meses.Value > 1)
                    {
                        await GenerarPagosPlan(newId, venta.Total, venta.Meses.Value);
                    }

                    // After creating a new venta, set it as EditVenta so the form
                    // stays in edit mode for this venta
                    venta.Id = newId;
                    EditVenta = venta;
                    EditVenta.Detalles = _detalles.ToList();
                    PopulateForm(EditVenta);

                    MessageBox.Show("Venta creada correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Propagate EditVenta to the parent PWVentas so the Pagos tab also sees it
                if (Window.GetWindow(this) is PopWindows.PWVentas pwVentas)
                {
                    pwVentas.EditVenta = EditVenta;
                }

                OnDataChanged?.Invoke();

                // Restore original button content (don't close window)
                btnGuardar.IsEnabled = true;
                btnGuardar.Content = originalContent;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar venta: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                btnGuardar.IsEnabled = true;
                btnGuardar.Content = null;
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

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }

        // ──────────── MARCAR COMPLETADO ────────────

        /// <summary>
        /// Shows/hides the "Marcar Completado" button based on Pagado + Entregado state.
        /// </summary>
        private void UpdateMarcarCompletadoVisibility()
        {
            bool ambosChequeados = chkPagado.IsChecked == true && chkEntregado.IsChecked == true;
            bool yaCompletado = string.Equals(
                (cmbEstado.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                "Completado",
                StringComparison.OrdinalIgnoreCase);

            btnMarcarCompletado.Visibility = (ambosChequeados && !yaCompletado)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void OnPagadoEntregadoChanged(object sender, RoutedEventArgs e)
        {
            UpdateMarcarCompletadoVisibility();
        }

        private async void BtnMarcarCompletado_Click(object sender, RoutedEventArgs e)
        {
            if (EditVenta == null || EditVenta.Id <= 0) return;

            var result = MessageBox.Show(
                $"¿Marcar venta #{EditVenta.Id} como Completado?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Set estado to Completado in the combobox
                foreach (ComboBoxItem item in cmbEstado.Items)
                {
                    if (string.Equals(item.Content?.ToString(), "Completado", StringComparison.OrdinalIgnoreCase))
                    {
                        cmbEstado.SelectedItem = item;
                        break;
                    }
                }

                // Save the venta
                EditVenta.Estado = "Completado";
                EditVenta.Pagado = chkPagado.IsChecked == true;
                EditVenta.Entregado = chkEntregado.IsChecked == true;

                await VentasService.UpdateVenta(EditVenta);

                MessageBox.Show("Venta marcada como Completado.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                UpdateMarcarCompletadoVisibility();
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar venta: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnHoyEntrega_Click(object sender, RoutedEventArgs e)
        {
            dpFechaEntrega.SelectedDate = DateTime.Today;
            txtHoraEntrega.Text = DateTime.Now.ToString(@"HH\:mm");
        }

        // ──────────── TRAER NIT DEL CLIENTE ────────────

        private async void BtnTraerNitCliente_Click(object sender, RoutedEventArgs e)
        {
            if (EditVenta == null && cmbCliente.SelectedValue == null)
            {
                MessageBox.Show("Seleccione un cliente primero.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string ci = EditVenta?.ClienteCi ?? cmbCliente.SelectedValue?.ToString() ?? "";
                if (string.IsNullOrEmpty(ci)) return;

                var cliente = await ClientesService.GetByCi(ci);
                if (cliente != null && !string.IsNullOrEmpty(cliente.Nit))
                {
                    txtNit.Text = cliente.Nit;
                }
                else
                {
                    MessageBox.Show("El cliente no tiene un NIT registrado.", "Información",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener NIT del cliente: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
