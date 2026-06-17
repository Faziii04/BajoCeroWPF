using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWOrdenesCompra : Window
    {
        public event Action? OnDataChanged;
        public int EditOrdenId { get; set; }

        private List<ProveedorModel> _proveedores = new();
        private List<InsumoModel> _insumos = new();
        private ObservableCollection<DetalleOrdenModel> _detalles = new();
        private bool _isEditing;

        public PWOrdenesCompra() { InitializeComponent(); this.Loaded += PWOrdenesCompra_Loaded; }

        private void CmbProveedor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProveedor.SelectedItem is ProveedorModel prov)
                cmbProveedor.Text = prov.Nombre;
        }

        private async void PWOrdenesCompra_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PWOrdenesCompra_Loaded;
            try
            {
                _proveedores = await ProveedoresService.GetAll();
                cmbProveedor.ItemsSource = _proveedores;
                _insumos = await InsumosService.GetAll();
                cmbInsumoAdd.ItemsSource = _insumos;
            }
            catch { }

            lstInsumos.ItemsSource = _detalles;

            if (EditOrdenId > 0)
            {
                txtTitulo.Text = $"Orden #{EditOrdenId}"; _isEditing = true; btnEliminar.IsEnabled = true;
                var (orden, detalles) = await OrdenesCompraService.GetById(EditOrdenId);
                if (orden != null)
                {
                    dpFechaPedido.SelectedDate = orden.FechaPedido;
                    txtHoraPedido.Text = orden.HoraPedidoDisplay;
                    if (orden.FechaLlegada.HasValue) dpFechaLlegada.SelectedDate = orden.FechaLlegada;
                    if (orden.HoraLlegada.HasValue) txtHoraLlegada.Text = orden.HoraLlegadaDisplay;
                    cmbProveedor.SelectedValue = orden.ProveedorId;
                    foreach (ComboBoxItem item in cmbEstado.Items)
                        if (string.Equals(item.Content?.ToString(), orden.Estado, StringComparison.OrdinalIgnoreCase)) { cmbEstado.SelectedItem = item; break; }
                    foreach (var d in detalles) _detalles.Add(d);
                    UpdateTotal();
                }
            }
            else
            {
                dpFechaPedido.SelectedDate = DateTime.Today;
                txtHoraPedido.Text = DateTime.Now.ToString("HH:mm");
            }
        }

        private void BtnAddInsumo_Click(object sender, RoutedEventArgs e)
        {
            if (cmbInsumoAdd.SelectedItem is not InsumoModel insumo) { MessageBox.Show("Seleccione un insumo.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (!decimal.TryParse(txtCantidadAdd.Text, out decimal cant) || cant <= 0) { MessageBox.Show("Cantidad inválida.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            var existente = _detalles.FirstOrDefault(d => d.InsumoId == insumo.Id);
            if (existente != null) { existente.Cantidad = (existente.Cantidad ?? 0) + cant; RefreshList(); }
            else { _detalles.Add(new DetalleOrdenModel { InsumoId = insumo.Id, InsumoNombre = insumo.Nombre, InsumoPrecio = insumo.PrecioUnitario, Cantidad = cant }); }

            UpdateTotal();
            cmbInsumoAdd.SelectedItem = null; cmbInsumoAdd.Text = ""; txtCantidadAdd.Text = "1";
        }

        private void RemoveInsumo_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock tb && tb.DataContext is DetalleOrdenModel d) { _detalles.Remove(d); UpdateTotal(); }
        }

        private void RefreshList()
        {
            var items = _detalles.ToList();
            _detalles.Clear();
            foreach (var i in items) _detalles.Add(i);
        }

        private void UpdateTotal()
        {
            decimal total = _detalles.Sum(d => d.Cantidad.GetValueOrDefault() * d.InsumoPrecio.GetValueOrDefault());
            txtTotal.Text = $"Bs {total:N2}";
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProveedor.SelectedItem is not ProveedorModel prov) { MessageBox.Show("Seleccione un proveedor.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (!dpFechaPedido.SelectedDate.HasValue) { MessageBox.Show("Seleccione una fecha de pedido.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (!TimeSpan.TryParse(txtHoraPedido.Text.Trim(), out TimeSpan horaPedido)) { MessageBox.Show("Hora de pedido inválida (use HH:mm).", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            TimeSpan? horaLlegada = null;
            if (!string.IsNullOrWhiteSpace(txtHoraLlegada.Text))
            {
                if (!TimeSpan.TryParse(txtHoraLlegada.Text.Trim(), out var hl)) { MessageBox.Show("Hora de llegada inválida (use HH:mm).", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                horaLlegada = hl;
            }

            var valid = _detalles.Where(d => d.InsumoId > 0 && d.Cantidad.GetValueOrDefault() > 0).ToList();
            if (valid.Count == 0) { MessageBox.Show("Agregue al menos un insumo.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            string estado = "Pendiente";
            if (cmbEstado.SelectedItem is ComboBoxItem est) estado = est.Content?.ToString() ?? "Pendiente";

            try
            {
                btnGuardar.IsEnabled = false;
                if (_isEditing)
                {
                    await OrdenesCompraService.Update(EditOrdenId, prov.Id, estado, dpFechaPedido.SelectedDate.Value, horaPedido, valid, dpFechaLlegada.SelectedDate, horaLlegada);
                    MessageBox.Show("Orden actualizada.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await OrdenesCompraService.Insert(prov.Id, estado, dpFechaPedido.SelectedDate.Value, horaPedido, valid, dpFechaLlegada.SelectedDate, horaLlegada);
                    MessageBox.Show("Orden creada.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                OnDataChanged?.Invoke(); this.Close();
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); btnGuardar.IsEnabled = true; }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditing) return;
            if (MessageBox.Show($"¿Eliminar orden #{EditOrdenId}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try { await OrdenesCompraService.Delete(EditOrdenId); MessageBox.Show("Orden eliminada.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information); OnDataChanged?.Invoke(); this.Close(); }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnAhoraPedido_Click(object sender, RoutedEventArgs e)
        {
            dpFechaPedido.SelectedDate = DateTime.Today;
            txtHoraPedido.Text = DateTime.Now.ToString("HH:mm");
        }

        private void BtnAhoraLlegada_Click(object sender, RoutedEventArgs e)
        {
            dpFechaLlegada.SelectedDate = DateTime.Today;
            txtHoraLlegada.Text = DateTime.Now.ToString("HH:mm");
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();
        private void BtnCerrar_Click(object sender, MouseButtonEventArgs e) => this.Close();
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) this.DragMove(); }
    }
}
