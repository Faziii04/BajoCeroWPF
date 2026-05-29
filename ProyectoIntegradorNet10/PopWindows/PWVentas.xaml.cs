using Microsoft.Win32;
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

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWVentas : Window
    {
        private List<ProductoModel> _allProductos = new();
        private List<ProductoModel> _filteredProductos = new();
        private List<ClienteModel> _clientes = new();
        private List<ClienteModel> _filteredClientes = new();
        private List<VentaDetalleModel> _detalles = new();
        private ProductoModel? _selectedProducto;
        private DispatcherTimer? _clienteSearchTimer;
        private bool _isReadOnly;

        /// <summary>
        /// Raised when the popup saves data so the parent can refresh.
        /// </summary>
        public event Action? OnDataChanged;

        /// <summary>
        /// If set, the popup opens in edit mode for this venta.
        /// </summary>
        public VentaModel? EditVenta { get; set; }

        public PWVentas()
        {
            InitializeComponent();
            this.Loaded += PWVentas_Loaded;
        }

        private async void PWVentas_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PWVentas_Loaded;
            await LoadData();
            SetupClienteSearch();

            if (EditVenta != null)
            {
                PopulateForm(EditVenta);
            }
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            try
            {
                // Load products with images and stock (only active ones)
                _allProductos = (await VentasService.GetAllProductos())
                    .Where(p => string.Equals(p.Estado, "Activo", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                _filteredProductos = new List<ProductoModel>(_allProductos);
                icProductos.ItemsSource = _filteredProductos;

                // Load clients
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

        private void PopulateForm(VentaModel venta)
        {
            txtTitulo.Text = $"Venta #{venta.Id}";

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

            // Hide product gallery in edit mode (can't change products)
            if (_isReadOnly)
            {
                // Hide the gallery section
                var scrollViewer = icProductos.Parent as ScrollViewer;
                if (scrollViewer != null) scrollViewer.Visibility = Visibility.Collapsed;
                txtBuscarProducto.Visibility = Visibility.Collapsed;
                btnGuardar.Visibility = Visibility.Collapsed;
            }
        }

        private void SetReadOnlyMode(bool readOnly)
        {
            var brush = readOnly
                ? new SolidColorBrush(Color.FromRgb(100, 100, 100))
                : null;

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
                txtTitulo.Text = $"Venta #{EditVenta?.Id} (Pagado)";
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
                    .Where(p => p.Nombre.ToLower().Contains(term)
                             || (p.Categoria?.ToLower().Contains(term) ?? false))
                    .ToList();
            }
            icProductos.ItemsSource = _filteredProductos;
        }

        private void ProductCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isReadOnly) return;
            if (sender is FrameworkElement element && element.DataContext is ProductoModel prod)
            {
                _selectedProducto = prod;
                txtSelectedProductName.Text = prod.Nombre;
                txtSelectedProductPrice.Text = $"Bs {prod.PrecioDisplay} | Stock: {prod.StockDisplay}";
                txtCantidad.Text = "1";
                panelCantidad.Visibility = Visibility.Visible;
                btnAgregarProducto.Visibility = Visibility.Visible;
            }
        }

        // ──────────── CANTIDAD CONTROLS ────────────

        private void BtnCantidadMenos_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly) return;
            if (int.TryParse(txtCantidad.Text, out int cant) && cant > 1)
            {
                txtCantidad.Text = (cant - 1).ToString();
            }
        }

        private void BtnCantidadMas_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly) return;
            if (int.TryParse(txtCantidad.Text, out int cant))
            {
                txtCantidad.Text = (cant + 1).ToString();
            }
        }

        // ──────────── ADD / REMOVE PRODUCTS ────────────

        private void BtnAgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly || _selectedProducto == null) return;

            int cantidad = 1;
            if (!string.IsNullOrWhiteSpace(txtCantidad.Text))
            {
                int.TryParse(txtCantidad.Text, out cantidad);
            }
            if (cantidad <= 0) cantidad = 1;

            var existing = _detalles.FirstOrDefault(d => d.ProductoId == _selectedProducto.Id);
            if (existing != null)
            {
                existing.Cantidad = (existing.Cantidad ?? 0) + cantidad;
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

        // ──────────── TOTAL CALCULATION ────────────

        private void UpdateTotal()
        {
            if (txtTotal == null || txtItemsCount == null) return;

            decimal subtotal = 0;
            foreach (var d in _detalles)
                subtotal += (d.Cantidad ?? 0) * (d.PrecioUnitario ?? 0);

            if (txtDescuento != null && decimal.TryParse(txtDescuento.Text, out decimal desc) && desc > 0)
            {
                subtotal -= subtotal * (desc / 100m);
            }

            txtTotal.Text = $"Bs {subtotal:N2}";
            txtItemsCount.Text = $"{_detalles.Count} producto{(_detalles.Count != 1 ? "s" : "")}";
        }

        private void OnFormChanged(object sender, RoutedEventArgs e)
        {
            UpdateTotal();
        }

        // ──────────── TIPO ────────────

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

            // Validate
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

                // Insert venta
                int newId = await VentasService.InsertVenta(venta);

                // Insert detalles
                foreach (var d in _detalles)
                {
                    d.VentaId = newId;
                    await VentasService.InsertDetalle(d);
                }

                // If Plan de pago with more than 1 month, auto-generate pagos
                if (venta.Tipo == "Plan de pago" && venta.Meses.HasValue && venta.Meses.Value > 1)
                {
                    await GenerarPagosPlan(newId, venta.Total, venta.Meses.Value);
                }

                MessageBox.Show("Venta creada correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                OnDataChanged?.Invoke();
                this.Close();
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

        // ──────────── CLOSE / CANCEL ────────────

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnCerrar_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
