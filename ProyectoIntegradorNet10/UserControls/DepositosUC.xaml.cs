using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class DepositosUC : UserControl
    {
        private List<DepositoModel> _depositos = new();
        private DepositoModel? _selectedDeposito;
        private bool _isEditing;
        private bool _isLoading;

        public DepositosUC()
        {
            InitializeComponent();
        }

        private async void DepositosUC_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDepositos();
        }

        private async Task LoadDepositos()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                _depositos = await DepositosService.GetAll();
                dgDepositos.ItemsSource = _depositos;
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateEmptyState()
        {
            txtEmptyState.Visibility = _depositos == null || _depositos.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClearForm()
        {
            _isEditing = false;
            _selectedDeposito = null;
            txtNombre.Text = string.Empty;
            txtDireccion.Text = string.Empty;
            btnEliminar.IsEnabled = false;
            dgDepositos.SelectedItem = null;
        }

        private void PopulateForm(DepositoModel d)
        {
            _isEditing = true;
            _selectedDeposito = d;
            txtNombre.Text = d.Nombre;
            txtDireccion.Text = d.Direccion ?? string.Empty;
            btnEliminar.IsEnabled = true;
        }

        private DepositoModel? GetFormData()
        {
            var nombre = txtNombre.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("El nombre del depósito es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return null;
            }

            return new DepositoModel
            {
                Id = _selectedDeposito?.Id ?? 0,
                Nombre = nombre,
                Direccion = string.IsNullOrWhiteSpace(txtDireccion.Text) ? null : txtDireccion.Text.Trim(),
            };
        }

        private void dgDepositos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDepositos.SelectedItem is DepositoModel dep)
            {
                PopulateForm(dep);
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var data = GetFormData();
            if (data == null) return;

            try
            {
                if (_isEditing && _selectedDeposito != null && _selectedDeposito.Id > 0)
                {
                    await DepositosService.Update(data);
                    MessageBox.Show("Depósito actualizado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await DepositosService.Insert(data);
                    MessageBox.Show("Depósito creado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ClearForm();
                await LoadDepositos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDeposito == null || _selectedDeposito.Id <= 0) return;

            var result = MessageBox.Show(
                $"¿Eliminar el depósito \"{_selectedDeposito.Nombre}\"?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await DepositosService.Delete(_selectedDeposito.Id);
                MessageBox.Show("Depósito eliminado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                await LoadDepositos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            txtNombre.Focus();
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            txtBuscar.Text = string.Empty;
            ClearForm();
            await LoadDepositos();
        }

        private async void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            var term = txtBuscar.Text.Trim();
            if (string.IsNullOrWhiteSpace(term))
            {
                await LoadDepositos();
                return;
            }

            try
            {
                _depositos = await DepositosService.Search(term);
                dgDepositos.ItemsSource = _depositos;
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
