using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.PopWindows;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class InventarioUC : UserControl
    {
        private List<InventarioModel> _inventario = new();
        private List<InventarioModel> _filteredInventario = new();
        private List<ProductoModel> _productos = new();
        private List<DepositoModel> _depositos = new();
        private bool _isLoading;

        public InventarioUC()
        {
            InitializeComponent();
        }

        private async void InventarioUC_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAll();
        }

        public async System.Threading.Tasks.Task LoadAll()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                var loadInventario = InventarioService.GetAll();
                var loadProductos = InventarioService.GetAllProductos();
                var loadDepositos = InventarioService.GetAllDepositos();

                await System.Threading.Tasks.Task.WhenAll(loadInventario, loadProductos, loadDepositos);

                _inventario = loadInventario.Result;
                _productos = loadProductos.Result;
                _depositos = loadDepositos.Result;

                // Populate filter ComboBoxes
                PopulateFilterCombos();

                ApplyFilters();
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

        private void PopulateFilterCombos()
        {
            // Depósito filter: "Todos" (Id = -1) + depósitos
            var depItems = new List<DepositoModel> { new DepositoModel { Id = -1, Nombre = "Todos los depósitos" } };
            depItems.AddRange(_depositos);
            cmbFiltroDeposito.ItemsSource = depItems;
            if (cmbFiltroDeposito.SelectedIndex == -1)
                cmbFiltroDeposito.SelectedIndex = 0;
        }

        private void ApplyFilters()
        {
            var filtered = _inventario.AsEnumerable();

            // Depósito filter
            if (cmbFiltroDeposito?.SelectedItem is DepositoModel depFilter && depFilter.Id != -1)
            {
                filtered = filtered.Where(i => i.DepositoId == depFilter.Id);
            }

            _filteredInventario = filtered.ToList();
            dgInventario.ItemsSource = _filteredInventario;
            UpdateEmptyState();
        }

        private void Filtro_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void UpdateEmptyState()
        {
            txtEmptyState.Visibility = _filteredInventario == null || _filteredInventario.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            var term = txtBuscar.Text.Trim();
            if (string.IsNullOrWhiteSpace(term))
            {
                await LoadAll();
                return;
            }

            try
            {
                _inventario = await InventarioService.Search(term);
                ApplyFilters();
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGestionar_Click(object sender, RoutedEventArgs e)
        {
            var popup = new PWInventario
            {
                Owner = Window.GetWindow(this),
            };

            popup.OnDataChanged += async () =>
            {
                await LoadAll();
            };

            popup.ShowDialog();
        }

        private void dgInventario_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgInventario.SelectedItem is InventarioModel item)
            {
                var popup = new PWInventario(item)
                {
                    Owner = Window.GetWindow(this),
                };

                popup.OnDataChanged += async () =>
                {
                    await LoadAll();
                };

                popup.ShowDialog();
            }
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            txtBuscar.Text = string.Empty;
            cmbFiltroDeposito.SelectedIndex = 0;
            await LoadAll();
        }
    }
}
