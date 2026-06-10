using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWInventario : Window
    {
        private List<ProductoModel> _productos = new();
        private List<DepositoModel> _depositos = new();
        private InventarioModel? _selectedItem;
        private bool _isLoading;

        /// <summary>
        /// Raised when stock changes so the parent InventarioUC can refresh.
        /// </summary>
        public event Action? OnDataChanged;

        public PWInventario()
        {
            InitializeComponent();
            cmbProducto.SelectionChanged += (s, e) => CheckCurrentStock();
            cmbDeposito.SelectionChanged += (s, e) => CheckCurrentStock();
            this.Loaded += async (s, e) => await LoadCombos();
        }

        /// <summary>
        /// Opens pre-loaded with a specific inventory item.
        /// </summary>
        public PWInventario(InventarioModel item) : this()
        {
            _selectedItem = item;
            this.Loaded += (s, e) =>
            {
                cmbProducto.SelectedValue = item.ProductoId;
                cmbDeposito.SelectedValue = item.DepositoId;
                txtCantidad.Text = item.Cantidad?.ToString("N0") ?? "0";
                ShowStockInfo(item);
            };
        }

        private async System.Threading.Tasks.Task LoadCombos()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                var loadProductos = InventarioService.GetAllProductos();
                var loadDepositos = InventarioService.GetAllDepositos();

                await System.Threading.Tasks.Task.WhenAll(loadProductos, loadDepositos);

                _productos = loadProductos.Result;
                _depositos = loadDepositos.Result;

                cmbProducto.ItemsSource = _productos;
                cmbDeposito.ItemsSource = _depositos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void ClearForm()
        {
            _selectedItem = null;
            cmbProducto.SelectedIndex = -1;
            cmbDeposito.SelectedIndex = -1;
            txtCantidad.Text = "1";
            panelStockInfo.Visibility = Visibility.Collapsed;
        }

        private void ShowStockInfo(InventarioModel item)
        {
            panelStockInfo.Visibility = Visibility.Visible;
            txtStockActual.Text = $"{item.CantidadDisplay} unidades";
            txtProductoDeposito.Text = $"{item.ProductoNombre} → {item.DepositoNombre}";
        }

        private async void CheckCurrentStock()
        {
            if (cmbProducto.SelectedValue is int prodId && cmbDeposito.SelectedValue is int depId)
            {
                try
                {
                    var item = await InventarioService.GetByProductoAndDeposito(prodId, depId);
                    if (item != null)
                    {
                        ShowStockInfo(item);
                        _selectedItem = item;
                    }
                    else
                    {
                        var prod = _productos.FirstOrDefault(p => p.Id == prodId);
                        var dep = _depositos.FirstOrDefault(d => d.Id == depId);
                        panelStockInfo.Visibility = Visibility.Visible;
                        txtStockActual.Text = "0 unidades (nuevo)";
                        txtProductoDeposito.Text = $"{prod?.Nombre ?? "?"} → {dep?.Nombre ?? "?"}";
                        _selectedItem = null;
                    }
                }
                catch
                {
                    panelStockInfo.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                panelStockInfo.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnAgregarStock_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out int prodId, out int depId, out decimal cantidad)) return;

            try
            {
                await InventarioService.AddStock(prodId, depId, cantidad);
                MessageBox.Show($"Se agregaron {cantidad:N0} unidades al stock.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar stock: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnQuitarStock_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out int prodId, out int depId, out decimal cantidad)) return;

            try
            {
                await InventarioService.RemoveStock(prodId, depId, cantidad);
                MessageBox.Show($"Se quitaron {cantidad:N0} unidades del stock.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al quitar stock: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSetStock_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out int prodId, out int depId, out decimal cantidad)) return;

            try
            {
                await InventarioService.SetStock(prodId, depId, cantidad);
                MessageBox.Show($"Stock fijado a {cantidad:N0} unidades.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al fijar stock: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProducto.SelectedValue is not int prodId || cmbDeposito.SelectedValue is not int depId)
            {
                MessageBox.Show("Seleccione un producto y depósito.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "¿Está seguro de eliminar este registro de inventario?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await InventarioService.Delete(prodId, depId);
                MessageBox.Show("Registro eliminado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar registro: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private bool ValidateInputs(out int prodId, out int depId, out decimal cantidad)
        {
            prodId = 0;
            depId = 0;
            cantidad = 0;

            if (cmbProducto.SelectedValue is not int pId)
            {
                MessageBox.Show("Seleccione un producto.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cmbDeposito.SelectedValue is not int dId)
            {
                MessageBox.Show("Seleccione un depósito.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!decimal.TryParse(txtCantidad.Text, out decimal cant) || cant <= 0)
            {
                MessageBox.Show("Ingrese una cantidad válida mayor a 0.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCantidad.Focus();
                return false;
            }

            prodId = pId;
            depId = dId;
            cantidad = cant;
            return true;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
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
