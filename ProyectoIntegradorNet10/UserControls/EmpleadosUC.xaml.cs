using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.PopWindows;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class EmpleadosUC : UserControl
    {
        public EmpleadosUC()
        {
            InitializeComponent();
            this.Loaded += EmpleadosUC_Loaded;
        }

        // ────────────────────────────── DATA LOADING ──────────────────────────────

        private async void EmpleadosUC_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= EmpleadosUC_Loaded;
            await LoadEmpleados();
        }

        private async System.Threading.Tasks.Task LoadEmpleados()
        {
            try
            {
                var empleados = await EmpleadoService.GetAllEmpleados();
                dgEmpleados.ItemsSource = empleados;
                txtEmptyState.Visibility = empleados.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar empleados: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ────────────────────────────── POPUP HELPERS ──────────────────────────────

        private void OpenEmployeePopup(EmpleadoModel? employee = null)
        {
            var popup = new PWEmployees
            {
                Owner = Window.GetWindow(this),
                EditEmployee = employee
            };

            popup.OnDataChanged += async () =>
            {
                await LoadEmpleados();
            };

            popup.ShowDialog();
        }

        // ────────────────────────────── EVENT HANDLERS ──────────────────────────────

        private async void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string term = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(term))
            {
                await LoadEmpleados();
                return;
            }

            try
            {
                var results = await EmpleadoService.SearchEmpleados(term);
                dgEmpleados.ItemsSource = results;
                txtEmptyState.Visibility = results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            OpenEmployeePopup();
        }

        // Single-click row → open popup
        private void dgEmpleados_Seleccionado(object sender, SelectionChangedEventArgs e)
        {
            if (dgEmpleados.SelectedItem is EmpleadoModel emp)
                OpenEmployeePopup(emp);
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            txtBuscar.Clear();
            await LoadEmpleados();
        }
    }
}
