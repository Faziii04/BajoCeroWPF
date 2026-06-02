using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWFamlias : Window
    {
        private bool _isEditing;
        private int? _editingId;

        /// <summary>
        /// Raised when the popup saves/deletes data so the parent can refresh.
        /// </summary>
        public event Action? OnDataChanged;

        /// <summary>
        /// If set, the popup opens in edit mode for this family.
        /// </summary>
        public FamiliaModel? EditFamilia { get; set; }

        public PWFamlias()
        {
            InitializeComponent();
            this.Loaded += PWFamlias_Loaded;
        }

        // ──────────────── LOADING ────────────────

        private async void PWFamlias_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PWFamlias_Loaded;

            if (EditFamilia != null)
            {
                txtTitulo.Text = "Editar Familia";
                PopulateForm(EditFamilia);
                await LoadProductosVinculados(EditFamilia.Id);
            }
            else
            {
                txtNombre.Focus();
            }
        }

        private async Task LoadProductosVinculados(int familiaId)
        {
            try
            {
                var productos = await ProductoFamiliaService.GetProductosByFamilia(familiaId);
                icProductosVinculados.ItemsSource = productos;
                txtProductoCount.Text = $"{productos.Count} producto(s)";
                borderProductos.Visibility = Visibility.Visible;

                txtNoProductos.Visibility = productos.Count == 0
                    ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading linked products: {ex.Message}");
            }
        }

        // ──────────────── FORM HELPERS ────────────────

        private void ClearForm()
        {
            txtNombre.Clear();
            txtDescripcion.Clear();
            txtUrl.Clear();
            ClearImagePreview();
            borderProductos.Visibility = Visibility.Collapsed;

            _isEditing = false;
            _editingId = null;
            btnEliminar.IsEnabled = false;
            txtTitulo.Text = "Nueva Familia";
        }

        private void ClearImagePreview()
        {
            imgPreview.Visibility = Visibility.Collapsed;
            txtNoImage.Visibility = Visibility.Visible;
        }

        private void PopulateForm(FamiliaModel f)
        {
            txtNombre.Text = f.Nombre;
            txtDescripcion.Text = f.Descripcion ?? "";
            txtUrl.Text = f.Url ?? "";

            if (!string.IsNullOrEmpty(f.Url))
            {
                LoadImageFromUrl(f.Url);
            }

            _isEditing = true;
            _editingId = f.Id;
            btnEliminar.IsEnabled = true;
        }

        private void LoadImageFromUrl(string url)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(url, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                imgPreview.Source = bitmap;
                imgPreview.Visibility = Visibility.Visible;
                txtNoImage.Visibility = Visibility.Collapsed;
            }
            catch
            {
                ClearImagePreview();
            }
        }

        private FamiliaModel? GetFormData()
        {
            string nombre = txtNombre.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("El nombre de la familia es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return null;
            }

            return new FamiliaModel
            {
                Id = _editingId ?? 0,
                Nombre = nombre,
                Descripcion = string.IsNullOrWhiteSpace(txtDescripcion.Text) ? null : txtDescripcion.Text.Trim(),
                Url = string.IsNullOrWhiteSpace(txtUrl.Text) ? null : txtUrl.Text.Trim()
            };
        }

        // ──────────────── IMAGE HANDLING ────────────────

        private void TxtUrl_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string url = txtUrl.Text.Trim();
            if (!string.IsNullOrEmpty(url))
            {
                LoadImageFromUrl(url);
            }
            else
            {
                ClearImagePreview();
            }
        }

        /// <summary>
        /// Removes a single product from this family.
        /// </summary>
        private async void BtnRemoveProducto_Click(object sender, MouseButtonEventArgs e)
        {
            if (_editingId == null) return;

            // Find the ProductoModel from the DataContext
            if (sender is FrameworkElement element && element.DataContext is ProductoModel producto)
            {
                var result = MessageBox.Show(
                    $"¿Quitar \"{producto.Nombre}\" de esta familia?",
                    "Quitar producto",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                try
                {
                    await ProductoFamiliaService.RemoveProductoFromFamilia(producto.Id, _editingId.Value);
                    await LoadProductosVinculados(_editingId.Value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al quitar producto: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ──────────────── EVENT HANDLERS ────────────────

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var data = GetFormData();
            if (data == null) return;

            try
            {
                if (_isEditing)
                {
                    await ProductoFamiliaService.Update(data);
                    MessageBox.Show("Familia actualizada correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    int newId = await ProductoFamiliaService.Insert(data);
                    data.Id = newId;
                    MessageBox.Show("Familia creada correctamente.", "Éxito",
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

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_editingId == null) return;

            var result = MessageBox.Show(
                $"¿Está seguro de eliminar la familia \"{txtNombre.Text}\"?\n" +
                "Se eliminarán todas las asociaciones con productos.",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await ProductoFamiliaService.Delete(_editingId.Value);
                MessageBox.Show("Familia eliminada correctamente.", "Éxito",
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
