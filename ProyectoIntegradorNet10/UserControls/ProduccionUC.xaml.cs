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
    public partial class ProduccionUC : UserControl
    {
        private bool _isInitialized;
        private bool _isLoading;
        private DateTime _lastSearchTick = DateTime.MinValue;

        public ProduccionUC()
        {
            InitializeComponent();
            this.Loaded += ProduccionUC_Loaded;
        }

        private async void ProduccionUC_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= ProduccionUC_Loaded;
            await LoadProducciones();
            _isInitialized = true;
        }

        private async Task LoadProducciones()
        {
            _isLoading = true;
            try
            {
                var list = await ProduccionService.GetAll();
                icProduccion.ItemsSource = list;
                txtEmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading produccion: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task ApplyFilters()
        {
            if (!_isInitialized) return;

            _isLoading = true;
            try
            {
                string? estado = null;
                if (cmbEstadoFilter.SelectedItem is ComboBoxItem est && est.Content?.ToString() != "Todos")
                    estado = est.Content?.ToString();

                DateTime? desde = dpDesde.SelectedDate;
                DateTime? hasta = dpHasta.SelectedDate;

                string term = txtBuscar?.Text?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(term))
                {
                    var list = await ProduccionService.Search(term);
                    icProduccion.ItemsSource = list;
                    txtEmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    var list = await ProduccionService.GetFiltered(estado, desde, hasta);
                    icProduccion.ItemsSource = list;
                    txtEmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        // ──────────── SEARCH & FILTER EVENTS ────────────

        private async void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading) return;
            _lastSearchTick = DateTime.UtcNow;
            var captured = _lastSearchTick;
            await Task.Delay(300);
            if (captured != _lastSearchTick) return;

            string term = txtBuscar.Text.Trim();
            if (string.IsNullOrWhiteSpace(term))
            {
                await ApplyFilters();
                return;
            }

            try
            {
                var list = await ProduccionService.Search(term);
                icProduccion.ItemsSource = list;
                txtEmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching: {ex.Message}");
            }
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

        // ──────────── CARD CLICK → POPUP ────────────

        private void CardProduccion_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isLoading) return;
            if (sender is Border border && border.DataContext is ProduccionModel prod)
            {
                var popup = new PWProduccion { EditProduccionId = prod.Id };
                popup.OnDataChanged += async () => await LoadProducciones();
                popup.Owner = Window.GetWindow(this);
                popup.ShowDialog();
            }
        }

        // ──────────── QUICK ACTIONS ────────────

        private async void BtnQuickIniciar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not ProduccionModel prod) return;

            var stockError = await ProduccionService.ValidateStock(prod.Id);
            if (stockError != null)
            {
                MessageBox.Show(stockError, "Stock insuficiente",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"¿Iniciar producción #{prod.Id}?\n\nSe descontarán los insumos del stock.",
                    "Confirmar inicio", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var (success, error) = await ProduccionService.Iniciar(prod.Id);
            if (success)
                await LoadProducciones();
            else
                MessageBox.Show(error ?? "Error desconocido", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async void BtnQuickCompletar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not ProduccionModel prod) return;

            var depositos = await DepositosService.GetAll();
            if (depositos.Count == 0)
            {
                MessageBox.Show("No hay depósitos registrados. Cree un depósito primero.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Modal picker window for deposito selection
            var picker = new Window
            {
                Title = "Seleccionar Depósito",
                Width = 350,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
            };

            var stack = new StackPanel { Margin = new Thickness(16) };
            stack.Children.Add(new TextBlock
            {
                Text = $"Seleccione el depósito destino para los productos de #{prod.Id}:",
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 12),
                TextWrapping = TextWrapping.Wrap
            });

            var combo = new ComboBox
            {
                ItemsSource = depositos,
                DisplayMemberPath = "Nombre",
                SelectedValuePath = "Id",
                Height = 32,
                FontSize = 13,
            };
            combo.SelectedIndex = 0;
            stack.Children.Add(combo);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };

            bool confirmed = false;
            var btnOk = new Button
            {
                Content = "Completar",
                Height = 36,
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            btnOk.Click += (s, args) => { confirmed = true; picker.Close(); };

            var btnCancel = new Button
            {
                Content = "Cancelar",
                Height = 36,
                Padding = new Thickness(16, 8, 16, 8),
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, args) => picker.Close();

            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            stack.Children.Add(btnPanel);
            picker.Content = stack;
            picker.ShowDialog();

            if (!confirmed) return;

            int depositoId = (int)combo.SelectedValue;
            var (success, error) = await ProduccionService.Completar(prod.Id, depositoId);
            if (success)
            {
                MessageBox.Show($"Producción #{prod.Id} completada.\nProductos añadidos al depósito \"{combo.Text}\".", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadProducciones();
            }
            else
            {
                MessageBox.Show(error ?? "Error desconocido", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnQuickCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not ProduccionModel prod) return;

            string message = prod.Estado == "Planificado"
                ? $"¿Cancelar producción planificada #{prod.Id}?"
                : $"¿Cancelar producción #{prod.Id}?\n\nSe revertirán los descuentos de insumos del stock.";

            if (MessageBox.Show(message, "Confirmar cancelación",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            var (success, error) = await ProduccionService.Cancelar(prod.Id);
            if (success)
                await LoadProducciones();
            else
                MessageBox.Show(error ?? "Error desconocido", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // ──────────── NUEVO / REFRESCAR ────────────

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var popup = new PWProduccion();
            popup.OnDataChanged += async () => await LoadProducciones();
            popup.Owner = Window.GetWindow(this);
            popup.ShowDialog();
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            txtBuscar.Clear();
            cmbEstadoFilter.SelectedIndex = 0;
            dpDesde.SelectedDate = null;
            dpHasta.SelectedDate = null;
            await LoadProducciones();
        }
    }
}
