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
                cmbCliente.ItemsSource = _filteredClientes;
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

        // ──────────── POPULATE FORM (EDIT MODE) ────────────

        public void PopulateForm(VentaModel venta)
        {
            // Check if read-only
            string estado = venta.Estado ?? "";
            _isReadOnly = string.Equals(estado, "Pagado", StringComparison.OrdinalIgnoreCase)
                       || estado.IndexOf("Pagado", StringComparison.OrdinalIgnoreCase) >= 0;

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

            // Load detalles
            _detalles = venta.Detalles.ToList();
            RefreshProductList();

            // Hide product gallery in edit mode
            if (_isReadOnly)
            {
                var scrollViewer = icProductos.Parent as ScrollViewer;
                if (scrollViewer != null) scrollViewer.Visibility = Visibility.Collapsed;
                txtBuscarProducto.Visibility = Visibility.Collapsed;
                btnGuardar.Visibility = Visibility.Collapsed;
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
            btnAgregarProducto.Visibility = readOnly ? Visibility.Collapsed : Visibility.Collapsed;

            if (readOnly)
            {
                btnGuardar.Visibility = Visibility.Collapsed;
            }
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
            if (_isReadOnly) return;

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

            try
            {
                btnGuardar.IsEnabled = false;
                btnGuardar.Content = new TextBlock
                {
                    Text = "Guardando...",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var venta = new VentaModel
                {
                    ClienteCi = cmbCliente.SelectedValue.ToString(),
                    Fecha = DateTime.Today,
                    Hora = DateTime.Now.TimeOfDay,
                    Tipo = rbPlanPago.IsChecked == true ? "Plan de pago" : "Contado",
                    Estado = "Pendiente",
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

                MessageBox.Show("Venta creada correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                OnDataChanged?.Invoke();

                // Close parent window
                Window.GetWindow(this)?.Close();
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
    }
}
