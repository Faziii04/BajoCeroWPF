using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class EmpleadosUC : UserControl
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

        public EmpleadosUC()
        {
            InitializeComponent();
            LoadEmpleados();
            LoadRoles();
        }

        // ────────────────────────────── DATA LOADING ──────────────────────────────

        private void LoadEmpleados()
        {
            try
            {
                var empleados = EmpleadoService.GetAllEmpleados();
                dgEmpleados.ItemsSource = empleados;
                txtEmptyState.Visibility = empleados.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar empleados: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRoles()
        {
            try
            {
                var roles = EmpleadoService.GetAllRoles();
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
                MessageBox.Show($"Error al cargar roles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRolesForEmpleado(string ci)
        {
            try
            {
                var assignedRoles = EmpleadoService.GetRolesByEmpleado(ci);
                var assignedIds = new HashSet<int>(assignedRoles
                    .Where(r => r.Estado == "Activo")
                    .Select(r => r.RolId));

                foreach (var item in _allRoles)
                {
                    item.IsSelected = assignedIds.Contains(item.Id);
                }

                // Refresh the ListBox
                lstRoles.ItemsSource = null;
                lstRoles.ItemsSource = _allRoles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar roles del empleado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            foreach (var item in _allRoles)
                item.IsSelected = false;
            lstRoles.ItemsSource = null;
            lstRoles.ItemsSource = _allRoles;

            _isEditing = false;
            _editingCi = null;
            txtCi.IsEnabled = true;
            btnEliminar.IsEnabled = false;
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

            _isEditing = true;
            _editingCi = emp.Ci;
            txtCi.IsEnabled = false;
            btnEliminar.IsEnabled = true;

            LoadRolesForEmpleado(emp.Ci);
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
                MessageBox.Show("El campo CI es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCi.Focus();
                return null;
            }
            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("El campo Nombre es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return null;
            }
            if (string.IsNullOrEmpty(apellido))
            {
                MessageBox.Show("El campo Apellido es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtApellido.Focus();
                return null;
            }
            if (string.IsNullOrEmpty(correo))
            {
                MessageBox.Show("El campo Correo es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCorreo.Focus();
                return null;
            }
            if (string.IsNullOrEmpty(usuario))
            {
                MessageBox.Show("El campo Usuario es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsuario.Focus();
                return null;
            }
            if (string.IsNullOrEmpty(contrasena) && !_isEditing)
            {
                MessageBox.Show("El campo Contraseña es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        // ────────────────────────────── EVENT HANDLERS ──────────────────────────────

        private void dgEmpleados_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgEmpleados.SelectedItem is EmpleadoModel emp)
            {
                PopulateForm(emp);
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string term = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(term))
            {
                LoadEmpleados();
                return;
            }

            try
            {
                var results = EmpleadoService.SearchEmpleados(term);
                dgEmpleados.ItemsSource = results;
                txtEmptyState.Visibility = results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            txtCi.Focus();
        }

        private void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            txtBuscar.Clear();
            LoadEmpleados();
            LoadRoles();
            ClearForm();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var data = GetFormData();
            if (data == null) return;

            try
            {
                if (_isEditing)
                {
                    // Update existing
                    EmpleadoService.UpdateEmpleado(data);
                    MessageBox.Show("Empleado actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Insert new
                    EmpleadoService.InsertEmpleado(data);
                    MessageBox.Show("Empleado creado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Save role assignments
                SaveRoleAssignments(data.Ci);

                LoadEmpleados();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
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
                EmpleadoService.DeleteEmpleado(_editingCi);
                MessageBox.Show("Empleado eliminado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadEmpleados();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            dgEmpleados.SelectedItem = null;
        }

        private void ChkRole_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Roles are saved when clicking "Guardar", not in real-time
        }

        // ────────────────────────────── ROLE SAVING ──────────────────────────────

        private void SaveRoleAssignments(string ci)
        {
            try
            {
                var currentlyAssigned = EmpleadoService.GetRolesByEmpleado(ci)
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
                        EmpleadoService.AssignRoleToEmpleado(ci, roleId);
                    }
                }

                // Roles to remove
                foreach (var roleId in currentlyAssigned)
                {
                    if (!desiredAssigned.Contains(roleId))
                    {
                        EmpleadoService.RemoveRoleFromEmpleado(ci, roleId);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar roles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
