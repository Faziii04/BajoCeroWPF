using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class InventarioUC : UserControl
    {
        private List<InventarioModel> _inventario = new();
        private List<ProductoModel> _productos = new();
        private List<DepositoModel> _depositos = new();
        private InventarioModel? _selectedItem;
        private bool _isLoading;

        public InventarioUC()
        {
            InitializeComponent();
        }

        private async void InventarioUC_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAll();
        }

        private async Task LoadAll()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                var loadInventario = InventarioService.GetAll();
                var loadProductos = InventarioService.GetAllProductos();
                var loadDepositos = InventarioService.GetAllDepositos();

                await Task.WhenAll(loadInventario, loadProductos, loadDepositos);

                _inventario = loadInventario.Result;
                _productos = loadProductos.Result;
                _depositos = loadDepositos.Result;

                dgInventario.ItemsSource = _inventario;
                cmbProducto.ItemsSource = _productos;
                cmbDeposito.ItemsSource = _depositos;

                UpdateEmptyState();
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

        private void UpdateEmptyState()
        {
            txtEmptyState.Visibility = _inventario == null || _inventario.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClearForm()
        {
            _selectedItem = null;
            cmbProducto.SelectedIndex = -1;
            cmbDeposito.SelectedIndex = -1;
            txtCantidad.Text = "1";
            panelStockInfo.Visibility = Visibility.Collapsed;
            dgInventario.SelectedItem = null;
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
                        panelStockInfo.Visibility = Visibility.Visible;
                        txtStockActual.Text = $"Stock actual: {item.CantidadDisplay} unidades";
                        _selectedItem = item;
                    }
                    else
                    {
                        panelStockInfo.Visibility = Visibility.Visible;
                        txtStockActual.Text = "Stock actual: 0 unidades (nuevo registro)";
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

        private void dgInventario_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgInventario.SelectedItem is InventarioModel item)
            {
                _selectedItem = item;
                cmbProducto.SelectedValue = item.ProductoId;
                cmbDeposito.SelectedValue = item.DepositoId;
                txtCantidad.Text = item.Cantidad?.ToString() ?? "0";
                panelStockInfo.Visibility = Visibility.Visible;
                txtStockActual.Text = $"Stock actual: {item.CantidadDisplay} unidades";
            }
        }

        private async void BtnAgregarStock_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out int prodId, out int depId, out decimal cantidad))
                return;

            try
            {
                await InventarioService.AddStock(prodId, depId, cantidad);
                MessageBox.Show($"Se agregaron {cantidad} unidades al stock.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                await LoadAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar stock: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSetStock_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out int prodId, out int depId, out decimal cantidad))
                return;

            try
            {
                await InventarioService.SetStock(prodId, depId, cantidad);
                MessageBox.Show($"Stock fijado a {cantidad} unidades.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                await LoadAll();
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
                await LoadAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar registro: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            txtBuscar.Text = string.Empty;
            ClearForm();
            await LoadAll();
        }

        private async void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            var term = txtBuscar.Text.Trim();
            if (string.IsNullOrWhiteSpace(term))
            {
                await LoadAll();
                return;
            }

            try
            {
                _inventario = await InventarioService.Search(term);
                dgInventario.ItemsSource = _inventario;
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
    }
}
