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
    public partial class PWRepartidor : Window
    {
        // Concrete class for ComboBox items (anonymous types don't display well in WPF)
        private class EmpleadoComboItem
        {
            public string Ci { get; set; } = string.Empty;
            public string DisplayText { get; set; } = string.Empty;
        }

        private List<RepartidorModel> _allRepartidores = new();
        private List<EmpleadoComboItem> _empleadoItems = new();
        private RepartidorModel? _selectedRepartidor;
        private bool _isEditing;

        /// <summary>
        /// Raised when data changes so the parent can refresh.
        /// </summary>
        public event Action? OnDataChanged;

        public PWRepartidor()
        {
            InitializeComponent();
            this.Loaded += PWRepartidor_Loaded;
        }

        // ────────────────────────────── LOADING ──────────────────────────────

        private async void PWRepartidor_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PWRepartidor_Loaded;
            await LoadEmpleados();
            await LoadRepartidores();
        }

        private async Task LoadEmpleados()
        {
            try
            {
                var empleados = await EmpleadoService.GetAllEmpleados();
                _empleadoItems = empleados.Select(emp => new EmpleadoComboItem
                {
                    Ci = emp.Ci,
                    DisplayText = $"{emp.Ci} - {emp.Nombre} {emp.Apellido}"
                }).ToList();

                cmbEmpleado.ItemsSource = _empleadoItems;
                cmbEmpleado.SelectedValuePath = "Ci";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar empleados: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadRepartidores()
        {
            try
            {
                _allRepartidores = await RepartidorService.GetAll();
                FiltrarLista();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar repartidores: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FiltrarLista()
        {
            string search = txtBuscar.Text?.Trim().ToLower() ?? "";

            var filtered = _allRepartidores;
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(r =>
                    (r.EmpleadoNombre?.ToLower().Contains(search) ?? false) ||
                    (r.Licencia?.ToLower().Contains(search) ?? false) ||
                    (r.Zona?.ToLower().Contains(search) ?? false))
                    .ToList();
            }

            lstRepartidores.ItemsSource = filtered;
            txtEmptyState.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ────────────────────────────── FORM HELPERS ──────────────────────────────

        private void LimpiarFormulario()
        {
            cmbEmpleado.SelectedValue = null;
            txtLicencia.Clear();
            txtZona.Clear();
            cmbEstado.SelectedIndex = 0;
            _isEditing = false;
            _selectedRepartidor = null;
            txtFormTitle.Text = "Nuevo Repartidor";
            btnEliminar.Visibility = Visibility.Collapsed;
        }

        private void PopularFormulario(RepartidorModel r)
        {
            // Select the employee
            if (r.EmpleadoCi != null)
                cmbEmpleado.SelectedValue = r.EmpleadoCi;

            txtLicencia.Text = r.Licencia ?? "";
            txtZona.Text = r.Zona ?? "";

            // Set estado
            if (r.Estado?.Equals("Activo", StringComparison.OrdinalIgnoreCase) == true)
                cmbEstado.SelectedIndex = 0;
            else
                cmbEstado.SelectedIndex = 1;

            _isEditing = true;
            _selectedRepartidor = r;
            txtFormTitle.Text = "Editar Repartidor";
            btnEliminar.Visibility = Visibility.Visible;
        }

        private RepartidorModel? ObtenerDatosFormulario()
        {
            string? empleadoCi = cmbEmpleado.SelectedValue as string;
            string licencia = txtLicencia.Text.Trim();

            if (string.IsNullOrEmpty(empleadoCi))
            {
                MessageBox.Show("Debe seleccionar un empleado.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            if (string.IsNullOrEmpty(licencia))
            {
                MessageBox.Show("El número de licencia es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtLicencia.Focus();
                return null;
            }

            string estado = cmbEstado.SelectedIndex == 0 ? "Activo" : "Inactivo";

            return new RepartidorModel
            {
                Id = _selectedRepartidor?.Id ?? 0,
                EmpleadoCi = empleadoCi,
                Licencia = licencia,
                Zona = string.IsNullOrWhiteSpace(txtZona.Text) ? null : txtZona.Text.Trim(),
                Estado = estado
            };
        }

        // ────────────────────────────── EVENT HANDLERS ──────────────────────────────

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private void LstRepartidores_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstRepartidores.SelectedItem is RepartidorModel r)
            {
                PopularFormulario(r);
            }
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarLista();
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var data = ObtenerDatosFormulario();
            if (data == null) return;

            try
            {
                if (_isEditing)
                {
                    await RepartidorService.Update(data);
                    MessageBox.Show("Repartidor actualizado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await RepartidorService.Insert(data);
                    MessageBox.Show("Repartidor creado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                OnDataChanged?.Invoke();
                LimpiarFormulario();
                await LoadRepartidores();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRepartidor == null) return;

            var result = MessageBox.Show(
                $"¿Está seguro de eliminar al repartidor \"{_selectedRepartidor.EmpleadoNombre}\" (Lic: {_selectedRepartidor.Licencia})?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await RepartidorService.Delete(_selectedRepartidor.Id);
                MessageBox.Show("Repartidor eliminado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                OnDataChanged?.Invoke();
                LimpiarFormulario();
                await LoadRepartidores();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
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
