using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWClientes : Window
    {
        private bool _isEditing;
        private string? _editingCi;

        /// <summary>
        /// Tracks the local file path when an image is selected via the file picker.
        /// If set, the image will be uploaded to S3 on save.
        /// </summary>
        private string? _pendingImagePath;

        /// <summary>
        /// The marker placed on the map.
        /// </summary>
        private GMapMarker? _marker;

        /// <summary>
        /// Current map marker position.
        /// </summary>
        private PointLatLng? _mapPosition;

        /// <summary>
        /// Whether clicking on the map places a marker.
        /// </summary>
        private bool _mapEditingEnabled = true;

        /// <summary>
        /// Raised when the popup saves/deletes data so the parent can refresh.
        /// </summary>
        public event Action? OnDataChanged;

        /// <summary>
        /// If set, the popup opens in edit mode for this client.
        /// </summary>
        public ClienteModel? EditCliente { get; set; }

        public PWClientes()
        {
            InitializeComponent();
            this.Loaded += PWClientes_Loaded;
        }

        // ────────────────────────────── LOADING ──────────────────────────────

        private void PWClientes_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PWClientes_Loaded;

            // Initialize the map
            InitMap();

            if (EditCliente != null)
            {
                txtTitulo.Text = "Editar Cliente";
                PopulateForm(EditCliente);
            }
            else
            {
                txtCi.Focus();
            }
        }

        private void InitMap()
        {
            // Set GMap to fetch tiles from the server (required for tiles to load)
            GMaps.Instance.Mode = AccessMode.ServerOnly;

            // Default to Santa Cruz, Bolivia
            var defaultPos = new PointLatLng(-17.7833, -63.1821);

            gmapControl.MapProvider = GoogleMapProvider.Instance;
            gmapControl.DragButton = MouseButton.Left;
            gmapControl.CanDragMap = true;
            gmapControl.ShowCenter = false;
            gmapControl.MinZoom = 2;
            gmapControl.MaxZoom = 18;
            gmapControl.Zoom = 13;
            gmapControl.Position = defaultPos;

            // Handle mouse click to place marker
            gmapControl.MouseLeftButtonUp += GmapControl_MouseLeftButtonUp;
        }

        private void GmapControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (gmapControl.IsDragging) return;
            if (!_mapEditingEnabled) return;

            var point = e.GetPosition(gmapControl);
            var pos = gmapControl.FromLocalToLatLng((int)point.X, (int)point.Y);

            PlaceMarker(pos);
        }

        private void ChkPermitirEdicion_Checked(object sender, RoutedEventArgs e)
        {
            _mapEditingEnabled = chkPermitirEdicion.IsChecked == true;
        }

        private void PlaceMarker(PointLatLng pos)
        {
            // Remove existing marker
            if (_marker != null)
            {
                gmapControl.Markers.Remove(_marker);
            }

            // Create new marker (red circle)
            _marker = new GMapMarker(pos)
            {
                Shape = new Ellipse
                {
                    Width = 16,
                    Height = 16,
                    Fill = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                }
            };

            gmapControl.Markers.Add(_marker);
            _mapPosition = pos;

            // Update coordinate display
            txtLatitud.Text = pos.Lat.ToString("F6");
            txtLongitud.Text = pos.Lng.ToString("F6");
        }

        // ────────────────────────────── FORM HELPERS ──────────────────────────────

        private void ClearForm()
        {
            txtCi.Clear();
            txtNombre.Clear();
            txtApellido.Clear();
            txtDireccion.Clear();
            txtTelefono.Clear();
            txtNit.Clear();
            txtUrl.Clear();
            ClearImagePreview();

            // Clear map marker
            if (_marker != null)
            {
                gmapControl.Markers.Remove(_marker);
                _marker = null;
            }
            _mapPosition = null;
            gmapControl.Position = new PointLatLng(-17.7833, -63.1821);
            gmapControl.Zoom = 13;

            _pendingImagePath = null;

            _isEditing = false;
            _editingCi = null;
            txtCi.IsEnabled = true;
            btnEliminar.IsEnabled = false;
            txtTitulo.Text = "Nuevo Cliente";
        }

        private void ClearImagePreview()
        {
            imgPreview.Visibility = Visibility.Collapsed;
            imgPreviewClip.Visibility = Visibility.Collapsed;
            txtNoImage.Visibility = Visibility.Visible;
            txtImageStatus.Text = "Sin imagen seleccionada";
        }

        private void PopulateForm(ClienteModel c)
        {
            txtCi.Text = c.Ci;
            txtNombre.Text = c.Nombre ?? "";
            txtApellido.Text = c.Apellido ?? "";
            txtDireccion.Text = c.Direccion ?? "";
            txtTelefono.Text = c.Telefono ?? "";
            txtNit.Text = c.Nit ?? "";
            txtUrl.Text = c.Url ?? "";

            // Place marker if lat/long exist
            if (c.Latitud.HasValue && c.Longitud.HasValue)
            {
                var pos = new PointLatLng((double)c.Latitud.Value, (double)c.Longitud.Value);
                PlaceMarker(pos);
                gmapControl.Position = pos;
                gmapControl.Zoom = 15;
            }

            // Try to load the image if URL exists
            if (!string.IsNullOrEmpty(c.Url))
            {
                LoadImageFromUrl(c.Url);
            }

            _isEditing = true;
            _editingCi = c.Ci;
            txtCi.IsEnabled = false;
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

        private ClienteModel? GetFormData()
        {
            string ci = txtCi.Text.Trim();
            if (string.IsNullOrEmpty(ci))
            {
                MessageBox.Show("El campo CI es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCi.Focus();
                return null;
            }

            string nombre = txtNombre.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("El campo Nombre es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return null;
            }

            return new ClienteModel
            {
                Ci = ci,
                Nombre = nombre,
                Apellido = string.IsNullOrEmpty(txtApellido.Text.Trim()) ? null : txtApellido.Text.Trim(),
                Direccion = string.IsNullOrEmpty(txtDireccion.Text.Trim()) ? null : txtDireccion.Text.Trim(),
                Telefono = string.IsNullOrEmpty(txtTelefono.Text.Trim()) ? null : txtTelefono.Text.Trim(),
                Nit = string.IsNullOrEmpty(txtNit.Text.Trim()) ? null : txtNit.Text.Trim(),
                Url = string.IsNullOrEmpty(txtUrl.Text.Trim()) ? null : txtUrl.Text.Trim(),
                Latitud = _mapPosition.HasValue ? (decimal)_mapPosition.Value.Lat : null,
                Longitud = _mapPosition.HasValue ? (decimal)_mapPosition.Value.Lng : null
            };
        }

        // ────────────────────────────── IMAGE HANDLING ──────────────────────────────

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
                    txtImageStatus.Text = $"Imagen seleccionada: {System.IO.Path.GetFileName(filePath)}";

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

                    string? uploadedUrl = await S3Helper.UploadClientImageAsync(data.Ci, _pendingImagePath);

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
                        btnGuardar.Content = null; // Reset to default template
                        return;
                    }

                    _pendingImagePath = null;
                }

                // ─── Save client data ───
                if (_isEditing)
                {
                    await ClientesService.Update(data);
                    MessageBox.Show("Cliente actualizado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await ClientesService.Insert(data);
                    MessageBox.Show("Cliente creado correctamente.", "Éxito",
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
                btnGuardar.Content = null; // Reset to default template
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_editingCi == null) return;

            var result = MessageBox.Show(
                $"¿Está seguro de eliminar al cliente con CI {_editingCi}?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Delete the image from S3
                await S3Helper.DeleteClientImageAsync(_editingCi);

                await ClientesService.Delete(_editingCi);
                MessageBox.Show("Cliente eliminado correctamente.", "Éxito",
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

        /// <summary>
        /// Only allow window dragging when clicking on the header area,
        /// not on the close button, map, or other controls.
        /// </summary>
        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            // Don't drag if the click was on the close button or its children
            var originalSource = e.OriginalSource as DependencyObject;
            while (originalSource != null)
            {
                if (originalSource is Border border && border.Name == "closeButton")
                    return;
                originalSource = System.Windows.Media.VisualTreeHelper.GetParent(originalSource);
            }

            var pos = e.GetPosition(this);

            // Allow drag only in the top 55px (header area)
            if (pos.Y < 55)
            {
                this.DragMove();
            }
        }

        // ────────────────────────────── ADDRESS SEARCH ──────────────────────────────

        private List<LocationIQResult> _searchResults = new();

        private void TxtBuscarDireccion_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtBuscarDireccion.Text == "Buscar dirección...")
            {
                txtBuscarDireccion.Text = "";
                txtBuscarDireccion.Foreground = (Brush)FindResource("NavTextColor");
            }
        }

        private void TxtBuscarDireccion_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBuscarDireccion.Text))
            {
                txtBuscarDireccion.Text = "Buscar dirección...";
                txtBuscarDireccion.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
            }
        }

        private async void BtnBuscarDireccion_Click(object sender, RoutedEventArgs e)
        {
            string query = txtBuscarDireccion.Text.Trim();
            if (string.IsNullOrEmpty(query) || query == "Buscar dirección...") return;

            try
            {
                btnBuscar.IsEnabled = false;
                btnBuscar.Content = new TextBlock
                {
                    Text = "Buscando...",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 11
                };

                _searchResults = await LocationIQHelper.SearchAddress(query);

                if (_searchResults.Count > 0)
                {
                    lstResultados.ItemsSource = _searchResults;
                    resultadosPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    lstResultados.ItemsSource = null;
                    resultadosPanel.Visibility = Visibility.Collapsed;
                    MessageBox.Show("No se encontraron resultados para esa dirección.", "Búsqueda",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnBuscar.IsEnabled = true;
                // Restore button content
                btnBuscar.Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };
                ((StackPanel)btnBuscar.Content).Children.Add(new TextBlock
                {
                    Text = "🔍",
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 4, 0)
                });
                ((StackPanel)btnBuscar.Content).Children.Add(new TextBlock
                {
                    Text = "Buscar",
                    FontSize = 11
                });
            }
        }

        private void LstResultados_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstResultados.SelectedItem is LocationIQResult result)
            {
                if (double.TryParse(result.lat, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(result.lon, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lng))
                {
                    var pos = new PointLatLng(lat, lng);
                    PlaceMarker(pos);
                    gmapControl.Position = pos;
                    gmapControl.Zoom = 16;

                    // Hide results panel
                    resultadosPanel.Visibility = Visibility.Collapsed;
                    lstResultados.SelectedItem = null;
                }
            }
        }
    }
}
