using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProyectoIntegradorNet10.PopWindows;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class RolesPermisosUC : UserControl
    {
        public RolesPermisosUC()
        {
            InitializeComponent();
        }

        private async void RolesPermisosUC_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= RolesPermisosUC_Loaded;
            await LoadRoles();
        }

        private async System.Threading.Tasks.Task LoadRoles()
        {
            try
            {
                var roles = await RolesPermisosService.GetAllRoles();
                dgRoles.ItemsSource = roles;
                var source = roles as System.Collections.ICollection;
                int count = source?.Count ?? 0;
                txtEmptyState.Visibility = (count == 0) ? Visibility.Visible : Visibility.Collapsed;
                panelEmptyState.Visibility = (count == 0) ? Visibility.Visible : Visibility.Collapsed;
                txtRolesCount.Text = count > 0 ? $"({count} rol{(count != 1 ? "es" : "")})" : "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar roles: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AbrirPWRoles(Models.RolModel? rol = null)
        {
            var popup = new PWRoles
            {
                Owner = Window.GetWindow(this),
                EditRole = rol
            };
            popup.OnDataChanged += async () => await LoadRoles();
            popup.ShowDialog();
        }

        // ── Toolbar ──

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            AbrirPWRoles();
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            txtBuscar.Clear();
            await LoadRoles();
        }

        private async void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string term = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(term))
            {
                await LoadRoles();
                return;
            }

            try
            {
                var results = await RolesPermisosService.SearchRoles(term);
                dgRoles.ItemsSource = results;
                var source = results as System.Collections.ICollection;
                bool empty = source == null || source.Count == 0;
                txtEmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
                panelEmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
                if (empty)
                    txtEmptyState.Text = $"No se encontraron roles para \"{term}\".";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── DataGrid selection & double-click ──

        private void DgRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgRoles.SelectedItem is Models.RolModel rol)
            {
                dgRoles.SelectedItem = null;
                AbrirPWRoles(rol);
            }
        }

        // ── Inline row actions ──

        private void BtnEditarInline_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is int rolId)
            {
                var rol = (dgRoles.ItemsSource as System.Collections.IEnumerable)
                    ?.Cast<Models.RolModel>()
                    ?.FirstOrDefault(r => r.Id == rolId);
                if (rol != null) AbrirPWRoles(rol);
            }
        }

        private async void BtnEliminarInline_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is int rolId)
            {
                var rol = (dgRoles.ItemsSource as System.Collections.IEnumerable)
                    ?.Cast<Models.RolModel>()
                    ?.FirstOrDefault(r => r.Id == rolId);
                if (rol == null) return;

                var result = MessageBox.Show(
                    $"¿Está seguro de eliminar el rol \"{rol.Nombre}\"?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                try
                {
                    await RolesPermisosService.DeleteRol(rolId);
                    MessageBox.Show("Rol eliminado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadRoles();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
