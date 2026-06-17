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
    public partial class ProveedoresUC : UserControl
    {
        private bool _isLoading;
        private DateTime _lastSearchTick = DateTime.MinValue;

        public ProveedoresUC() { InitializeComponent(); this.Loaded += ProveedoresUC_Loaded; }

        private async void ProveedoresUC_Loaded(object sender, RoutedEventArgs e) { this.Loaded -= ProveedoresUC_Loaded; await LoadProveedores(); }

        private async Task LoadProveedores()
        {
            _isLoading = true;
            try { var list = await ProveedoresService.GetAll(); icProveedores.ItemsSource = list; txtEmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading proveedores: {ex.Message}"); }
            finally { _isLoading = false; }
        }

        private async void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _lastSearchTick = DateTime.UtcNow; var captured = _lastSearchTick; await Task.Delay(300);
            if (captured != _lastSearchTick) return;
            _isLoading = true;
            try { string term = txtBuscar.Text.Trim(); var list = string.IsNullOrWhiteSpace(term) ? await ProveedoresService.GetAll() : await ProveedoresService.Search(term); icProveedores.ItemsSource = list; txtEmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error searching: {ex.Message}"); }
            finally { _isLoading = false; }
        }

        private void CardProveedor_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isLoading) return;
            if (sender is Border border && border.DataContext is ProveedorModel p)
            {
                var popup = new PWProveedores { EditProveedorId = p.Id };
                popup.OnDataChanged += async () => await LoadProveedores();
                popup.Owner = Window.GetWindow(this); popup.ShowDialog();
            }
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var popup = new PWProveedores();
            popup.OnDataChanged += async () => await LoadProveedores();
            popup.Owner = Window.GetWindow(this); popup.ShowDialog();
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e) { txtBuscar.Clear(); await LoadProveedores(); }
    }
}
