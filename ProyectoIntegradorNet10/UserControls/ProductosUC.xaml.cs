using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.PopWindows;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class ProductosUC : UserControl
    {
        private List<ProductoModel> _productos = new();
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
                icProductos.ItemsSource = _productos;
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

        private void OpenProductPopup(ProductoModel? producto = null)
        {
            var popup = new PWProductos
            {
                Owner = Window.GetWindow(this),
                EditProduct = producto
            };

            popup.OnDataChanged += async () =>
            {
                await LoadProductos();
            };

            popup.ShowDialog();
        }

        /// <summary>
        /// Handles click on a product card to open the edit popup.
        /// </summary>
        private void Card_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ProductoModel producto)
            {
                OpenProductPopup(producto);
            }
        }

        // ────────────────────────────── EVENT HANDLERS ──────────────────────────────

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            OpenProductPopup();
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            _suspendSearch = true;
            txtBuscar.Text = string.Empty;
            _suspendSearch = false;
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
                icProductos.ItemsSource = _productos;
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
