using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.PopWindows
{
    public partial class PWVerFactura : Window
    {
        public PWVerFactura(FacturaModel factura)
        {
            InitializeComponent();
            this.Loaded += async (s, e) => await LoadFacturaDetail(factura);
        }

        private async System.Threading.Tasks.Task LoadFacturaDetail(FacturaModel factura)
        {
            try
            {
                // Header
                txtTitulo.Text = $"Factura #{factura.Id}";
                txtSubtitulo.Text = $"Emitida el {factura.FechaDisplay}";
                txtFacturaId.Text = $"Factura #{factura.Id}";
                txtFacturaFecha.Text = factura.FechaDisplay;
                txtVentaId.Text = $"Venta #{factura.VentaId}";
                txtCliente.Text = $"Cliente: {factura.ClienteNombre ?? "N/A"}";
                txtNit.Text = $"NIT: {factura.Nit ?? "N/A"}";

                // Montos
                txtSubtotal.Text = $"Bs {factura.SubtotalDisplay}";
                txtDescuento.Text = (factura.Descuento ?? 0).ToString("N2");
                txtTotal.Text = $"Bs {factura.TotalDisplay}";

                // Load detalles and pagos
                var venta = await VentasService.GetVentaById(factura.VentaId);
                if (venta != null)
                {
                    venta.Detalles = await VentasService.GetDetallesByVenta(factura.VentaId);
                    var pagos = await VentasService.GetPagosByVenta(factura.VentaId);

                    // Productos
                    var productos = new List<string>();
                    foreach (var d in venta.Detalles)
                    {
                        productos.Add($"• {d.ProductoNombre} x{d.Cantidad} @ Bs {d.PrecioUnitario:N2} = Bs {d.Subtotal:N2}");
                    }
                    lstProductos.ItemsSource = productos.Count > 0
                        ? productos
                        : new List<string> { "(Sin productos)" };

                    // Pagos
                    var pagosList = new List<string>();
                    decimal totalPagado = 0;
                    foreach (var p in pagos)
                    {
                        string estadoIcon = p.Estado == "Pagado" ? "✅" : p.Estado == "Vencido" ? "❌" : "⏳";
                        pagosList.Add($"{estadoIcon} Bs {p.Monto:N2} - {p.Metodo} ({p.Estado}) - {p.FechaDisplay}");
                        if (p.Estado == "Pagado")
                            totalPagado += p.Monto;
                    }
                    lstPagos.ItemsSource = pagosList.Count > 0
                        ? pagosList
                        : new List<string> { "(Sin pagos)" };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar detalle de factura: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnCerrar_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
