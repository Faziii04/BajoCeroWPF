using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWPrestamo : Window
    {
        // ─── Public ───
        public event Action? OnDataChanged;
        public int EditPrestamoId { get; set; }

        // ─── Product gallery state ───
        private List<ProductoModel> _allProductos = new();
        private List<ProductoModel> _filteredProductos = new();
        private List<FamiliaModel> _familias = new();
        private Dictionary<int, HashSet<int>> _familiaProductMap = new(); // familiaId → productIds
        private int? _activeFamiliaFilter;
        private string _familiaSearchTerm = "";
        private ProductoModel? _selectedProducto;

        // ─── Loan state ───
        private List<ClienteModel> _clientesList = new();
        private ObservableCollection<PrestamoDetalleModel> _detalles = new();
        private bool _isEditing;
        private bool _isInitialized;
        private DispatcherTimer? _clienteSearchTimer;

        public PWPrestamo()
        {
            InitializeComponent();
            this.Loaded += PWPrestamo_Loaded;
        }

        // ────────────────────────────── LOADING ──────────────────────────────

        private async void PWPrestamo_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PWPrestamo_Loaded;

            try
            {
                // Load products
                _allProductos = (await ProductosService.GetAll())
                    .Where(p => string.Equals(p.Estado, "Activo", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                _filteredProductos = new List<ProductoModel>(_allProductos);
                icProductos.ItemsSource = _filteredProductos;

                // Load families
                try
                {
                    _familias = await ProductoFamiliaService.GetAll();
                    // Load family-product mappings
                    foreach (var f in _familias)
                    {
                        var prods = await ProductoFamiliaService.GetProductosByFamilia(f.Id);
                        _familiaProductMap[f.Id] = new HashSet<int>(prods.Select(p => p.Id));
                    }
                }
                catch { _familias = new(); }
                RenderFamiliaChips();

                // Load clients
                _clientesList = await ClientesService.GetAll();
                cmbCliente.ItemsSource = _clientesList;

                // Setup client search
                SetupClienteSearch();

                // Bind products list
                lstProductos.ItemsSource = _detalles;

                if (EditPrestamoId > 0)
                {
                    txtTitulo.Text = $"Préstamo #{EditPrestamoId}";
                    _isEditing = true;
                    btnEliminar.IsEnabled = true;
                    await LoadPrestamoData(EditPrestamoId);
                }
                else
                {
                    txtFechaInfo.Text = $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }

            _isInitialized = true;
        }

        private async Task LoadPrestamoData(int prestamoId)
        {
            try
            {
                var (prestamo, detalles) = await PrestamosService.GetById(prestamoId);
                if (prestamo == null) return;

                txtFechaInfo.Text = $"Fecha: {prestamo.FechaDisplay}";

                // Select cliente
                if (!string.IsNullOrEmpty(prestamo.ClienteNombre))
                {
                    var match = _clientesList.FirstOrDefault(c =>
                        c.NombreCompleto.Equals(prestamo.ClienteNombre, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        cmbCliente.SelectedItem = match;
                }

                // Select estado
                string estado = prestamo.Estado ?? "Activo";
                foreach (ComboBoxItem item in cmbEstado.Items)
                {
                    if (string.Equals(item.Content?.ToString(), estado, StringComparison.OrdinalIgnoreCase))
                    {
                        cmbEstado.SelectedItem = item;
                        break;
                    }
                }

                // Load detalles
                _detalles.Clear();
                foreach (var d in detalles)
                {
                    _detalles.Add(new PrestamoDetalleModel
                    {
                        ClienteCi = d.ClienteCi,
                        ProductoId = d.ProductoId,
                        PrestamoId = d.PrestamoId,
                        Cantidad = d.Cantidad,
                        ValorReposicion = d.ValorReposicion,
                        ProductoNombre = d.ProductoNombre,
                        ProductoPrecio = d.ProductoPrecio,
                    });
                }

                UpdateTotals();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading prestamo data: {ex.Message}");
            }
        }

        // ────────────────────────────── FAMILY FILTERS ──────────────────────────────

        private void RenderFamiliaChips()
        {
            pnlFiltroFamilias.Children.Clear();

            // "Todas" chip
            var chipDefault = new Border
        {
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(10, 3, 10, 3),
                Cursor = Cursors.Hand,
                Background = _activeFamiliaFilter == null || _activeFamiliaFilter == -1
                    ? (System.Windows.Media.Brush)FindResource("AcentoBrush")
                    : (System.Windows.Media.Brush)FindResource("SeparatorColor"),
                Margin = new Thickness(0, 0, 4, 3),
                Tag = -1,
            };
            chipDefault.MouseLeftButtonDown += ChipFamilia_Click;
            chipDefault.Child = new TextBlock
            {
                Text = "Todas",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = _activeFamiliaFilter == null || _activeFamiliaFilter == -1
                    ? System.Windows.Media.Brushes.White
                    : (System.Windows.Media.Brush)FindResource("NavTextColor"),
            };
            pnlFiltroFamilias.Children.Add(chipDefault);

            var filtered = string.IsNullOrWhiteSpace(_familiaSearchTerm)
                ? _familias
                : _familias.Where(f =>
                    f.Nombre.Contains(_familiaSearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var familia in filtered)
            {
                bool isActive = _activeFamiliaFilter == familia.Id;
                var chip = new Border
                {
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(10, 3, 10, 3),
                    Cursor = Cursors.Hand,
                    Background = isActive
                        ? (System.Windows.Media.Brush)FindResource("AcentoBrush")
                        : (System.Windows.Media.Brush)FindResource("SeparatorColor"),
                    Margin = new Thickness(0, 0, 4, 3),
                    Tag = familia.Id,
                };
                chip.MouseLeftButtonDown += ChipFamilia_Click;
                chip.Child = new TextBlock
                {
                    Text = familia.Nombre,
                    FontSize = 11,
                    FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = isActive
                        ? System.Windows.Media.Brushes.White
                        : (System.Windows.Media.Brush)FindResource("NavTextColor"),
                };
                pnlFiltroFamilias.Children.Add(chip);
            }
        }

        private void ChipFamilia_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border chip && chip.Tag is int id)
            {
                _activeFamiliaFilter = id == -1 ? null : id;
                ApplyProductFilters();
                RenderFamiliaChips();
            }
        }

        private void TxtBuscarFamilia_TextChanged(object sender, TextChangedEventArgs e)
        {
            _familiaSearchTerm = txtBuscarFamilia.Text.Trim();
            RenderFamiliaChips();
        }

        // ────────────────────────────── PRODUCT SEARCH / FILTER ──────────────────────────────

        private void TxtBuscarProducto_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyProductFilters();
        }

        private void ApplyProductFilters()
        {
            string search = txtBuscarProducto.Text.Trim().ToLower();

            var filtered = string.IsNullOrWhiteSpace(search)
                ? _allProductos
                : _allProductos.Where(p =>
                    p.Nombre.ToLower().Contains(search)).ToList();

            // Apply family filter using pre-loaded map
            if (_activeFamiliaFilter.HasValue && _familiaProductMap.TryGetValue(_activeFamiliaFilter.Value, out var productIds))
            {
                filtered = filtered.Where(p => productIds.Contains(p.Id)).ToList();
            }

            _filteredProductos = filtered;
            icProductos.ItemsSource = _filteredProductos;
        }

        // ────────────────────────────── CLIENT SEARCH / SELECT ──────────────────────────────

        private void CmbCliente_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCliente.SelectedItem is ClienteModel cliente)
            {
                cmbCliente.Text = cliente.NombreCompleto;
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
            string term = "";
            if (cmbCliente.Template.FindName("PART_EditableTextBox", cmbCliente) is TextBox tb)
                term = tb.Text.Trim().ToLower();
            else
                term = cmbCliente.Text.Trim().ToLower();

            // Direct filtering instead of ICollectionView (more reliable for selection)
            if (string.IsNullOrWhiteSpace(term))
            {
                cmbCliente.ItemsSource = _clientesList;
            }
            else
            {
                var filtered = _clientesList.Where(c =>
                    (c.Ci?.ToLower().Contains(term) ?? false)
                    || (c.Nombre?.ToLower().Contains(term) ?? false)
                    || (c.Apellido?.ToLower().Contains(term) ?? false)
                    || c.NombreCompleto.ToLower().Contains(term)
                ).ToList();
                cmbCliente.ItemsSource = filtered;
            }
        }

        // ────────────────────────────── PRODUCT SELECTION ──────────────────────────────

        private void ProductCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ProductoModel producto)
            {
                _selectedProducto = producto;
                txtSelectedProductName.Text = producto.Nombre;
                txtSelectedProductPrice.Text = $"Bs {producto.PrecioVenta?.ToString("N2") ?? "0.00"}";
                txtCantidad.Text = "1";
                panelCantidad.Visibility = Visibility.Visible;
                btnAgregarProducto.Visibility = Visibility.Visible;
            }
        }

        // ────────────────────────────── QUANTITY CONTROLS ──────────────────────────────

        private void BtnCantidadMenos_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtCantidad.Text, out int cant) && cant > 1)
                txtCantidad.Text = (cant - 1).ToString();
        }

        private void BtnCantidadMas_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtCantidad.Text, out int cant))
                txtCantidad.Text = (cant + 1).ToString();
            else
                txtCantidad.Text = "2";
        }

        // ────────────────────────────── ADD / REMOVE PRODUCTS ──────────────────────────────

        private void BtnAgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProducto == null) return;

            if (!int.TryParse(txtCantidad.Text, out int cant) || cant <= 0)
            {
                MessageBox.Show("Ingrese una cantidad válida.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if product already added → increment quantity
            var existing = _detalles.FirstOrDefault(d => d.ProductoId == _selectedProducto.Id);
            if (existing != null)
            {
                existing.Cantidad = (existing.Cantidad ?? 0) + cant;
                existing.ValorReposicion = (existing.ProductoPrecio ?? 0) * (existing.Cantidad ?? 0);
                // Refresh
                int idx = _detalles.IndexOf(existing);
                _detalles.RemoveAt(idx);
                _detalles.Insert(idx, existing);
            }
            else
            {
                _detalles.Add(new PrestamoDetalleModel
                {
                    ProductoId = _selectedProducto.Id,
                    ProductoNombre = _selectedProducto.Nombre,
                    ProductoPrecio = _selectedProducto.PrecioVenta,
                    Cantidad = cant,
                    ValorReposicion = (_selectedProducto.PrecioVenta ?? 0) * cant,
                });
            }

            UpdateTotals();

            // Reset selection
            _selectedProducto = null;
            panelCantidad.Visibility = Visibility.Collapsed;
            btnAgregarProducto.Visibility = Visibility.Collapsed;
        }

        private void RemoveProducto_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock tb && tb.DataContext is PrestamoDetalleModel detalle)
            {
                _detalles.Remove(detalle);
                UpdateTotals();
            }
        }

        // ────────────────────────────── TOTALS ──────────────────────────────

        private void UpdateTotals()
        {
            int count = _detalles.Sum(d => d.Cantidad.GetValueOrDefault());
            decimal total = _detalles.Sum(d => d.ValorReposicion.GetValueOrDefault());
            txtItemsCount.Text = $"{count} productos";
            txtTotal.Text = $"Bs {total:N2}";
        }

        // ────────────────────────────── SAVE ──────────────────────────────

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCliente.SelectedItem is not ClienteModel cliente)
            {
                MessageBox.Show("Debe seleccionar un cliente.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCliente.Focus();
                return;
            }

            var validDetalles = _detalles
                .Where(d => d.ProductoId > 0 && d.Cantidad.GetValueOrDefault() > 0)
                .ToList();

            if (validDetalles.Count == 0)
            {
                MessageBox.Show("Debe agregar al menos un producto con cantidad válida.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string estado = "Activo";
            if (cmbEstado.SelectedItem is ComboBoxItem estItem)
                estado = estItem.Content?.ToString() ?? "Activo";

            try
            {
                btnGuardar.IsEnabled = false;

                foreach (var d in validDetalles)
                    d.ClienteCi = cliente.Ci;

                if (_isEditing && EditPrestamoId > 0)
                {
                    await PrestamosService.Update(EditPrestamoId, cliente.Ci, estado, validDetalles);
                    MessageBox.Show("Préstamo actualizado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await PrestamosService.Insert(cliente.Ci, estado, validDetalles);
                    MessageBox.Show("Préstamo creado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                OnDataChanged?.Invoke();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                btnGuardar.IsEnabled = true;
            }
        }

        // ────────────────────────────── DELETE ──────────────────────────────

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditing || EditPrestamoId <= 0) return;

            var result = MessageBox.Show(
                $"¿Está seguro de eliminar el préstamo #{EditPrestamoId}?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await PrestamosService.Delete(EditPrestamoId);
                MessageBox.Show("Préstamo eliminado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                OnDataChanged?.Invoke();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ────────────────────────────── CLOSE / DRAG ──────────────────────────────

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();
        private void BtnCerrar_Click(object sender, MouseButtonEventArgs e) => this.Close();

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
