using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWEmployees : Window
    {
        // View-model wrapper for roles with selectable state
        private class RolCheckItem
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public bool IsSelected { get; set; }
        }

        private List<RolCheckItem> _allRoles = new();
        private bool _isEditing;
        private string? _editingCi;

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
        /// If set, the popup opens in edit mode for this employee.
        /// </summary>
        public EmpleadoModel? EditEmployee { get; set; }

        public PWEmployees()
        {
            InitializeComponent();
            this.Loaded += PWEmployees_Loaded;
        }

        // ────────────────────────────── LOADING ──────────────────────────────

        private async void PWEmployees_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PWEmployees_Loaded;

            await LoadRoles();

            if (EditEmployee != null)
            {
                txtTitulo.Text = "Editar Empleado";
                PopulateForm(EditEmployee);
            }
            else
            {
                txtCi.Focus();
            }
        }

        private async Task LoadRoles()
        {
            try
            {
                var roles = await EmpleadoService.GetAllRoles();
                _allRoles = roles.Select(r => new RolCheckItem
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    IsSelected = false
                }).ToList();
                lstRoles.ItemsSource = _allRoles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar roles: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadRolesForEmpleado(string ci)
        {
            try
            {
                var assignedRoles = await EmpleadoService.GetRolesByEmpleado(ci);
                var assignedIds = new HashSet<int>(assignedRoles
                    .Where(r => r.Estado == "Activo")
                    .Select(r => r.RolId));

                foreach (var item in _allRoles)
                {
                    item.IsSelected = assignedIds.Contains(item.Id);
                }

                lstRoles.ItemsSource = null;
                lstRoles.ItemsSource = _allRoles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar roles del empleado: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ────────────────────────────── FORM HELPERS ──────────────────────────────

        private void ClearForm()
        {
            txtCi.Clear();
            txtNombre.Clear();
            txtApellido.Clear();
            txtCorreo.Clear();
            txtTelefono.Clear();
            txtDireccion.Clear();
            txtArea.Clear();
            txtTurno.Clear();
            txtUsuario.Clear();
            txtContrasena.Clear();
            txtUrl.Clear();
            ClearImagePreview();

            foreach (var item in _allRoles)
                item.IsSelected = false;
            lstRoles.ItemsSource = null;
            lstRoles.ItemsSource = _allRoles;

            _pendingImagePath = null;

            _isEditing = false;
            _editingCi = null;
            txtCi.IsEnabled = true;
            btnEliminar.IsEnabled = false;
            txtTitulo.Text = "Nuevo Empleado";
        }

        private void ClearImagePreview()
        {
            imgPreview.Visibility = Visibility.Collapsed;
            imgPreviewClip.Visibility = Visibility.Collapsed;
            txtNoImage.Visibility = Visibility.Visible;
            txtImageStatus.Text = "Sin imagen seleccionada";
        }

        private void PopulateForm(EmpleadoModel emp)
        {
            txtCi.Text = emp.Ci;
            txtNombre.Text = emp.Nombre;
            txtApellido.Text = emp.Apellido;
            txtCorreo.Text = emp.Correo;
            txtTelefono.Text = emp.Telefono ?? "";
            txtDireccion.Text = emp.Direccion ?? "";
            txtArea.Text = emp.Area ?? "";
            txtTurno.Text = emp.Turno ?? "";
            txtUsuario.Text = emp.Usuario;
            txtContrasena.Password = emp.Contrasena;
            txtUrl.Text = emp.Url ?? "";

            // Try to load the image if URL exists
            if (!string.IsNullOrEmpty(emp.Url))
            {
                LoadImageFromUrl(emp.Url);
            }

            _isEditing = true;
            _editingCi = emp.Ci;
            txtCi.IsEnabled = false;
            btnEliminar.IsEnabled = true;

            _ = LoadRolesForEmpleado(emp.Ci);
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

        private EmpleadoModel? GetFormData()
        {
            string ci = txtCi.Text.Trim();
            string nombre = txtNombre.Text.Trim();
            string apellido = txtApellido.Text.Trim();
            string correo = txtCorreo.Text.Trim();
            string usuario = txtUsuario.Text.Trim();
            string contrasena = txtContrasena.Password;

            // Validate required fields
            if (string.IsNullOrEmpty(ci))
            {
                MessageBox.Show("El campo CI es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCi.Focus();
                return null;
            }
            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("El campo Nombre es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return null;
            }
            if (string.IsNullOrEmpty(apellido))
            {
                MessageBox.Show("El campo Apellido es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtApellido.Focus();
                return null;
            }
            if (string.IsNullOrEmpty(correo))
            {
                MessageBox.Show("El campo Correo es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCorreo.Focus();
                return null;
            }
            if (string.IsNullOrEmpty(usuario))
            {
                MessageBox.Show("El campo Usuario es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsuario.Focus();
                return null;
            }
            if (string.IsNullOrEmpty(contrasena) && !_isEditing)
            {
                MessageBox.Show("El campo Contraseña es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtContrasena.Focus();
                return null;
            }

            return new EmpleadoModel
            {
                Ci = ci,
                Nombre = nombre,
                Apellido = apellido,
                Correo = correo,
                Telefono = string.IsNullOrEmpty(txtTelefono.Text.Trim()) ? null : txtTelefono.Text.Trim(),
                Direccion = string.IsNullOrEmpty(txtDireccion.Text.Trim()) ? null : txtDireccion.Text.Trim(),
                Area = string.IsNullOrEmpty(txtArea.Text.Trim()) ? null : txtArea.Text.Trim(),
                Turno = string.IsNullOrEmpty(txtTurno.Text.Trim()) ? null : txtTurno.Text.Trim(),
                Usuario = usuario,
                Contrasena = contrasena,
                Url = string.IsNullOrEmpty(txtUrl.Text.Trim()) ? null : txtUrl.Text.Trim()
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
                Title = "Seleccionar imagen del empleado",
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

        private void ChkRole_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Roles are saved when clicking "Guardar", not in real-time
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

                    string? uploadedUrl = await S3Helper.UploadEmployeeImageAsync(data.Ci, _pendingImagePath);

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

                // ─── Save employee data ───
                if (_isEditing)
                {
                    await EmpleadoService.UpdateEmpleado(data);
                    MessageBox.Show("Empleado actualizado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await EmpleadoService.InsertEmpleado(data);
                    MessageBox.Show("Empleado creado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Save role assignments
                await SaveRoleAssignments(data.Ci);

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
                $"¿Está seguro de eliminar al empleado con CI {_editingCi}?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await EmpleadoService.DeleteEmpleado(_editingCi);
                MessageBox.Show("Empleado eliminado correctamente.", "Éxito",
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

        // ────────────────────────────── ROLE SAVING ──────────────────────────────

        private async Task SaveRoleAssignments(string ci)
        {
            try
            {
                var currentlyAssigned = (await EmpleadoService.GetRolesByEmpleado(ci))
                    .Where(r => r.Estado == "Activo")
                    .Select(r => r.RolId)
                    .ToHashSet();

                var desiredAssigned = _allRoles
                    .Where(r => r.IsSelected)
                    .Select(r => r.Id)
                    .ToHashSet();

                // Roles to add
                foreach (var roleId in desiredAssigned)
                {
                    if (!currentlyAssigned.Contains(roleId))
                    {
                        await EmpleadoService.AssignRoleToEmpleado(ci, roleId);
                    }
                }

                // Roles to remove
                foreach (var roleId in currentlyAssigned)
                {
                    if (!desiredAssigned.Contains(roleId))
                    {
                        await EmpleadoService.RemoveRoleFromEmpleado(ci, roleId);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar roles: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
