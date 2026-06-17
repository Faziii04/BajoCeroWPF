using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.PopWindows;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class OrdenesCompraUC : UserControl
    {
        private bool _isInitialized;
        private bool _isLoading;
        private DateTime _lastSearchTick = DateTime.MinValue;

        public OrdenesCompraUC() { InitializeComponent(); this.Loaded += OrdenesCompraUC_Loaded; }

        private async void OrdenesCompraUC_Loaded(object sender, RoutedEventArgs e) { this.Loaded -= OrdenesCompraUC_Loaded; await LoadOrdenes(); _isInitialized = true; }

        private async Task LoadOrdenes()
        {
            _isLoading = true;
            try { var list = await OrdenesCompraService.GetAll(); icOrdenes.ItemsSource = list; txtEmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading ordenes: {ex.Message}"); }
            finally { _isLoading = false; }
        }

        private async Task ApplyFilters()
        {
            if (!_isInitialized) return;
            if (cmbEstadoFilter == null || dpLlegadaDesde == null || dpLlegadaHasta == null) return;

            _isLoading = true;
            try
            {
                string? estado = null;
                if (cmbEstadoFilter.SelectedItem is ComboBoxItem est && est.Content?.ToString() != "Todos")
                    estado = est.Content?.ToString();

                string? provSearch = txtBuscar.Text.Trim();
                if (string.IsNullOrWhiteSpace(provSearch)) provSearch = null;

                DateTime? desde = dpLlegadaDesde.SelectedDate;
                DateTime? hasta = dpLlegadaHasta.SelectedDate;

                var list = await OrdenesCompraService.GetFiltered(estado, provSearch, desde, hasta);
                icOrdenes.ItemsSource = list;
                txtEmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error filtering: {ex.Message}"); }
            finally { _isLoading = false; }
        }

        private async void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _lastSearchTick = DateTime.UtcNow; var captured = _lastSearchTick; await Task.Delay(300);
            if (captured != _lastSearchTick) return;
            await ApplyFilters();
        }

        private async void CmbEstadoFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || !_isInitialized) return;
            await ApplyFilters();
        }

        private async void DpFilter_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || !_isInitialized) return;
            await ApplyFilters();
        }

        private void CardOrden_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isLoading) return;
            if (sender is Border border && border.DataContext is OrdenCompraModel o)
            {
                var popup = new PWOrdenesCompra { EditOrdenId = o.Id };
                popup.OnDataChanged += async () => await LoadOrdenes();
                popup.Owner = Window.GetWindow(this); popup.ShowDialog();
            }
        }

        private async void BtnQuickRecibido_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not OrdenCompraModel o) return;
            if (MessageBox.Show($"¿Recibir orden #{o.Id}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try { await OrdenesCompraService.UpdateEstado(o.Id, "Recibido"); await LoadOrdenes(); }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private async void BtnQuickCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not OrdenCompraModel o) return;
            if (MessageBox.Show($"¿Cancelar orden #{o.Id}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try { await OrdenesCompraService.UpdateEstado(o.Id, "Cancelado"); await LoadOrdenes(); }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var popup = new PWOrdenesCompra();
            popup.OnDataChanged += async () => await LoadOrdenes();
            popup.Owner = Window.GetWindow(this); popup.ShowDialog();
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            txtBuscar.Clear(); cmbEstadoFilter.SelectedIndex = 0; dpLlegadaDesde.SelectedDate = null; dpLlegadaHasta.SelectedDate = null;
            await LoadOrdenes();
        }
    }
}
