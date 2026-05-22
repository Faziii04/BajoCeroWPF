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
    public partial class ProductosUC : UserControl
    {
        private List<ProductoModel> _productos = new();
        private ProductoModel? _selectedProducto;
        private bool _isEditing;
        private bool _isLoading;
        private bool _suspendSearch;

        public ProductosUC()
        {
            InitializeComponent();
        }

        private async void ProductosUC_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProductos();
        }

        private async Task LoadProductos()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                _productos = await ProductosService.GetAll();
                dgProductos.ItemsSource = _productos;
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar productos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateEmptyState()
        {
            txtEmptyState.Visibility = _productos == null || _productos.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClearForm()
        {
            _selectedProducto = null;
            _isEditing = false;
            txtNombre.Text = string.Empty;
            txtCategoria.Text = string.Empty;
            txtPrecioVenta.Text = string.Empty;
            txtEstado.Text = "Activo";
            btnEliminar.IsEnabled = false;
            dgProductos.SelectedItem = null;
        }

        private void PopulateForm(ProductoModel p)
        {
            txtNombre.Text = p.Nombre;
            txtCategoria.Text = p.Categoria ?? string.Empty;
            txtPrecioVenta.Text = p.PrecioVenta?.ToString("N2") ?? string.Empty;
            txtEstado.Text = p.Estado ?? "Activo";
            btnEliminar.IsEnabled = true;
        }

        private ProductoModel? GetFormData()
        {
            var nombre = txtNombre.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("El nombre del producto es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return null;
            }

            decimal? precio = null;
            if (!string.IsNullOrWhiteSpace(txtPrecioVenta.Text))
            {
                if (!decimal.TryParse(txtPrecioVenta.Text.Replace(",", ""), out var parsed))
                {
                    MessageBox.Show("El precio de venta no es válido.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPrecioVenta.Focus();
                    return null;
                }
                precio = parsed;
            }

            return new ProductoModel
            {
                Id = _selectedProducto?.Id ?? 0,
                Nombre = nombre,
                Categoria = string.IsNullOrWhiteSpace(txtCategoria.Text) ? null : txtCategoria.Text.Trim(),
                PrecioVenta = precio,
                Estado = string.IsNullOrWhiteSpace(txtEstado.Text) ? "Activo" : txtEstado.Text.Trim(),
            };
        }

        private void dgProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suspendSearch) return;

            if (dgProductos.SelectedItem is ProductoModel p)
            {
                _selectedProducto = p;
                _isEditing = true;
                PopulateForm(p);
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var producto = GetFormData();
            if (producto == null) return;

            try
            {
                if (_isEditing && _selectedProducto != null)
                {
                    await ProductosService.Update(producto);
                    MessageBox.Show("Producto actualizado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var newId = await ProductosService.Insert(producto);
                    producto.Id = newId;
                    MessageBox.Show("Producto creado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ClearForm();
                await LoadProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar producto: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProducto == null) return;

            var result = MessageBox.Show(
                $"¿Está seguro de desactivar el producto \"{_selectedProducto.Nombre}\"?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await ProductosService.Delete(_selectedProducto.Id);
                MessageBox.Show("Producto desactivado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                await LoadProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al desactivar producto: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            txtNombre.Focus();
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            _suspendSearch = true;
            txtBuscar.Text = string.Empty;
            _suspendSearch = false;
            ClearForm();
            await LoadProductos();
        }

        private async void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suspendSearch) return;

            var term = txtBuscar.Text.Trim();
            if (string.IsNullOrWhiteSpace(term))
            {
                await LoadProductos();
                return;
            }

            try
            {
                _productos = await ProductosService.Search(term);
                dgProductos.ItemsSource = _productos;
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar productos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
