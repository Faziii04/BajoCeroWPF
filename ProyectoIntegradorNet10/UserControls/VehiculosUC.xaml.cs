using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class VehiculosUC : UserControl
    {
        private List<VehiculoModel> _allVehiculos = new();
        private VehiculoModel? _selectedVehiculo;
        private bool _isEditing;

        public VehiculosUC()
        {
            InitializeComponent();
        }

        private async void VehiculosUC_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarVehiculos();
        }

        private async System.Threading.Tasks.Task CargarVehiculos()
        {
            try
            {
                _allVehiculos = await VehiculoService.GetAll();
                AplicarFiltro();
                ActualizarDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar vehículos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarDashboard()
        {
            int total = _allVehiculos.Count;
            int vigentes = _allVehiculos.Count(v => v.SoatEstado == "Vigente");
            int porVencer = _allVehiculos.Count(v => v.SoatEstado == "Por vencer");
            int vencidos = _allVehiculos.Count(v => v.SoatEstado == "Vencido");

            txtTotalVehiculos.Text = total.ToString();
            txtSoatVigentes.Text = vigentes.ToString();
            txtSoatPorVencer.Text = porVencer.ToString();
            txtSoatVencidos.Text = vencidos.ToString();

            txtCountTodos.Text = $"({total})";
            txtCountVigentes.Text = $"({vigentes})";
            txtCountPorVencer.Text = $"({porVencer})";
            txtCountVencidos.Text = $"({vencidos})";
        }

        private void AplicarFiltro()
        {
            string searchTerm = txtBuscar.Text?.Trim().ToLower() ?? "";

            IEnumerable<VehiculoModel> filtered = _allVehiculos;

            // Apply SOAT status filter
            if (rbVigentes.IsChecked == true)
                filtered = filtered.Where(v => v.SoatEstado == "Vigente");
            else if (rbPorVencer.IsChecked == true)
                filtered = filtered.Where(v => v.SoatEstado == "Por vencer");
            else if (rbVencidos.IsChecked == true)
                filtered = filtered.Where(v => v.SoatEstado == "Vencido");

            // Apply search term
            if (!string.IsNullOrEmpty(searchTerm))
            {
                filtered = filtered.Where(v =>
                    v.Placa.ToLower().Contains(searchTerm) ||
                    (v.Marca?.ToLower().Contains(searchTerm) ?? false) ||
                    (v.Modelo?.ToLower().Contains(searchTerm) ?? false) ||
                    (v.Tipo?.ToLower().Contains(searchTerm) ?? false));
            }

            var list = filtered.ToList();
            icVehiculos.ItemsSource = list;
            txtEmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Filter events ──

        private void Filtro_Checked(object sender, RoutedEventArgs e)
        {
            if (icVehiculos == null) return; // null guard for XAML parsing
            AplicarFiltro();
        }

        private async void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (icVehiculos == null) return;
            AplicarFiltro();
        }

        // ── Card click → open modal for editing ──

        private void Card_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is VehiculoModel v)
            {
                _selectedVehiculo = v;
                _isEditing = true;
                PopularFormulario(v);
                AbrirModal("Editar Vehículo", true);
            }
        }

        // ── Modal CRUD ──

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            _selectedVehiculo = null;
            _isEditing = false;
            LimpiarFormulario();
            AbrirModal("Nuevo Vehículo", false);
        }

        private void BtnCerrarModal_Click(object sender, RoutedEventArgs e)
        {
            CerrarModal();
        }

        private void AbrirModal(string titulo, bool editing)
        {
            txtModalTitle.Text = titulo;
            btnEliminar.Visibility = editing ? Visibility.Visible : Visibility.Collapsed;
            modalOverlay.Visibility = Visibility.Visible;
        }

        private void CerrarModal()
        {
            modalOverlay.Visibility = Visibility.Collapsed;
            _selectedVehiculo = null;
            _isEditing = false;
        }

        private void LimpiarFormulario()
        {
            txtPlaca.Text = string.Empty;
            txtMarca.Text = string.Empty;
            txtModelo.Text = string.Empty;
            cmbTipo.SelectedIndex = 0;
            txtKilometraje.Text = string.Empty;
            dpSoatVencimiento.SelectedDate = null;
        }

        private void PopularFormulario(VehiculoModel v)
        {
            txtPlaca.Text = v.Placa;
            txtMarca.Text = v.Marca;
            txtModelo.Text = v.Modelo;
            txtKilometraje.Text = v.Kilometraje?.ToString("0");

            // Set Tipo ComboBox
            bool found = false;
            if (!string.IsNullOrEmpty(v.Tipo))
            {
                for (int i = 0; i < cmbTipo.Items.Count; i++)
                {
                    if (cmbTipo.Items[i] is ComboBoxItem item &&
                        item.Content.ToString()!.Equals(v.Tipo, StringComparison.OrdinalIgnoreCase))
                    {
                        cmbTipo.SelectedIndex = i;
                        found = true;
                        break;
                    }
                }
            }
            if (!found)
            {
                cmbTipo.Text = v.Tipo ?? "";
            }

            dpSoatVencimiento.SelectedDate = v.SoatVencimiento;
        }

        private VehiculoModel? ObtenerDatosFormulario()
        {
            string placa = txtPlaca.Text.Trim();
            if (string.IsNullOrEmpty(placa))
            {
                MessageBox.Show("La placa es obligatoria.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            // If creating new, check placa doesn't already exist
            if (!_isEditing)
            {
                if (_allVehiculos.Any(v => v.Placa.Equals(placa, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Ya existe un vehículo con esa placa.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }
            }

            decimal? km = null;
            if (decimal.TryParse(txtKilometraje.Text.Trim(), out decimal kmVal))
                km = kmVal;

            string? tipo = null;
            if (cmbTipo.SelectedItem is ComboBoxItem selectedItem)
                tipo = selectedItem.Content.ToString();
            else if (!string.IsNullOrWhiteSpace(cmbTipo.Text))
                tipo = cmbTipo.Text.Trim();

            return new VehiculoModel
            {
                Placa = placa.ToUpper(),
                Marca = string.IsNullOrWhiteSpace(txtMarca.Text) ? null : txtMarca.Text.Trim(),
                Modelo = string.IsNullOrWhiteSpace(txtModelo.Text) ? null : txtModelo.Text.Trim(),
                Tipo = tipo,
                Kilometraje = km,
                SoatVencimiento = dpSoatVencimiento.SelectedDate,
                UltimaActualizacion = DateTime.Now,
            };
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var data = ObtenerDatosFormulario();
            if (data == null) return;

            try
            {
                if (_isEditing && _selectedVehiculo != null)
                {
                    await VehiculoService.Update(data);
                }
                else
                {
                    await VehiculoService.Insert(data);
                }

                CerrarModal();
                await CargarVehiculos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVehiculo == null) return;

            var result = MessageBox.Show(
                $"¿Eliminar el vehículo con placa {_selectedVehiculo.Placa}?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await VehiculoService.Delete(_selectedVehiculo.Placa);
                CerrarModal();
                await CargarVehiculos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            await CargarVehiculos();
        }
    }
}
