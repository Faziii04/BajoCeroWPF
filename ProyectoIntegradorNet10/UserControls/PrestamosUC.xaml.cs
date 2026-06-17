using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.PopWindows;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class PrestamosUC : UserControl
    {
        private bool _isInitialized;
        private bool _isLoading;
        private DateTime _lastSearchTick = DateTime.MinValue;

        public PrestamosUC()
        {
            InitializeComponent();
            this.Loaded += PrestamosUC_Loaded;
        }

        private async void PrestamosUC_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PrestamosUC_Loaded;
            await LoadPrestamos();
            _isInitialized = true;
        }

        // ────────────────────────────── DATA LOADING ──────────────────────────────

        private async Task LoadPrestamos()
        {
            _isLoading = true;
            try
            {
                var list = await PrestamosService.GetAll();
                dgPrestamos.ItemsSource = list;
                txtEmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading prestamos: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task ApplyFilters()
        {
            _isLoading = true;
            try
            {
                // Guard: ensure controls are initialized
                if (cmbEstadoFilter == null || txtClienteSearch == null ||
                    dpFechaDesde == null || dpFechaHasta == null)
                    return;

                // Read filter values
                string? estado = null;
                if (cmbEstadoFilter.SelectedItem is ComboBoxItem estItem)
                {
                    string val = estItem.Content?.ToString() ?? "Todos";
                    if (!val.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                        estado = val;
                }

                string? clienteSearch = txtClienteSearch.Text.Trim();
                if (string.IsNullOrWhiteSpace(clienteSearch))
                    clienteSearch = null;

                DateTime? desde = dpFechaDesde.SelectedDate;
                DateTime? hasta = dpFechaHasta.SelectedDate;

                var list = await PrestamosService.GetFiltered(estado, clienteSearch, desde, hasta);
                dgPrestamos.ItemsSource = list;
                txtEmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering prestamos: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        // ────────────────────────────── FILTER EVENTS ──────────────────────────────

        private async void TxtClienteSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Debounce 300ms
            _lastSearchTick = DateTime.UtcNow;
            var captured = _lastSearchTick;
            await Task.Delay(300);
            if (captured != _lastSearchTick) return;

            await ApplyFilters();
        }

        private async void CmbEstadoFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || !_isInitialized) return;
            await ApplyFilters();
        }

        private async void DpFecha_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || !_isInitialized) return;
            await ApplyFilters();
        }

        // ────────────────────────────── ROW SELECTION ──────────────────────────────

        private void DgPrestamos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;

            if (dgPrestamos.SelectedItem is PrestamoModel selected)
            {
                OpenPrestamoPopup(selected.Id);
                // Deselect after opening to allow re-selecting same row
                dgPrestamos.SelectedItem = null;
            }
        }

        // ────────────────────────────── BUTTON HANDLERS ──────────────────────────────

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var popup = new PWPrestamo();
            popup.OnDataChanged += async () => await LoadPrestamos();
            popup.Owner = Window.GetWindow(this);
            popup.ShowDialog();
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            // Reset filters
            txtClienteSearch.Clear();
            cmbEstadoFilter.SelectedIndex = 0;
            dpFechaDesde.SelectedDate = null;
            dpFechaHasta.SelectedDate = null;

            await LoadPrestamos();
        }

        // ────────────────────────────── OPEN POPUP ──────────────────────────────

        private async void OpenPrestamoPopup(int prestamoId)
        {
            try
            {
                var (prestamo, _) = await PrestamosService.GetById(prestamoId);
                if (prestamo == null) return;

                var popup = new PWPrestamo { EditPrestamoId = prestamoId };
                popup.OnDataChanged += async () => await LoadPrestamos();
                popup.Owner = Window.GetWindow(this);
                popup.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening prestamo popup: {ex.Message}");
            }
        }
    }
}
