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
                txtEmptyState.Visibility = (source == null || source.Count == 0)
                    ? Visibility.Visible : Visibility.Collapsed;
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
                txtEmptyState.Visibility = (source == null || source.Count == 0)
                    ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── DataGrid ──

        private void dgRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Can be used for preview if needed
        }

        private void dgRoles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgRoles.SelectedItem is Models.RolModel rol)
            {
                AbrirPWRoles(rol);
            }
        }
    }
}
