using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class ClientesUC : UserControl
    {
        private List<ClienteModel> _clientes = new();
        private bool _isEditing;

        /// <summary>
        /// Tracks the local file path when an image is selected via the file picker.
        /// If set, the image will be uploaded to S3 on save.
        /// </summary>
        private string? _pendingImagePath;

        public ClientesUC()
        {
            InitializeComponent();
            Loaded += ClientesUC_Loaded;
        }

        private async void ClientesUC_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadClientes();
        }

        private async Task LoadClientes()
        {
            try
            {
                _clientes = await ClientesService.GetAll();
                dgClientes.ItemsSource = _clientes;
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar clientes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateEmptyState()
        {
            txtEmptyState.Visibility = (_clientes == null || _clientes.Count == 0)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ClearForm()
        {
            _isEditing = false;
            txtCi.Text = "";
            txtNombre.Text = "";
            txtApellido.Text = "";
            txtDireccion.Text = "";
            txtTelefono.Text = "";
            txtNit.Text = "";
            txtCi.IsEnabled = true;
            btnEliminar.IsEnabled = false;
            ClearImagePreview();
            _pendingImagePath = null;
        }

        private void ClearImagePreview()
        {
            imgPreview.Visibility = Visibility.Collapsed;
            txtNoImage.Visibility = Visibility.Visible;
            txtImageStatus.Text = "Sin imagen seleccionada";
        }

        private void PopulateForm(ClienteModel c)
        {
            _isEditing = true;
            txtCi.Text = c.Ci;
            txtNombre.Text = c.Nombre ?? "";
            txtApellido.Text = c.Apellido ?? "";
            txtDireccion.Text = c.Direccion ?? "";
            txtTelefono.Text = c.Telefono ?? "";
            txtNit.Text = c.Nit ?? "";
            txtCi.IsEnabled = false;
            btnEliminar.IsEnabled = true;

            // Load image if URL exists
            if (!string.IsNullOrEmpty(c.Url))
            {
                LoadImageFromUrl(c.Url);
            }
            else
            {
                ClearImagePreview();
            }
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

        private ClienteModel? GetFormData()
        {
            string ci = txtCi.Text.Trim();
            if (string.IsNullOrEmpty(ci))
            {
                MessageBox.Show("El CI es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCi.Focus();
                return null;
            }

            string nombre = txtNombre.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("El nombre es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return null;
            }

            return new ClienteModel
            {
                Ci = ci,
                Nombre = nombre,
                Apellido = txtApellido.Text.Trim(),
                Direccion = txtDireccion.Text.Trim(),
                Telefono = txtTelefono.Text.Trim(),
                Nit = txtNit.Text.Trim(),
            };
        }

        private void dgClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgClientes.SelectedItem is ClienteModel cliente)
            {
                PopulateForm(cliente);
            }
        }

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

                    string? uploadedUrl = await S3Helper.UploadClientImageAsync(data.Ci, _pendingImagePath);

                    if (uploadedUrl != null)
                    {
                        data.Url = uploadedUrl;
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

                    _pendingImagePath = null;
                }

                if (_isEditing)
                {
                    await ClientesService.Update(data);
                    MessageBox.Show("Cliente actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await ClientesService.Insert(data);
                    MessageBox.Show("Cliente creado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                ClearForm();
                await LoadClientes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnGuardar.IsEnabled = true;
                btnGuardar.Content = null;
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            string ci = txtCi.Text.Trim();
            if (string.IsNullOrEmpty(ci)) return;

            var result = MessageBox.Show($"¿Eliminar al cliente {txtNombre.Text}?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Delete the image from S3
                await S3Helper.DeleteClientImageAsync(ci);

                await ClientesService.Delete(ci);
                MessageBox.Show("Cliente eliminado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                await LoadClientes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            dgClientes.SelectedItem = null;
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            txtCi.Focus();
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            await LoadClientes();
        }

        private async void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string term = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(term))
            {
                await LoadClientes();
                return;
            }

            try
            {
                _clientes = await ClientesService.Search(term);
                dgClientes.ItemsSource = _clientes;
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ────────────────────────────── IMAGE HANDLING ──────────────────────────────

        private void BtnUploadImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar imagen del cliente",
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
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar la imagen: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
