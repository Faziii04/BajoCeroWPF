using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWProductos : Window
    {
        private bool _isEditing;
        private int? _editingId;

        /// <summary>
        /// Tracks the local file path when an image is selected via the file picker.
        /// If set, the image will be uploaded to S3 on save.
        /// </summary>
        private string? _pendingImagePath;

        /// <summary>
        /// Raised when the popup saves/deletes data so the parent can refresh.
        /// </summary>
        public event Action? OnDataChanged;

        /// <summary>
        /// If set, the popup opens in edit mode for this product.
        /// </summary>
        public ProductoModel? EditProduct { get; set; }

        public PWProductos()
        {
            InitializeComponent();
            this.Loaded += PWProductos_Loaded;
        }

        // ────────────────────────────── LOADING ──────────────────────────────

        private void PWProductos_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PWProductos_Loaded;

            if (EditProduct != null)
            {
                txtTitulo.Text = "Editar Producto";
                PopulateForm(EditProduct);
            }
            else
            {
                txtNombre.Focus();
            }
        }

        // ────────────────────────────── FORM HELPERS ──────────────────────────────

        private void ClearForm()
        {
            txtNombre.Clear();
            txtCategoria.Clear();
            txtPrecioVenta.Clear();
            cmbEstado.SelectedIndex = 0; // "Activo"
            txtUrl.Clear();
            ClearImagePreview();

            _pendingImagePath = null;

            _isEditing = false;
            _editingId = null;
            btnEliminar.IsEnabled = false;
            txtTitulo.Text = "Nuevo Producto";
        }

        private void ClearImagePreview()
        {
            imgPreview.Visibility = Visibility.Collapsed;
            imgPreviewClip.Visibility = Visibility.Collapsed;
            txtNoImage.Visibility = Visibility.Visible;
            txtImageStatus.Text = "Sin imagen seleccionada";
        }

        private void PopulateForm(ProductoModel p)
        {
            txtNombre.Text = p.Nombre;
            txtCategoria.Text = p.Categoria ?? "";
            txtPrecioVenta.Text = p.PrecioVenta?.ToString("N2") ?? "";
            txtUrl.Text = p.Url ?? "";

            // Set Estado combo
            string estado = p.Estado ?? "Activo";
            foreach (ComboBoxItem item in cmbEstado.Items)
            {
                if (string.Equals(item.Content.ToString(), estado, StringComparison.OrdinalIgnoreCase))
                {
                    cmbEstado.SelectedItem = item;
                    break;
                }
            }

            // Try to load the image if URL exists
            if (!string.IsNullOrEmpty(p.Url))
            {
                LoadImageFromUrl(p.Url);
            }

            _isEditing = true;
            _editingId = p.Id;
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
                txtImageStatus.Text = "Imagen cargada";
            }
            catch
            {
                ClearImagePreview();
            }
        }

        private ProductoModel? GetFormData()
        {
            string nombre = txtNombre.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("El nombre del producto es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return null;
            }

            decimal? precio = null;
            if (!string.IsNullOrWhiteSpace(txtPrecioVenta.Text))
            {
                // Try parsing with current culture first, then invariant culture
                string priceText = txtPrecioVenta.Text.Trim();
                if (!decimal.TryParse(priceText, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.CurrentCulture, out var parsed))
                {
                    // Fallback: replace comma with dot and try invariant
                    if (!decimal.TryParse(priceText.Replace(",", "."), System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out parsed))
                    {
                        MessageBox.Show("El precio de venta no es válido.", "Validación",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtPrecioVenta.Focus();
                        return null;
                    }
                }
                precio = parsed;
            }

            string estado = "Activo";
            if (cmbEstado.SelectedItem is ComboBoxItem selectedItem)
            {
                estado = selectedItem.Content.ToString() ?? "Activo";
            }

            return new ProductoModel
            {
                Id = _editingId ?? 0,
                Nombre = nombre,
                Categoria = string.IsNullOrWhiteSpace(txtCategoria.Text) ? null : txtCategoria.Text.Trim(),
                PrecioVenta = precio,
                Estado = estado,
                Url = string.IsNullOrWhiteSpace(txtUrl.Text) ? null : txtUrl.Text.Trim()
            };
        }

        // ────────────────────────────── IMAGE HANDLING ──────────────────────────────

        private void TxtUrl_TextChanged(object sender, TextChangedEventArgs e)
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

        private void BtnUploadImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar imagen del producto",
                Filter = "Imágenes (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|Todos los archivos (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;

                try
                {
                    // Load the image into the preview
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    imgPreview.Source = bitmap;
                    imgPreview.Visibility = Visibility.Visible;
                    txtNoImage.Visibility = Visibility.Collapsed;
                    txtImageStatus.Text = $"Imagen seleccionada: {Path.GetFileName(filePath)}";

                    // Store the local path — will be uploaded to S3 on save
                    _pendingImagePath = filePath;
                    txtUrl.Text = filePath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar la imagen: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ────────────────────────────── EVENT HANDLERS ──────────────────────────────

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var data = GetFormData();
            if (data == null) return;

            try
            {
                // ─── Upload image to S3 if a local file was selected ───
                if (_pendingImagePath != null && File.Exists(_pendingImagePath))
                {
                    btnGuardar.IsEnabled = false;
                    btnGuardar.Content = new TextBlock
                    {
                        Text = "Subiendo imagen...",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    if (_isEditing && _editingId.HasValue)
                    {
                        // Existing product — upload with existing ID
                        string? uploadedUrl = await S3Helper.UploadProductImageAsync(_editingId.Value, _pendingImagePath);
                        if (uploadedUrl != null)
                        {
                            data.Url = uploadedUrl;
                            txtUrl.Text = uploadedUrl;
                            txtImageStatus.Text = "Imagen subida a la nube";
                        }
                        else
                        {
                            MessageBox.Show("No se pudo subir la imagen. Intente de nuevo.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            btnGuardar.IsEnabled = true;
                            btnGuardar.Content = null;
                            return;
                        }
                    }

                    _pendingImagePath = null;
                }

                // ─── Save product data ───
                if (_isEditing)
                {
                    await ProductosService.Update(data);
                    MessageBox.Show("Producto actualizado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    int newId = await ProductosService.Insert(data);
                    data.Id = newId;

                    // If there's a pending image for a new product, upload it now with the new ID
                    if (_pendingImagePath != null && File.Exists(_pendingImagePath))
                    {
                        string? uploadedUrl = await S3Helper.UploadProductImageAsync(newId, _pendingImagePath);
                        if (uploadedUrl != null)
                        {
                            data.Url = uploadedUrl;
                            // Update the product record with the URL
                            await ProductosService.Update(data);
                            txtImageStatus.Text = "Imagen subida a la nube";
                        }
                        _pendingImagePath = null;
                    }

                    MessageBox.Show("Producto creado correctamente.", "Éxito",
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
                btnGuardar.Content = null;
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_editingId == null) return;

            var result = MessageBox.Show(
                $"¿Está seguro de desactivar el producto \"{txtNombre.Text}\"?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Delete the image from S3
                await S3Helper.DeleteProductImageAsync(_editingId.Value);

                await ProductosService.Delete(_editingId.Value);
                MessageBox.Show("Producto desactivado correctamente.", "Éxito",
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
