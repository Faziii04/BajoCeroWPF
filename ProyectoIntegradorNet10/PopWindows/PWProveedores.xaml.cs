using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWProveedores : Window
    {
        public event Action? OnDataChanged;
        public int EditProveedorId { get; set; }

        private bool _isEditing;

        public PWProveedores()
        {
            InitializeComponent();
            this.Loaded += PWProveedores_Loaded;
        }

        private async void PWProveedores_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PWProveedores_Loaded;

            if (EditProveedorId > 0)
            {
                txtTitulo.Text = "Editar Proveedor";
                _isEditing = true;
                btnEliminar.IsEnabled = true;
                await LoadProveedor(EditProveedorId);
            }
            else
            {
                txtNombre.Focus();
            }
        }

        private async Task LoadProveedor(int id)
        {
            try
            {
                var p = await ProveedoresService.GetById(id);
                if (p == null) return;

                txtNombre.Text = p.Nombre;
                txtDireccion.Text = p.Direccion ?? "";
                txtTelefono.Text = p.Telefono ?? "";
                txtDescripcion.Text = p.Descripcion ?? "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading proveedor: {ex.Message}");
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombre.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("El nombre del proveedor es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return;
            }

            try
            {
                btnGuardar.IsEnabled = false;

                if (_isEditing && EditProveedorId > 0)
                {
                    await ProveedoresService.Update(new ProveedorModel
                    {
                        Id = EditProveedorId,
                        Nombre = nombre,
                        Direccion = string.IsNullOrWhiteSpace(txtDireccion.Text) ? null : txtDireccion.Text.Trim(),
                        Telefono = string.IsNullOrWhiteSpace(txtTelefono.Text) ? null : txtTelefono.Text.Trim(),
                        Descripcion = string.IsNullOrWhiteSpace(txtDescripcion.Text) ? null : txtDescripcion.Text.Trim(),
                    });
                    MessageBox.Show("Proveedor actualizado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await ProveedoresService.Insert(new ProveedorModel
                    {
                        Nombre = nombre,
                        Direccion = string.IsNullOrWhiteSpace(txtDireccion.Text) ? null : txtDireccion.Text.Trim(),
                        Telefono = string.IsNullOrWhiteSpace(txtTelefono.Text) ? null : txtTelefono.Text.Trim(),
                        Descripcion = string.IsNullOrWhiteSpace(txtDescripcion.Text) ? null : txtDescripcion.Text.Trim(),
                    });
                    MessageBox.Show("Proveedor creado correctamente.", "Éxito",
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
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditing || EditProveedorId <= 0) return;

            var result = MessageBox.Show(
                $"¿Está seguro de eliminar al proveedor \"{txtNombre.Text}\"?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await ProveedoresService.Delete(EditProveedorId);
                MessageBox.Show("Proveedor eliminado correctamente.", "Éxito",
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
