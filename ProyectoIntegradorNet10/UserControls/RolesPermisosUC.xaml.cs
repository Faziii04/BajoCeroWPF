using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class RolesPermisosUC : UserControl
    {
        // ─── View-model wrappers ───

        private class PermisoCheckItem
        {
            public int PermisoId { get; set; }
            public string PermisoNombre { get; set; } = string.Empty;
            public bool IsSelected { get; set; }
        }

        // ─── State ───

        private List<PermisoCheckItem> _allPermisos = new();
        private bool _isEditingRol;
        private bool _isEditingPermiso;
        private int _editingRolId;
        private int _editingPermisoId;
        private bool _isRolTabActive = true;

        public RolesPermisosUC()
        {
            InitializeComponent();
            this.Loaded += RolesPermisosUC_Loaded;
        }

        // ════════════════════════════════════════════════════════════════
        //  DATA LOADING
        // ════════════════════════════════════════════════════════════════

        private async void RolesPermisosUC_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= RolesPermisosUC_Loaded;
            await Task.WhenAll(LoadRoles(), LoadPermisos());
        }

        private async Task LoadRoles()
        {
            try
            {
                var roles = await RolesPermisosService.GetAllRoles();
                dgRoles.ItemsSource = roles;
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar roles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadPermisos()
        {
            try
            {
                var permisos = await RolesPermisosService.GetAllPermisos();
                dgPermisos.ItemsSource = permisos;

                // Also refresh the checklist items
                _allPermisos = permisos.Select(p => new PermisoCheckItem
                {
                    PermisoId = p.Id,
                    PermisoNombre = p.Permiso,
                    IsSelected = false
                }).ToList();
                lstPermisos.ItemsSource = _allPermisos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar permisos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadPermisosForRol(int rolId)
        {
            try
            {
                var assigned = await RolesPermisosService.GetPermisosByRol(rolId);
                var assignedIds = new HashSet<int>(assigned
                    .Where(rp => rp.Estado == "Activo")
                    .Select(rp => rp.PermisoId));

                foreach (var item in _allPermisos)
                {
                    item.IsSelected = assignedIds.Contains(item.PermisoId);
                }

                // Refresh the ListBox
                lstPermisos.ItemsSource = null;
                lstPermisos.ItemsSource = _allPermisos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar permisos del rol: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateEmptyState()
        {
            if (_isRolTabActive)
            {
                var source = dgRoles.ItemsSource as System.Collections.ICollection;
                txtEmptyState.Visibility = (source == null || source.Count == 0)
                    ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                var source = dgPermisos.ItemsSource as System.Collections.ICollection;
                txtEmptyState.Visibility = (source == null || source.Count == 0)
                    ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  TAB SWITCHING
        // ════════════════════════════════════════════════════════════════

        private void TabRoles_Checked(object sender, RoutedEventArgs e)
        {
            // Guard: this event fires during XAML parsing when IsChecked="True",
            // before InitializeComponent() has connected all named elements.
            if (dgRoles == null) return;

            _isRolTabActive = true;
            dgRoles.Visibility = Visibility.Visible;
            dgPermisos.Visibility = Visibility.Collapsed;
            panelRolForm.Visibility = Visibility.Visible;
            panelPermisoForm.Visibility = Visibility.Collapsed;
            ClearRolForm();
            UpdateEmptyState();
        }

        private void TabPermisos_Checked(object sender, RoutedEventArgs e)
        {
            // Guard: this event can fire during XAML parsing.
            if (dgPermisos == null) return;

            _isRolTabActive = false;
            dgRoles.Visibility = Visibility.Collapsed;
            dgPermisos.Visibility = Visibility.Visible;
            panelRolForm.Visibility = Visibility.Collapsed;
            panelPermisoForm.Visibility = Visibility.Visible;
            ClearPermisoForm();
            UpdateEmptyState();
        }

        // ════════════════════════════════════════════════════════════════
        //  FORM HELPERS — ROL
        // ════════════════════════════════════════════════════════════════

        private void ClearRolForm()
        {
            txtRolNombre.Clear();
            txtRolDescripcion.Clear();

            foreach (var item in _allPermisos)
                item.IsSelected = false;
            lstPermisos.ItemsSource = null;
            lstPermisos.ItemsSource = _allPermisos;

            _isEditingRol = false;
            _editingRolId = 0;
            btnEliminarRol.IsEnabled = false;
        }

        private void PopulateRolForm(RolModel rol)
        {
            txtRolNombre.Text = rol.Nombre;
            txtRolDescripcion.Text = rol.Descripcion ?? "";

            _isEditingRol = true;
            _editingRolId = rol.Id;
            btnEliminarRol.IsEnabled = true;

            _ = LoadPermisosForRol(rol.Id);
        }

        private RolModel? GetRolFormData()
        {
            string nombre = txtRolNombre.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("El campo Nombre del rol es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtRolNombre.Focus();
                return null;
            }

            return new RolModel
            {
                Id = _isEditingRol ? _editingRolId : 0,
                Nombre = nombre,
                Descripcion = string.IsNullOrEmpty(txtRolDescripcion.Text.Trim())
                    ? null : txtRolDescripcion.Text.Trim()
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  FORM HELPERS — PERMISO
        // ════════════════════════════════════════════════════════════════

        private void ClearPermisoForm()
        {
            txtPermisoNombre.Clear();
            txtPermisoDescripcion.Clear();

            _isEditingPermiso = false;
            _editingPermisoId = 0;
            btnEliminarPermiso.IsEnabled = false;
        }

        private void PopulatePermisoForm(PermisoModel permiso)
        {
            txtPermisoNombre.Text = permiso.Permiso;
            txtPermisoDescripcion.Text = permiso.Descripcion ?? "";

            _isEditingPermiso = true;
            _editingPermisoId = permiso.Id;
            btnEliminarPermiso.IsEnabled = true;
        }

        private PermisoModel? GetPermisoFormData()
        {
            string nombre = txtPermisoNombre.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("El campo Permiso es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPermisoNombre.Focus();
                return null;
            }

            return new PermisoModel
            {
                Id = _isEditingPermiso ? _editingPermisoId : 0,
                Permiso = nombre,
                Descripcion = string.IsNullOrEmpty(txtPermisoDescripcion.Text.Trim())
                    ? null : txtPermisoDescripcion.Text.Trim()
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  EVENT HANDLERS — ROL
        // ════════════════════════════════════════════════════════════════

        private void dgRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgRoles.SelectedItem is RolModel rol)
            {
                PopulateRolForm(rol);
            }
        }

        private async void BtnGuardarRol_Click(object sender, RoutedEventArgs e)
        {
            var data = GetRolFormData();
            if (data == null) return;

            try
            {
                if (_isEditingRol)
                {
                    await RolesPermisosService.UpdateRol(data);
                    MessageBox.Show("Rol actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await RolesPermisosService.InsertRol(data);
                    MessageBox.Show("Rol creado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Save permission assignments for this role
                int rolId = _isEditingRol ? _editingRolId : await GetLastInsertedRolId();
                if (rolId > 0)
                {
                    await SavePermisoAssignments(rolId);
                }

                await LoadRoles();
                ClearRolForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar rol: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEliminarRol_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditingRol) return;

            var result = MessageBox.Show(
                $"¿Está seguro de eliminar el rol \"{txtRolNombre.Text.Trim()}\"?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await RolesPermisosService.DeleteRol(_editingRolId);
                MessageBox.Show("Rol eliminado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadRoles();
                ClearRolForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar rol: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelarRol_Click(object sender, RoutedEventArgs e)
        {
            ClearRolForm();
            dgRoles.SelectedItem = null;
        }

        // ════════════════════════════════════════════════════════════════
        //  EVENT HANDLERS — PERMISO
        // ════════════════════════════════════════════════════════════════

        private void dgPermisos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPermisos.SelectedItem is PermisoModel permiso)
            {
                PopulatePermisoForm(permiso);
            }
        }

        private async void BtnGuardarPermiso_Click(object sender, RoutedEventArgs e)
        {
            var data = GetPermisoFormData();
            if (data == null) return;

            try
            {
                if (_isEditingPermiso)
                {
                    await RolesPermisosService.UpdatePermiso(data);
                    MessageBox.Show("Permiso actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await RolesPermisosService.InsertPermiso(data);
                    MessageBox.Show("Permiso creado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                await LoadPermisos();
                ClearPermisoForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar permiso: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEliminarPermiso_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditingPermiso) return;

            var result = MessageBox.Show(
                $"¿Está seguro de eliminar el permiso \"{txtPermisoNombre.Text.Trim()}\"?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await RolesPermisosService.DeletePermiso(_editingPermisoId);
                MessageBox.Show("Permiso eliminado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadPermisos();
                ClearPermisoForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar permiso: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelarPermiso_Click(object sender, RoutedEventArgs e)
        {
            ClearPermisoForm();
            dgPermisos.SelectedItem = null;
        }

        // ════════════════════════════════════════════════════════════════
        //  TOOLBAR
        // ════════════════════════════════════════════════════════════════

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            if (_isRolTabActive)
            {
                ClearRolForm();
                txtRolNombre.Focus();
            }
            else
            {
                ClearPermisoForm();
                txtPermisoNombre.Focus();
            }
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            txtBuscar.Clear();
            await Task.WhenAll(LoadRoles(), LoadPermisos());
            ClearRolForm();
            ClearPermisoForm();
        }

        private async void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string term = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(term))
            {
                await Task.WhenAll(LoadRoles(), LoadPermisos());
                return;
            }

            try
            {
                if (_isRolTabActive)
                {
                    var results = await RolesPermisosService.SearchRoles(term);
                    dgRoles.ItemsSource = results;
                }
                else
                {
                    var results = await RolesPermisosService.SearchPermisos(term);
                    dgPermisos.ItemsSource = results;
                }
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  PERMISSION CHECKLIST
        // ════════════════════════════════════════════════════════════════

        private void ChkPermiso_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Permissions are saved when clicking "Guardar", not in real-time
        }

        // ════════════════════════════════════════════════════════════════
        //  PERMISSION ASSIGNMENT SAVING
        // ════════════════════════════════════════════════════════════════

        private async Task SavePermisoAssignments(int rolId)
        {
            try
            {
                var currentlyAssigned = (await RolesPermisosService.GetPermisosByRol(rolId))
                    .Where(rp => rp.Estado == "Activo")
                    .Select(rp => rp.PermisoId)
                    .ToHashSet();

                var desiredAssigned = _allPermisos
                    .Where(p => p.IsSelected)
                    .Select(p => p.PermisoId)
                    .ToHashSet();

                // Permissions to add
                foreach (var permisoId in desiredAssigned)
                {
                    if (!currentlyAssigned.Contains(permisoId))
                    {
                        await RolesPermisosService.AssignPermisoToRol(rolId, permisoId);
                    }
                }

                // Permissions to remove
                foreach (var permisoId in currentlyAssigned)
                {
                    if (!desiredAssigned.Contains(permisoId))
                    {
                        await RolesPermisosService.RemovePermisoFromRol(rolId, permisoId);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar permisos del rol: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Helper to get the last inserted role ID after an INSERT.
        /// Uses a separate connection to query the current sequence value.
        /// </summary>
        private static async Task<int> GetLastInsertedRolId()
        {
            try
            {
                using var conn = await DatabaseConnection.DataSource.OpenConnectionAsync();
                using var cmd = new Npgsql.NpgsqlCommand(
                    "SELECT lastval()", conn);
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch
            {
                return 0;
            }
        }
    }
}
