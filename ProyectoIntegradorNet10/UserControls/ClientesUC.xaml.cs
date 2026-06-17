using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.PopWindows;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class ClientesUC : UserControl
    {
        private List<ClienteModel> _clientes = new();

        public ClientesUC()
        {
            InitializeComponent();
            Loaded += ClientesUC_Loaded;
        }

        private async void ClientesUC_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadClientes();
        }

        private async Task LoadClientes()
        {
            try
            {
                _clientes = await ClientesService.GetAll();
                dgClientes.ItemsSource = _clientes;
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar clientes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateEmptyState()
        {
            txtEmptyState.Visibility = (_clientes == null || _clientes.Count == 0)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var popup = new PWClientes
            {
                Owner = Window.GetWindow(this)
            };
            popup.OnDataChanged += async () => await LoadClientes();
            popup.ShowDialog();
        }

        private void DgClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgClientes.SelectedItem is ClienteModel cliente)
            {
                dgClientes.SelectedItem = null;
                var popup = new PWClientes
                {
                    Owner = Window.GetWindow(this),
                    EditCliente = cliente
                };
                popup.OnDataChanged += async () => await LoadClientes();
                popup.ShowDialog();
            }
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            await LoadClientes();
        }

        private async void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string term = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(term))
            {
                await LoadClientes();
                return;
            }

            try
            {
                _clientes = await ClientesService.Search(term);
                dgClientes.ItemsSource = _clientes;
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
