using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWAsignarRepartidor : Window
    {
        private List<RepartidorModel> _allRepartidores = new();
        private RepartidorVehiculoModel? _activeAssignment;

        /// <summary>
        /// The vehicle plate this popup was opened for.
        /// </summary>
        public string VehiculoPlaca { get; set; } = string.Empty;

        /// <summary>
        /// Vehicle display info (e.g. "ABC123 - Toyota Corolla").
        /// </summary>
        public string VehiculoInfo { get; set; } = string.Empty;

        /// <summary>
        /// Raised when an assignment is made or removed, so the parent can refresh.
        /// </summary>
        public event Action? OnDataChanged;

        public PWAsignarRepartidor()
        {
            InitializeComponent();
            this.Loaded += PWAsignarRepartidor_Loaded;
        }

        private async void PWAsignarRepartidor_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PWAsignarRepartidor_Loaded;

            txtVehiculoInfo.Text = VehiculoInfo;
            await CargarDatos();
        }

        private async Task CargarDatos()
        {
            await Task.WhenAll(
                CargarRepartidoresActivos(),
                CargarAsignacionActual());
        }

        private async Task CargarRepartidoresActivos()
        {
            try
            {
                _allRepartidores = await RepartidorService.GetActivos();
                AplicarFiltro();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar repartidores: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AplicarFiltro()
        {
            string search = txtBuscarRepartidor?.Text?.Trim().ToLower() ?? "";

            var filtered = _allRepartidores;
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(r =>
                    (r.EmpleadoNombre?.ToLower().Contains(search) ?? false) ||
                    (r.EmpleadoCi?.ToLower().Contains(search) ?? false) ||
                    (r.Licencia?.ToLower().Contains(search) ?? false) ||
                    (r.Zona?.ToLower().Contains(search) ?? false))
                    .ToList();
            }

            lstRepartidores.ItemsSource = filtered;
            txtEmptyState.Visibility = filtered.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TxtBuscarRepartidor_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltro();
        }

        private async Task CargarAsignacionActual()
        {
            try
            {
                _activeAssignment = await RepartidorService.GetActiveAssignmentByPlaca(VehiculoPlaca);

                if (_activeAssignment != null)
                {
                    txtAsignacionActual.Text = $"Actualmente asignado a: {_activeAssignment.RepartidorDisplay} " +
                        $"(desde {_activeAssignment.FechaHoraAsignacion:dd/MM/yyyy HH:mm})";
                    btnDesasignar.Visibility = Visibility.Visible;
                }
                else
                {
                    txtAsignacionActual.Text = "Sin asignación activa.";
                    btnDesasignar.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar asignación actual: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnAsignar_Click(object sender, RoutedEventArgs e)
        {
            if (lstRepartidores.SelectedItem is not RepartidorModel selected)
            {
                MessageBox.Show("Seleccione un repartidor de la lista.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await RepartidorService.AssignRepartidorToVehiculo(selected.Id, VehiculoPlaca);
                MessageBox.Show($"Repartidor \"{selected.EmpleadoNombre}\" asignado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                OnDataChanged?.Invoke();
                await CargarDatos();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al asignar repartidor: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDesasignar_Click(object sender, RoutedEventArgs e)
        {
            if (_activeAssignment == null) return;

            var result = MessageBox.Show(
                $"¿Desasignar a \"{_activeAssignment.RepartidorDisplay}\" del vehículo {VehiculoPlaca}?",
                "Confirmar desasignación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await RepartidorService.RemoveAssignment(VehiculoPlaca);
                MessageBox.Show("Asignación eliminada correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                OnDataChanged?.Invoke();
                await CargarDatos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al desasignar: {ex.Message}", "Error",
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
