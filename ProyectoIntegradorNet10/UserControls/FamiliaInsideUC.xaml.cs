using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.PopWindows;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class FamiliaUC : UserControl
    {
        private List<FamiliaModel> _familias = new();
        private bool _isLoading;
        private bool _suspendSearch;

        public FamiliaUC()
        {
            InitializeComponent();
        }

        private async void FamiliaUC_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFamilias();
        }

        private async Task LoadFamilias()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                _familias = await ProductoFamiliaService.GetAll();
                icFamilias.ItemsSource = _familias;
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar familias: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateEmptyState()
        {
            int count = _familias?.Count ?? 0;
            bool empty = count == 0;
            panelEmptyFamilias.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            txtFamiliaCount.Text = empty ? "Sin familias" : $"{count} familia{(count != 1 ? "s" : "")}";
        }

        private void OpenFamiliaPopup(FamiliaModel? familia = null)
        {
            var popup = new PWFamlias
            {
                Owner = Window.GetWindow(this),
                EditFamilia = familia
            };

            popup.OnDataChanged += async () =>
            {
                await LoadFamilias();
            };

            popup.ShowDialog();
        }

        // ──────────────── EVENT HANDLERS ────────────────

        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is FamiliaModel familia)
            {
                OpenFamiliaPopup(familia);
            }
        }

        private void BtnNuevaFamilia_Click(object sender, RoutedEventArgs e)
        {
            OpenFamiliaPopup();
        }

        private async void BtnRefrescarFamilia_Click(object sender, RoutedEventArgs e)
        {
            _suspendSearch = true;
            txtBuscarFamilia.Text = string.Empty;
            _suspendSearch = false;
            await LoadFamilias();
        }

        private async void TxtBuscarFamilia_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suspendSearch) return;

            var term = txtBuscarFamilia.Text.Trim();
            if (string.IsNullOrWhiteSpace(term))
            {
                await LoadFamilias();
                return;
            }

            try
            {
                _familias = await ProductoFamiliaService.Search(term);
                icFamilias.ItemsSource = _familias;
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar familias: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
