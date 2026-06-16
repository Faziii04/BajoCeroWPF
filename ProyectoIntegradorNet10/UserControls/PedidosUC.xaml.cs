using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class PedidosUC : UserControl
    {
        private VentaModel? _venta;
        private List<RepartidorModel> _repartidores = new();

        public event Action? OnDataChanged;

        public PedidosUC()
        {
            InitializeComponent();
        }

        public async void SetVenta(VentaModel venta)
        {
            _venta = venta;
            await LoadRepartidores();
            LoadVenta();
        }

        private async System.Threading.Tasks.Task LoadRepartidores()
        {
            try
            {
                _repartidores = await RepartidorService.GetActivos();
                cmbRepartidor.ItemsSource = _repartidores;
            }
            catch { }
        }

        private async void LoadVenta()
        {
            if (_venta == null) return;

            txtVentaTitulo.Text = $"Venta #{_venta.Id}";
            txtVentaCliente.Text = $"{_venta.ClienteNombre ?? "—"} • CI: {_venta.ClienteCi ?? "—"}";
            txtEstadoBadge.Text = _venta.Estado ?? "Pedido";
            txtVentaTotal.Text = $"Bs {_venta.TotalDisplay}";
            txtDelivery.Text = _venta.Delivery ? "✅ Sí" : "❌ No";
            txtPagado.Text = _venta.Pagado ? "✅ Sí" : "❌ No";
            txtEntregado.Text = _venta.Entregado ? "✅ Sí" : "❌ No";

            // Repartidor combo
            if (_venta.RepartidorId.HasValue)
            {
                cmbRepartidor.SelectedValue = _venta.RepartidorId.Value;
                var rep = _repartidores.FirstOrDefault(r => r.Id == _venta.RepartidorId.Value);
                txtRepartidorActual.Text = rep != null
                    ? $"Actual: {rep.EmpleadoNombre}"
                    : $"Actual: {_venta.RepartidorNombre ?? "—"}";
            }
            else
            {
                cmbRepartidor.SelectedIndex = -1;
                txtRepartidorActual.Text = "Sin repartidor asignado";
            }

            // Load detalles
            try
            {
                _venta.Detalles = await VentasService.GetDetallesByVenta(_venta.Id);
                lstProductos.ItemsSource = _venta.Detalles;
            }
            catch
            {
                lstProductos.ItemsSource = null;
            }

            // Disable entregado button if already delivered
            btnMarcarEntregado.IsEnabled = !_venta.Entregado;
            btnMarcarEntregado.Opacity = _venta.Entregado ? 0.5 : 1.0;
            if (_venta.Entregado)
                btnMarcarEntregado.Content = "✅ Ya entregado";
            else
                btnMarcarEntregado.Content = "✅ Marcar como Entregado";
        }

        // ──────────── ASSIGN REPARTIDOR ────────────

        private async void BtnAsignarRepartidor_Click(object sender, RoutedEventArgs e)
        {
            if (_venta == null) return;

            int? repartidorId = cmbRepartidor.SelectedValue as int?;
            if (repartidorId == null)
            {
                MessageBox.Show("Seleccione un repartidor de la lista.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                btnAsignarRepartidor.IsEnabled = false;
                btnAsignarRepartidor.Content = "Asignando...";

                _venta.RepartidorId = repartidorId;
                await VentasService.UpdateVenta(_venta);

                // Update display
                var rep = _repartidores.FirstOrDefault(r => r.Id == repartidorId);
                txtRepartidorActual.Text = rep != null ? $"Actual: {rep.EmpleadoNombre}" : "";
                _venta.RepartidorNombre = rep?.EmpleadoNombre;

                OnDataChanged?.Invoke();

                btnAsignarRepartidor.IsEnabled = true;
                btnAsignarRepartidor.Content = "Asignar";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al asignar repartidor: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                btnAsignarRepartidor.IsEnabled = true;
                btnAsignarRepartidor.Content = "Asignar";
            }
        }

        // ──────────── MARK AS DELIVERED ────────────

        private async void BtnMarcarEntregado_Click(object sender, RoutedEventArgs e)
        {
            if (_venta == null || _venta.Entregado) return;

            var result = MessageBox.Show(
                $"¿Marcar venta #{_venta.Id} como entregado?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                btnMarcarEntregado.IsEnabled = false;
                btnMarcarEntregado.Content = "Guardando...";

                _venta.Entregado = true;
                _venta.Estado = "Completado";
                await VentasService.UpdateVenta(_venta);

                MessageBox.Show("Venta marcada como entregada.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                btnMarcarEntregado.Content = "✅ Ya entregado";
                btnMarcarEntregado.Opacity = 0.5;
                OnDataChanged?.Invoke();
                LoadVenta();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                btnMarcarEntregado.IsEnabled = true;
                btnMarcarEntregado.Content = "✅ Marcar como Entregado";
            }
        }
    }
}
