using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWProduccion : Window
    {
        /// <summary>If > 0, opens in edit mode.</summary>
        public int EditProduccionId { get; set; }

        /// <summary>Fires after save/delete so the parent refreshes.</summary>
        public event Action? OnDataChanged;

        private ObservableCollection<ProduccionInsumoModel> _insumos = new();
        private ObservableCollection<ProduccionProductoModel> _productos = new();
        private bool _isEditMode;

        public PWProduccion()
        {
            InitializeComponent();
            this.Loaded += PWProduccion_Loaded;
        }

        private async void PWProduccion_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PWProduccion_Loaded;

            // Load combos
            try
            {
                var insumos = await InsumosService.GetAll();
                cmbInsumo.ItemsSource = insumos;

                var productos = await ProductosService.GetAll();
                cmbProducto.ItemsSource = productos;

                var depositos = await DepositosService.GetAll();
                cmbDeposito.ItemsSource = depositos;
                if (depositos.Count > 0)
                    cmbDeposito.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading combos: {ex.Message}");
            }

            // Bind lists
            lstInsumos.ItemsSource = _insumos;
            lstProductos.ItemsSource = _productos;

            // If editing, load data
            if (EditProduccionId > 0)
            {
                _isEditMode = true;
                txtTitulo.Text = $"Editar Producción #{EditProduccionId}";
                btnEliminar.IsEnabled = true;

                try
                {
                    var prod = await ProduccionService.GetById(EditProduccionId);
                    if (prod != null)
                    {
                        dpFechaInicio.SelectedDate = prod.FechaInicio;
                        txtCostoTotal.Text = prod.CostoTotal?.ToString("N2") ?? "";

                        // Set estado (select matching item)
                        foreach (ComboBoxItem item in cmbEstado.Items)
                        {
                            if (item.Content?.ToString() == prod.Estado)
                            {
                                cmbEstado.SelectedItem = item;
                                break;
                            }
                        }

                        // Load insumos and productos
                        foreach (var ins in prod.Insumos)
                            _insumos.Add(ins);
                        foreach (var prd in prod.Productos)
                            _productos.Add(prd);

                        // Apply read-only mode based on estado
                        ApplyEstadoMode(prod.Estado);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar producción: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // New production: default to Planificado
                dpFechaInicio.SelectedDate = DateTime.Today;
                cmbEstado.SelectedIndex = 0; // Planificado
            }
        }

        /// <summary>
        /// Toggles editability of insumos/productos based on estado.
        /// Only Planificado allows editing insumos and productos.
        /// </summary>
        private void ApplyEstadoMode(string? estado)
        {
            bool editable = estado == "Planificado";
            bool isCompleted = estado == "Completado";

            cmbInsumo.IsEnabled = editable;
            txtCantidadInsumo.IsEnabled = editable;
            btnAgregarInsumo.IsEnabled = editable;
            cmbProducto.IsEnabled = editable;
            txtCantidadProducto.IsEnabled = editable;
            btnAgregarProducto.IsEnabled = editable;

            // For completed state, show deposito picker (informational)
            panelDeposito.Visibility = isCompleted ? Visibility.Visible : Visibility.Collapsed;
            if (isCompleted)
                cmbDeposito.IsEnabled = false; // locked after save
        }

        private void CmbEstado_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (panelDeposito == null) return;
            if (cmbEstado.SelectedItem is ComboBoxItem item)
            {
                string? estado = item.Content?.ToString();
                panelDeposito.Visibility = estado == "Completado" ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                panelDeposito.Visibility = Visibility.Collapsed;
            }
        }

        // ──────────── ADD/REMOVE INSUMOS ────────────

        private void BtnAgregarInsumo_Click(object sender, RoutedEventArgs e)
        {
            if (cmbInsumo.SelectedValue == null) return;

            int insumoId = (int)cmbInsumo.SelectedValue;
            decimal cantidad = 1;
            if (!string.IsNullOrWhiteSpace(txtCantidadInsumo.Text))
                decimal.TryParse(txtCantidadInsumo.Text, out cantidad);

            if (cantidad <= 0)
            {
                MessageBox.Show("La cantidad debe ser mayor a 0.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check duplicates
            if (_insumos.Any(i => i.InsumoId == insumoId))
            {
                MessageBox.Show("Este insumo ya está en la lista.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var insumo = cmbInsumo.SelectedItem as dynamic;
            string nombre = insumo?.Nombre ?? "";
            decimal? precio = insumo?.PrecioUnitario as decimal?;
            string? unidad = insumo?.UnidadMedida as string;
            decimal? stock = insumo?.CantidadStock as decimal?;

            // Stock warning
            if (stock.HasValue && cantidad > stock.Value)
            {
                MessageBox.Show($"Stock insuficiente de \"{nombre}\": disponible {stock.Value:N0}, solicitado {cantidad:N0}",
                    "Advertencia de stock", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            _insumos.Add(new ProduccionInsumoModel
            {
                InsumoId = insumoId,
                Cantidad = cantidad,
                InsumoNombre = nombre,
                InsumoPrecio = precio,
                UnidadMedida = unidad,
                StockDisponible = stock,
            });

            txtCantidadInsumo.Text = "1";
            cmbInsumo.SelectedIndex = -1;
        }

        private void RemoveInsumo_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ProduccionInsumoModel ins)
                _insumos.Remove(ins);
        }

        // ──────────── ADD/REMOVE PRODUCTOS ────────────

        private void BtnAgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProducto.SelectedValue == null) return;

            int productoId = (int)cmbProducto.SelectedValue;
            decimal cantidad = 1;
            if (!string.IsNullOrWhiteSpace(txtCantidadProducto.Text))
                decimal.TryParse(txtCantidadProducto.Text, out cantidad);

            if (cantidad <= 0)
            {
                MessageBox.Show("La cantidad debe ser mayor a 0.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check duplicates
            if (_productos.Any(p => p.ProductoId == productoId))
            {
                MessageBox.Show("Este producto ya está en la lista.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var prod = cmbProducto.SelectedItem as dynamic;
            string nombre = prod?.Nombre ?? "";
            decimal? precio = prod?.PrecioVenta as decimal?;

            _productos.Add(new ProduccionProductoModel
            {
                ProductoId = productoId,
                Cantidad = cantidad,
                ProductoNombre = nombre,
                PrecioVenta = precio,
            });

            txtCantidadProducto.Text = "1";
            cmbProducto.SelectedIndex = -1;
        }

        private void RemoveProducto_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ProduccionProductoModel prod)
                _productos.Remove(prod);
        }

        // ──────────── SAVE ────────────

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            if (dpFechaInicio.SelectedDate == null)
            {
                MessageBox.Show("La fecha de inicio es obligatoria.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get estado from combo (informational only in edit mode)
            string? estado = (cmbEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Planificado";

            // Validate deposito if Completado
            if (estado == "Completado" && cmbDeposito.SelectedValue == null)
            {
                MessageBox.Show("Seleccione un depósito destino para los productos.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal? costoTotal = null;
            if (decimal.TryParse(txtCostoTotal.Text, out decimal costo))
                costoTotal = costo;

            try
            {
                if (_isEditMode)
                {
                    // Only allow full edit if Planificado
                    if (estado == "Planificado")
                    {
                        await ProduccionService.Update(
                            EditProduccionId,
                            dpFechaInicio.SelectedDate.Value,
                            costoTotal,
                            _insumos.ToList(),
                            _productos.ToList());
                    }
                    else
                    {
                        // Header-only update (insumos locked)
                        await ProduccionService.Update(
                            EditProduccionId,
                            dpFechaInicio.SelectedDate.Value,
                            costoTotal,
                            _insumos.ToList(),
                            _productos.ToList());
                    }

                    MessageBox.Show("Producción actualizada correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Create new (always Planificado)
                    await ProduccionService.Insert(
                        dpFechaInicio.SelectedDate.Value,
                        costoTotal,
                        _insumos.ToList(),
                        _productos.ToList());

                    MessageBox.Show("Producción creada correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                OnDataChanged?.Invoke();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ──────────── DELETE ────────────

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (EditProduccionId <= 0) return;

            var result = MessageBox.Show(
                $"¿Eliminar permanentemente producción #{EditProduccionId}?\n" +
                "Se revertirán todos los efectos de stock si corresponde.",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var (success, error) = await ProduccionService.Delete(EditProduccionId);
            if (success)
            {
                OnDataChanged?.Invoke();
                this.Close();
            }
            else
            {
                MessageBox.Show(error ?? "Error al eliminar", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ──────────── CANCEL / CLOSE ────────────

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
