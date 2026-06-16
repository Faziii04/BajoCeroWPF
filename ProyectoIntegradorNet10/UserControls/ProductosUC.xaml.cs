using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.PopWindows;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class ProductosUC : UserControl
    {
        private List<ProductoModel> _productos = new();
        private List<FamiliaModel> _familias = new();
        private int? _activeFamiliaFilter; // null = all, otherwise familia ID
        private bool _isLoading;
        private bool _suspendSearch;
        private string _familiaSearchTerm = "";

        public ProductosUC()
        {
            InitializeComponent();
        }

        private async void ProductosUC_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFamilias();
            await LoadProductos();
        }

        private async Task LoadFamilias()
        {
            try
            {
                _familias = await ProductoFamiliaService.GetAll();
                RenderFamiliaChips();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading families: {ex.Message}");
            }
        }

        /// <summary>
        /// Renders the family filter chips from the loaded families list,
        /// filtered by the current search term.
        /// </summary>
        private void RenderFamiliaChips()
        {
            // Remove old chips (keep only the "Todas" chip)
            var toRemove = new List<UIElement>();
            foreach (UIElement child in pnlFiltroFamilias.Children)
            {
                if (child != chipTodas)
                    toRemove.Add(child);
            }
            foreach (var child in toRemove)
                pnlFiltroFamilias.Children.Remove(child);

            // Filter families by search term
            var filtered = string.IsNullOrWhiteSpace(_familiaSearchTerm)
                ? _familias
                : _familias.Where(f =>
                    f.Nombre.Contains(_familiaSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (f.Descripcion?.Contains(_familiaSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();

            // Add family chips
            foreach (var f in filtered)
            {
                bool isActive = _activeFamiliaFilter == f.Id;

                var chip = new Border
                {
                    CornerRadius = new CornerRadius(14),
                    Padding = new Thickness(14, 5, 14, 5),
                    Margin = new Thickness(0, 0, 6, 4),
                    Cursor = Cursors.Hand,
                    Tag = f.Id,
                };

                // Background
                if (isActive)
                    chip.Background = TryFindResource("AcentoBrush") as Brush
                        ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6C63FF"));
                else
                    chip.Background = TryFindResource("GridRowHoverBrush") as Brush
                        ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3F4B"));

                chip.MouseLeftButtonDown += ChipFamilia_Click;

                chip.Child = new TextBlock
                {
                    Text = f.Nombre,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = isActive ? Brushes.White
                        : (TryFindResource("NavTextColor") as Brush ?? Brushes.White),
                };

                pnlFiltroFamilias.Children.Add(chip);
            }

            UpdateTodasChipState();
        }

        private void UpdateTodasChipState()
        {
            // Highlight "Todas" when no filter is active
            if (_activeFamiliaFilter == null)
            {
                chipTodas.Background = TryFindResource("AcentoBrush") as Brush;
            }
            else
            {
                chipTodas.Background = TryFindResource("GridRowHoverBrush") as Brush
                    ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3F4B"));
            }
        }

        private async void ChipFamilia_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is int tag)
            {
                int? familiaId = tag == -1 ? null : tag;

                // Toggle: if same chip clicked, clear filter
                if (_activeFamiliaFilter == familiaId && familiaId != null)
                    familiaId = null;

                _activeFamiliaFilter = familiaId;

                // Update chip visuals
                RenderFamiliaChips();

                // Reload products with filter
                await LoadProductos();
            }
        }

        private async Task LoadProductos()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                if (_activeFamiliaFilter.HasValue)
                {
                    _productos = await ProductoFamiliaService.GetProductosByFamilia(_activeFamiliaFilter.Value);
                }
                else
                {
                    _productos = await ProductosService.GetAll();
                }
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
            int count = _productos?.Count ?? 0;
            bool empty = count == 0;
            panelEmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            txtProductCount.Text = empty ? "Sin productos" : $"{count} producto{(count != 1 ? "s" : "")}";
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
                await LoadFamilias();
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
            _activeFamiliaFilter = null;
            RenderFamiliaChips();
            await LoadProductos();
            await LoadFamilias();
        }

        private void TxtBuscarFamilia_TextChanged(object sender, TextChangedEventArgs e)
        {
            _familiaSearchTerm = txtBuscarFamilia.Text.Trim();
            RenderFamiliaChips();
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
