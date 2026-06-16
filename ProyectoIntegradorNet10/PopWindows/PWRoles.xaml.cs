using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWRoles : Window
    {
        private class PermisoCheckItem
        {
            public int PermisoId { get; set; }
            public string PermisoNombre { get; set; } = string.Empty;
            public string PermisoDescripcion { get; set; } = string.Empty;
            public string PermisoIcono { get; set; } = "🔹";
            public bool IsSelected { get; set; }
        }

        private List<PermisoCheckItem> _allPermisos = new();
        private bool _isEditing;
        private int _editingRolId;

        /// <summary>
        /// If set, opens in edit mode for this role.
        /// </summary>
        public RolModel? EditRole { get; set; }

        /// <summary>
        /// Raised when data is saved/deleted so the parent can refresh.
        /// </summary>
        public event Action? OnDataChanged;

        public PWRoles()
        {
            InitializeComponent();
            this.Loaded += PWRoles_Loaded;
        }

        private async void PWRoles_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PWRoles_Loaded;
            await LoadPermisos();

            if (EditRole != null)
            {
                txtTitulo.Text = "Editar Rol";
                txtNombre.Text = EditRole.Nombre;
                txtDescripcion.Text = EditRole.Descripcion ?? "";
                _isEditing = true;
                _editingRolId = EditRole.Id;
                btnEliminar.IsEnabled = true;
                await LoadPermisosForRol(EditRole.Id);
            }
            else
            {
                txtTitulo.Text = "Nuevo Rol";
                btnEliminar.IsEnabled = false;
                txtNombre.Focus();
            }
        }

        private void UpdatePermisosCount()
        {
            if (txtPermisosCount == null || txtPermisosTotal == null) return;
            int total = _allPermisos.Count;
            int selected = _allPermisos.Count(p => p.IsSelected);
            txtPermisosCount.Text = $"{selected} seleccionados";
            txtPermisosTotal.Text = total.ToString();
        }

        private void TxtBuscarPermiso_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (lstPermisos == null || _allPermisos == null) return;
            string term = txtBuscarPermiso.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(term))
            {
                lstPermisos.ItemsSource = _allPermisos;
            }
            else
            {
                lstPermisos.ItemsSource = _allPermisos
                    .Where(p => p.PermisoNombre.ToLower().Contains(term))
                    .ToList();
            }
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (lstPermisos.ItemsSource is System.Collections.IEnumerable items)
            {
                foreach (var item in items)
                {
                    if (item is PermisoCheckItem pci)
                        pci.IsSelected = true;
                }
                lstPermisos.Items.Refresh();
                UpdatePermisosCount();
            }
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            if (lstPermisos.ItemsSource is System.Collections.IEnumerable items)
            {
                foreach (var item in items)
                {
                    if (item is PermisoCheckItem pci)
                        pci.IsSelected = false;
                }
                lstPermisos.Items.Refresh();
                UpdatePermisosCount();
            }
        }

        private void Permiso_Changed(object sender, RoutedEventArgs e)
        {
            UpdatePermisosCount();
        }

        private static string GetIconoForPermiso(string permiso)
        {
            string p = permiso.ToLower();
            if (p.Contains("ver") || p.Contains("visualizar") || p.Contains("leer")) return "👁️";
            if (p.Contains("crear") || p.Contains("agregar") || p.Contains("insertar")) return "➕";
            if (p.Contains("editar") || p.Contains("modificar") || p.Contains("actualizar")) return "✏️";
            if (p.Contains("eliminar") || p.Contains("borrar") || p.Contains("quitar")) return "🗑️";
            if (p.Contains("reporte") || p.Contains("informe")) return "📊";
            if (p.Contains("admin") || p.Contains("config")) return "⚙️";
            if (p.Contains("pago") || p.Contains("factura") || p.Contains("venta")) return "💰";
            if (p.Contains("cliente")) return "👤";
            if (p.Contains("empleado") || p.Contains("usuario")) return "👥";
            if (p.Contains("inventario") || p.Contains("stock") || p.Contains("producto")) return "📦";
            if (p.Contains("vehiculo") || p.Contains("repartidor")) return "🚚";
            return "🔹";
        }

        private void PermisoCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PermisoCheckItem item)
            {
                item.IsSelected = !item.IsSelected;
                UpdatePermisosCount();
                lstPermisos?.Items.Refresh();
            }
        }

        private async Task LoadPermisos()
        {
            try
            {
                var permisos = await RolesPermisosService.GetAllPermisos();
                _allPermisos = permisos.Select(p => new PermisoCheckItem
                {
                    PermisoId = p.Id,
                    PermisoNombre = p.Permiso,
                    PermisoDescripcion = p.Descripcion ?? "",
                    PermisoIcono = GetIconoForPermiso(p.Permiso),
                    IsSelected = false
                }).ToList();
                lstPermisos.ItemsSource = _allPermisos;
                UpdatePermisosCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar permisos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                    item.IsSelected = assignedIds.Contains(item.PermisoId);

                lstPermisos.ItemsSource = null;
                lstPermisos.ItemsSource = _allPermisos;
                UpdatePermisosCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar permisos del rol: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private RolModel? GetFormData()
        {
            string nombre = txtNombre.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("El nombre del rol es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return null;
            }

            return new RolModel
            {
                Id = _isEditing ? _editingRolId : 0,
                Nombre = nombre,
                Descripcion = string.IsNullOrEmpty(txtDescripcion.Text.Trim())
                    ? null : txtDescripcion.Text.Trim()
            };
        }

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

                foreach (var permisoId in desiredAssigned.Except(currentlyAssigned))
                    await RolesPermisosService.AssignPermisoToRol(rolId, permisoId);

                foreach (var permisoId in currentlyAssigned.Except(desiredAssigned))
                    await RolesPermisosService.RemovePermisoFromRol(rolId, permisoId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar permisos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var data = GetFormData();
            if (data == null) return;

            try
            {
                if (_isEditing)
                {
                    await RolesPermisosService.UpdateRol(data);
                    await SavePermisoAssignments(_editingRolId);
                    MessageBox.Show("Rol actualizado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    int newId = await RolesPermisosService.InsertRolAndGetId(data);
                    await SavePermisoAssignments(newId);
                    MessageBox.Show("Rol creado correctamente.", "Éxito",
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
            if (!_isEditing) return;

            var result = MessageBox.Show(
                $"¿Está seguro de eliminar el rol \"{txtNombre.Text.Trim()}\"?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await RolesPermisosService.DeleteRol(_editingRolId);
                MessageBox.Show("Rol eliminado correctamente.", "Éxito",
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
