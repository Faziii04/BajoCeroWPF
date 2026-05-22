using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class FacturacionUC : UserControl
    {
        private List<FacturaModel> _facturas = new();
        private FacturaModel? _selectedFactura;

        public FacturacionUC()
        {
            InitializeComponent();
        }

        private async void FacturacionUC_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFacturas();
        }

        private async System.Threading.Tasks.Task LoadFacturas()
        {
            try
            {
                _facturas = await FacturasService.GetAll();
                dgFacturas.ItemsSource = _facturas;
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar facturas: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateEmptyState()
        {
            txtEmptyState.Visibility = (_facturas == null || _facturas.Count == 0)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ClearDetail()
        {
            _selectedFactura = null;
            txtDetalleId.Text = "ID: ---";
            txtDetalleVenta.Text = "Venta #: ---";
            txtDetalleFecha.Text = "";
            txtDetalleCliente.Text = "";
            txtDetalleNombre.Text = "";
            txtDetalleNit.Text = "";
            txtDetalleSubtotal.Text = "0.00";
            txtDetalleDescuento.Text = "0.00";
            txtDetalleTotal.Text = "0.00";
            lstDetalleProductos.ItemsSource = null;
            lstDetallePagos.ItemsSource = null;
            btnEliminar.IsEnabled = false;
        }

        private async void PopulateDetail(FacturaModel f)
        {
            _selectedFactura = f;
            txtDetalleId.Text = $"ID: {f.Id}";
            txtDetalleVenta.Text = $"Venta #: {f.VentaId}";
            txtDetalleFecha.Text = $"Fecha: {f.FechaDisplay}";
            txtDetalleCliente.Text = $"Cliente: {f.ClienteNombre ?? "N/A"}";
            txtDetalleNombre.Text = $"Nombre: {f.NombreCompleto ?? "N/A"}";
            txtDetalleNit.Text = $"NIT: {f.Nit ?? "N/A"}";
            txtDetalleSubtotal.Text = f.SubtotalDisplay;
            txtDetalleDescuento.Text = (f.Descuento ?? 0).ToString("N2");
            txtDetalleTotal.Text = f.TotalDisplay;

            // Load venta details and pagos for this factura
            try
            {
                var venta = await VentasService.GetVentaById(f.VentaId);
                if (venta != null)
                {
                    venta.Detalles = await VentasService.GetDetallesByVenta(f.VentaId);
                    var pagos = await VentasService.GetPagosByVenta(f.VentaId);

                    // Build productos list
                    var productos = new List<string>();
                    foreach (var d in venta.Detalles)
                    {
                        productos.Add($"• {d.ProductoNombre} x{d.Cantidad} @ {d.PrecioUnitario:N2} = {d.Subtotal:N2}");
                    }
                    lstDetalleProductos.ItemsSource = productos.Count > 0 ? productos : new List<string> { "(Sin productos)" };

                    // Build pagos list
                    var pagosList = new List<string>();
                    decimal totalPagado = 0;
                    foreach (var p in pagos)
                    {
                        string estadoIcon = p.Estado == "Pagado" ? "✅" : p.Estado == "Vencido" ? "❌" : "⏳";
                        pagosList.Add($"{estadoIcon} {p.MontoDisplay} - {p.Metodo} ({p.Estado}) - {p.FechaDisplay}");
                        if (p.Estado == "Pagado")
                            totalPagado += p.Monto;
                    }
                    lstDetallePagos.ItemsSource = pagosList.Count > 0 ? pagosList : new List<string> { "(Sin pagos)" };
                }
            }
            catch
            {
                lstDetalleProductos.ItemsSource = new List<string> { "(Error al cargar detalles)" };
                lstDetallePagos.ItemsSource = new List<string> { "(Error al cargar pagos)" };
            }

            btnEliminar.IsEnabled = true;
        }

        private void dgFacturas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgFacturas.SelectedItem is FacturaModel factura)
            {
                PopulateDetail(factura);
            }
            else
            {
                ClearDetail();
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFactura == null) return;

            var result = MessageBox.Show(
                $"¿Eliminar permanentemente factura #{_selectedFactura.Id} de Venta #{_selectedFactura.VentaId}?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await FacturasService.Delete(_selectedFactura.Id);
                MessageBox.Show("Factura eliminada.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadFacturas();
                ClearDetail();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar factura: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            await LoadFacturas();
            ClearDetail();
        }

        private async void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string term = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(term))
            {
                await LoadFacturas();
                return;
            }

            try
            {
                _facturas = await FacturasService.Search(term);
                dgFacturas.ItemsSource = _facturas;
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
