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
    public partial class InsumosUC : UserControl
    {
        private bool _isLoading;
        private DateTime _lastSearchTick = DateTime.MinValue;

        public InsumosUC() { InitializeComponent(); this.Loaded += InsumosUC_Loaded; }

        private async void InsumosUC_Loaded(object sender, RoutedEventArgs e) { this.Loaded -= InsumosUC_Loaded; await LoadInsumos(); }

        private async Task LoadInsumos()
        {
            _isLoading = true;
            try { var list = await InsumosService.GetAll(); icInsumos.ItemsSource = list; txtEmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading insumos: {ex.Message}"); }
            finally { _isLoading = false; }
        }

        private async void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _lastSearchTick = DateTime.UtcNow; var captured = _lastSearchTick; await Task.Delay(300);
            if (captured != _lastSearchTick) return;
            _isLoading = true;
            try { string term = txtBuscar.Text.Trim(); var list = string.IsNullOrWhiteSpace(term) ? await InsumosService.GetAll() : await InsumosService.Search(term); icInsumos.ItemsSource = list; txtEmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error searching: {ex.Message}"); }
            finally { _isLoading = false; }
        }

        private void CardInsumo_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isLoading) return;
            if (sender is Border border && border.DataContext is InsumoModel m)
            {
                var popup = new PWInsumos { EditInsumoId = m.Id };
                popup.OnDataChanged += async () => await LoadInsumos();
                popup.Owner = Window.GetWindow(this); popup.ShowDialog();
            }
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var popup = new PWInsumos();
            popup.OnDataChanged += async () => await LoadInsumos();
            popup.Owner = Window.GetWindow(this); popup.ShowDialog();
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e) { txtBuscar.Clear(); await LoadInsumos(); }
    }
}
