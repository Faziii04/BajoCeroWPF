using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProyectoIntegradorNet10.Models;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class PagosUC : UserControl
    {
        private List<PagoModel> _pagos = new();
        private PagoModel? _selectedPago;
        private bool _isNewPago;

        /// <summary>
        /// The venta to manage pagos for.
        /// </summary>
        public VentaModel? EditVenta { get; set; }

        /// <summary>
        /// Raised when pagos change so parents can refresh.
        /// </summary>
        public event Action? OnDataChanged;

        public PagosUC()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called by the parent when the tab becomes active or the venta changes.
        /// </summary>
        public async void LoadPagos()
        {
            if (EditVenta == null)
            {
                txtPagoVentaInfo.Text = "No hay venta seleccionada.";
                txtPagoVentaTotal.Text = "";
                txtSummaryTotal.Text = "Bs 0.00";
                txtSummaryPagado.Text = "Bs 0.00";
                txtSummaryPendiente.Text = "Bs 0.00";
                txtPagoCount.Text = "0 pagos";
                dgPagos.ItemsSource = null;
                btnGenerarPagos.Visibility = Visibility.Collapsed;
                btnMarcarPagado.Visibility = Visibility.Collapsed;
                btnGenerarFactura.Visibility = Visibility.Collapsed;
                panelFacturaInfo.Visibility = Visibility.Collapsed;
                ClearPagoForm();
                return;
            }

            try
            {
                var venta = EditVenta;
                txtPagoVentaInfo.Text = $"Venta #{venta.Id} - {venta.ClienteNombre}";
                txtPagoVentaTotal.Text = $"Tipo: {venta.Tipo} | Estado: {venta.Estado}";

                _pagos = await VentasService.GetPagosByVenta(venta.Id);
                dgPagos.ItemsSource = _pagos;
                txtPagoCount.Text = $"{_pagos.Count} pago{(_pagos.Count != 1 ? "s" : "")}";

                // Update summary chips
                txtSummaryTotal.Text = $"Bs {venta.Total:N2}";
                decimal totalPagado = 0;
                foreach (var p in _pagos)
                {
                    if (p.Estado == "Pagado")
                        totalPagado += p.Monto;
                }
                txtSummaryPagado.Text = $"Bs {totalPagado:N2}";
                decimal pendiente = venta.Total - totalPagado;
                txtSummaryPendiente.Text = pendiente > 0 ? $"Bs {pendiente:N2}" : "Bs 0.00";

                // Show "Generar Cuotas" only for Plan de pago with no pagos yet
                btnGenerarPagos.Visibility = (venta.Tipo == "Plan de pago" && (_pagos == null || _pagos.Count == 0))
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                CheckPagoSum();

                // Check for existing factura
                var existingFactura = await FacturasService.GetByVentaId(venta.Id);
                if (existingFactura != null)
                {
                    btnGenerarFactura.Visibility = Visibility.Collapsed;
                    txtFacturaInfo.Text = $"✅ Factura #{existingFactura.Id} emitida\nTotal: {existingFactura.TotalDisplay}";
                    panelFacturaInfo.Visibility = Visibility.Visible;
                }
                else
                {
                    btnGenerarFactura.Visibility = (venta.Estado == "Pagado")
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                    panelFacturaInfo.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar pagos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckPagoSum()
        {
            if (EditVenta == null || _pagos == null || _pagos.Count == 0)
            {
                btnMarcarPagado.Visibility = Visibility.Collapsed;
                return;
            }

            if (EditVenta.Estado == "Pagado")
            {
                btnMarcarPagado.Visibility = Visibility.Collapsed;
                return;
            }

            decimal totalPagado = 0;
            foreach (var p in _pagos)
            {
                if (p.Estado == "Pagado")
                    totalPagado += p.Monto;
            }

            btnMarcarPagado.Visibility = (totalPagado >= EditVenta.Total)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ClearPagoForm()
        {
            _isNewPago = false;
            _selectedPago = null;
            dgPagos.SelectedItem = null;
            txtPagoSubtitle.Text = "Seleccione un pago o cree uno nuevo";
            txtPagoMonto.Text = "";
            cmbPagoMetodo.SelectedIndex = -1;
            cmbPagoEstado.SelectedIndex = -1;
            txtBtnGuardarPago.Text = "Guardar Pago";
        }

        private void dgPagos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPagos.SelectedItem is PagoModel pago)
            {
                _isNewPago = false;
                _selectedPago = pago;
                txtPagoSubtitle.Text = $"Editando Pago #{pago.PagoId}";
                txtPagoMonto.Text = pago.MontoDisplay;

                // Determine button text based on estado
                txtBtnGuardarPago.Text = pago.Estado == "Pagado" ? "Actualizar Pago" : "Actualizar Pago";

                foreach (ComboBoxItem item in cmbPagoMetodo.Items)
                {
                    if (item.Content.ToString() == pago.Metodo)
                    {
                        cmbPagoMetodo.SelectedItem = item;
                        break;
                    }
                }

                foreach (ComboBoxItem item in cmbPagoEstado.Items)
                {
                    if (item.Content.ToString() == pago.Estado)
                    {
                        cmbPagoEstado.SelectedItem = item;
                        break;
                    }
                }

                txtPagoSelectedInfo.Text = $"Pago #{pago.PagoId}\n" +
                    $"Monto: {pago.MontoDisplay}\n" +
                    $"Fecha: {pago.FechaDisplay}\n" +
                    $"Método: {pago.Metodo}\n" +
                    $"Estado: {pago.Estado}";
            }
            else if (!_isNewPago)
            {
                ClearPagoForm();
            }
        }

        private void BtnNuevoPago_Click(object sender, RoutedEventArgs e)
        {
            if (EditVenta == null || EditVenta.Id <= 0)
            {
                MessageBox.Show("No hay una venta seleccionada.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _isNewPago = true;
            _selectedPago = null;
            dgPagos.SelectedItem = null;
            txtPagoSubtitle.Text = $"Nuevo pago para Venta #{EditVenta.Id}";
            txtPagoMonto.Text = "";
            cmbPagoMetodo.SelectedIndex = 0;
            cmbPagoEstado.SelectedIndex = 0;
            txtBtnGuardarPago.Text = "Guardar Pago";
            txtPagoMonto.Focus();
            txtPagoSelectedInfo.Text = "Completando nuevo pago...";
        }

        private async void BtnGuardarPago_Click(object sender, RoutedEventArgs e)
        {
            if (EditVenta == null || EditVenta.Id <= 0)
            {
                MessageBox.Show("No hay una venta seleccionada.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtPagoMonto.Text, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Ingrese un monto válido.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_isNewPago)
                {
                    string estado = (cmbPagoEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pendiente";
                    var nuevoPago = new PagoModel
                    {
                        VentaId = EditVenta.Id,
                        Fecha = estado == "Pagado" ? DateTime.Today : DateTime.Today,
                        Hora = estado == "Pagado" ? DateTime.Now.TimeOfDay : new TimeSpan(0, 0, 0),
                        Monto = monto,
                        Metodo = (cmbPagoMetodo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Efectivo",
                        Estado = estado,
                    };

                    await VentasService.InsertPago(nuevoPago);
                    await UpdateVentaEstadoFromPagos(EditVenta.Id);
                    MessageBox.Show("Pago creado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    if (_selectedPago == null)
                    {
                        MessageBox.Show("Seleccione un pago para actualizar.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _selectedPago.Monto = monto;
                    _selectedPago.Metodo = (cmbPagoMetodo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Efectivo";
                    string nuevoEstado = (cmbPagoEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pendiente";

                    if (nuevoEstado == "Pagado" && _selectedPago.Estado != "Pagado")
                    {
                        _selectedPago.Fecha = DateTime.Today;
                        _selectedPago.Hora = DateTime.Now.TimeOfDay;
                    }

                    _selectedPago.Estado = nuevoEstado;

                    await VentasService.UpdatePago(_selectedPago);
                    await UpdateVentaEstadoFromPagos(_selectedPago.VentaId);
                    MessageBox.Show("Pago actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ClearPagoForm();
                LoadPagos();
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar pago: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task UpdateVentaEstadoFromPagos(int ventaId)
        {
            var pagos = await VentasService.GetPagosByVenta(ventaId);
            if (pagos == null || pagos.Count == 0) return;

            bool allPagados = pagos.All(p => p.Estado == "Pagado");
            bool anyVencido = pagos.Any(p => p.Estado == "Vencido");

            var venta = await VentasService.GetVentaById(ventaId);
            if (venta == null) return;

            if (allPagados)
                venta.Estado = "Pagado";
            else if (anyVencido)
                venta.Estado = "Vencido";
            else
                venta.Estado = "Pendiente";

            await VentasService.UpdateVenta(venta);

            if (EditVenta != null && EditVenta.Id == ventaId)
            {
                EditVenta.Estado = venta.Estado;
            }
        }

        private async void BtnEliminarPago_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPago == null) return;

            var result = MessageBox.Show($"¿Eliminar pago #{_selectedPago.PagoId}?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                await VentasService.DeletePago(_selectedPago.PagoId);
                MessageBox.Show("Pago eliminado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                _selectedPago = null;
                LoadPagos();
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar pago: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnMarcarPagado_Click(object sender, RoutedEventArgs e)
        {
            if (EditVenta == null || EditVenta.Id <= 0) return;

            var result = MessageBox.Show(
                $"¿Marcar venta #{EditVenta.Id} como Pagado?\n" +
                $"Los pagos suman el total requerido.",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                EditVenta.Estado = "Pagado";
                await VentasService.UpdateVenta(EditVenta);
                MessageBox.Show("Venta marcada como Pagado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadPagos();
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar venta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGenerarPagos_Click(object sender, RoutedEventArgs e)
        {
            if (EditVenta == null || EditVenta.Id <= 0) return;
            if (EditVenta.Tipo != "Plan de pago") return;

            int meses = EditVenta.Meses ?? 1;
            if (meses <= 1)
            {
                MessageBox.Show("El plan de pago requiere más de 1 mes.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                await GenerarPagosPlan(EditVenta.Id, EditVenta.Total, meses);
                MessageBox.Show($"Se generaron {meses} cuotas para el plan.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadPagos();
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar pagos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task GenerarPagosPlan(int ventaId, decimal total, int meses)
        {
            decimal montoPorMes = Math.Round(total / meses, 2);
            decimal remainder = total - (montoPorMes * meses);
            DateTime baseDate = DateTime.Today;

            for (int i = 0; i < meses; i++)
            {
                var pago = new PagoModel
                {
                    VentaId = ventaId,
                    Fecha = baseDate.AddMonths(i + 1),
                    Hora = new TimeSpan(0, 0, 0),
                    Monto = (i == meses - 1) ? montoPorMes + remainder : montoPorMes,
                    Metodo = "Efectivo",
                    Estado = "Pendiente",
                };
                await VentasService.InsertPago(pago);
            }
        }

        private async void BtnRefrescarPagos_Click(object sender, RoutedEventArgs e)
        {
            LoadPagos();
        }

        private async void BtnGenerarFactura_Click(object sender, RoutedEventArgs e)
        {
            if (EditVenta == null || EditVenta.Id <= 0) return;

            try
            {
                var existing = await FacturasService.GetByVentaId(EditVenta.Id);
                if (existing != null)
                {
                    MessageBox.Show($"Ya existe una factura para esta venta (ID: {existing.Id}).",
                        "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string? nombreCompleto = null;
                string? nit = null;
                if (!string.IsNullOrEmpty(EditVenta.ClienteCi))
                {
                    var cliente = await ClientesService.GetByCi(EditVenta.ClienteCi);
                    if (cliente != null)
                    {
                        nombreCompleto = cliente.NombreCompleto;
                        nit = cliente.Nit;
                    }
                }

                var factura = new FacturaModel
                {
                    VentaId = EditVenta.Id,
                    Subtotal = EditVenta.Total,
                    Total = EditVenta.Total,
                    Descuento = EditVenta.PorcentajeDescuento,
                    FechaEmision = DateTime.Now,
                    NombreCompleto = nombreCompleto,
                    Nit = nit,
                    DescuentoTipo = EditVenta.PorcentajeDescuento.HasValue && EditVenta.PorcentajeDescuento.Value > 0 ? "Porcentaje" : null,
                };

                await FacturasService.Insert(factura);
                MessageBox.Show($"Factura generada correctamente para Venta #{EditVenta.Id}.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadPagos();
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar factura: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
