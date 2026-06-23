using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ClosedXML.Excel;
using ProyectoIntegradorNet10.Services;

namespace ProyectoIntegradorNet10.UserControls
{
    public partial class ReportesUC : UserControl
    {
        private Button? _activeReportButton;
        private IEnumerable? _lastLoadedData;
        private bool _isLoading;

        public ReportesUC()
        {
            InitializeComponent();
            dpDesde.SelectedDate = DateTime.Today.AddDays(-30);
            dpHasta.SelectedDate = DateTime.Today;
        }

        /// <summary>Reports that use date range filters.</summary>
        private static readonly HashSet<string> DateSensitiveReports = new(StringComparer.OrdinalIgnoreCase)
        {
            "Ingresos", "Productos", "Ventas", "Facturacion", "Produccion", "Ordenes",
        };

        // ──────────────────────────────────────────────
        //  Report type selection
        // ──────────────────────────────────────────────

        private async void BtnReporte_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || _isLoading) return;

            SetActiveReportButton(btn);

            var tag = btn.Tag?.ToString() ?? "";
            var (icon, title) = GetReportMeta(tag);
            txtReportIcon.Text = icon;
            txtReportTitle.Text = title;
            txtReportSubtitle.Text = "Cargando datos...";

            await LoadReport(tag);
        }

        private void SetActiveReportButton(Button button)
        {
            if (_activeReportButton != null && _activeReportButton != button)
            {
                _activeReportButton.ClearValue(BackgroundProperty);
                _activeReportButton.ClearValue(BorderBrushProperty);
                _activeReportButton.ClearValue(BorderThicknessProperty);
                _activeReportButton.Foreground = (Brush)FindResource("NavTextColor");
            }
            _activeReportButton = button;
            _activeReportButton.Background = (Brush)FindResource("GridRowSelectedBrush");
            _activeReportButton.Foreground = Brushes.White;
            _activeReportButton.BorderBrush = (Brush)FindResource("NavTextColor");
            _activeReportButton.BorderThickness = new Thickness(2, 0, 0, 0);
        }

        private static (string icon, string title) GetReportMeta(string tag)
        {
            return tag switch
            {
                "Ingresos"    => ("📊", "Ingresos"),
                "Productos"   => ("📦", "Productos más vendidos"),
                "Inventario"  => ("📋", "Inventario actual"),
                "Clientes"    => ("👥", "Clientes frecuentes"),
                "Empleados"   => ("👤", "Empleados por área"),
                "Ventas"      => ("💰", "Ventas por período"),
                "Facturacion" => ("🧾", "Facturación"),
                "Vehiculos"   => ("🚛", "Vehículos"),
                "Depositos"   => ("🏭", "Depósitos"),
                "Produccion"  => ("🏗️", "Producción"),
                "Ordenes"     => ("📑", "Órdenes de Compra"),
                _             => ("📊", "Reporte"),
            };
        }

        // ──────────────────────────────────────────────
        //  Data loading
        // ──────────────────────────────────────────────

        private async Task LoadReport(string tag)
        {
            _isLoading = true;
            badgeLoading.Visibility = Visibility.Visible;
            emptyState.Visibility = Visibility.Collapsed;
            kpiBar.Visibility = Visibility.Collapsed;
            chartBar.Visibility = Visibility.Collapsed;
            chartCanvas.Children.Clear();

            // Show/hide date filter bar based on report type
            dateFilterBar.Visibility = DateSensitiveReports.Contains(tag)
                ? Visibility.Visible : Visibility.Collapsed;

            try
            {
                DateTime? desde = dpDesde.SelectedDate;
                DateTime? hasta = dpHasta.SelectedDate;

                switch (tag)
                {
                    case "Ingresos":
                    {
                        var data = await ReportesService.GetIngresos(desde, hasta);
                        BuildRevenueChart(data);
                        BindDataGrid(data, GetIngresosColumns());
                        UpdateKpiIngresos(data);
                        chartBar.Visibility = Visibility.Visible;
                        break;
                    }
                    case "Productos":
                    {
                        var data = await ReportesService.GetProductosMasVendidos(desde, hasta);
                        BindDataGrid(data, GetProductosColumns());
                        UpdateKpiProductos(data);
                        break;
                    }
                    case "Inventario":
                    {
                        var data = await ReportesService.GetInventarioActual();
                        BindDataGrid(data, GetInventarioColumns());
                        UpdateKpiInventario(data);
                        break;
                    }
                    case "Clientes":
                    {
                        var data = await ReportesService.GetClientesFrecuentes();
                        BindDataGrid(data, GetClientesColumns());
                        UpdateKpiClientes(data);
                        break;
                    }
                    case "Empleados":
                    {
                        var data = await ReportesService.GetEmpleadosPorArea();
                        BindDataGrid(data, GetEmpleadosColumns());
                        UpdateKpiEmpleados(data);
                        break;
                    }
                    case "Ventas":
                    {
                        var data = await ReportesService.GetVentasPorPeriodo(desde, hasta);
                        BindDataGrid(data, GetVentasColumns());
                        UpdateKpiVentas(data);
                        break;
                    }
                    case "Facturacion":
                    {
                        var data = await ReportesService.GetFacturacion(desde, hasta);
                        BindDataGrid(data, GetFacturacionColumns());
                        UpdateKpiFacturacion(data);
                        break;
                    }
                    case "Vehiculos":
                    {
                        var data = await ReportesService.GetVehiculos();
                        BindDataGrid(data, GetVehiculosColumns());
                        UpdateKpiVehiculos(data);
                        break;
                    }
                    case "Depositos":
                    {
                        var data = await ReportesService.GetDepositos();
                        BindDataGrid(data, GetDepositosColumns());
                        UpdateKpiDepositos(data);
                        break;
                    }
                    case "Produccion":
                    {
                        var data = await ReportesService.GetProduccion(desde, hasta);
                        BindDataGrid(data, GetProduccionColumns());
                        UpdateKpiProduccion(data);
                        break;
                    }
                    case "Ordenes":
                    {
                        var data = await ReportesService.GetOrdenesCompra(desde, hasta);
                        BindDataGrid(data, GetOrdenesColumns());
                        UpdateKpiOrdenes(data);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el reporte: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                emptyState.Visibility = Visibility.Visible;
                kpiBar.Visibility = Visibility.Collapsed;
                chartBar.Visibility = Visibility.Collapsed;
            }
            finally
            {
                _isLoading = false;
                badgeLoading.Visibility = Visibility.Collapsed;
            }
        }

        // ──────────────────────────────────────────────
        //  Chart builder (WPF Canvas bars)
        // ──────────────────────────────────────────────

        private void BuildRevenueChart(List<ReporteIngresoDiario> data)
        {
            chartCanvas.Children.Clear();
            if (data.Count == 0) return;

            chartTitle.Text = "Ingresos por día";
            chartBar.Visibility = Visibility.Visible;

            // Wait for layout so we can measure ActualWidth/Height
            chartBar.UpdateLayout();
            chartCanvas.UpdateLayout();

            // Defer drawing until canvas has size
            Dispatcher.BeginInvoke(new Action(() => DrawBars(data)));
        }

        private void DrawBars(List<ReporteIngresoDiario> data)
        {
            chartCanvas.Children.Clear();
            double w = chartCanvas.ActualWidth;
            double h = chartCanvas.ActualHeight;
            if (w <= 0 || h <= 0 || data.Count == 0) return;

            var maxVal = (double)data.Max(d => d.Total);
            if (maxVal <= 0) maxVal = 1;
            double barW = Math.Max(8, (w / data.Count) * 0.7);
            double gap = w / data.Count;
            double padding = (gap - barW) / 2;

            var accentBrush = (Brush)FindResource("AcentoBrush");
            var labelBrush = (Brush)FindResource("SectionLabelColor");
            var textBrush = (Brush)FindResource("NavTextColor");

            double labelH = 20;
            double chartH = h - labelH;

            for (int i = 0; i < data.Count; i++)
            {
                double barH = (double)data[i].Total / maxVal * chartH;
                double x = padding + i * gap;
                double y = chartH - barH;

                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = barW,
                    Height = Math.Max(1, barH),
                    Fill = accentBrush,
                    RadiusX = 2,
                    RadiusY = 2,
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                chartCanvas.Children.Add(rect);

                // Date label
                if (data.Count <= 31 || i % Math.Max(1, data.Count / 15) == 0)
                {
                    var lbl = new TextBlock
                    {
                        Text = data[i].Fecha.ToString("dd/MM"),
                        FontSize = 9,
                        Foreground = labelBrush,
                        TextAlignment = TextAlignment.Center,
                        Width = gap,
                    };
                    Canvas.SetLeft(lbl, x - padding);
                    Canvas.SetTop(lbl, chartH + 2);
                    chartCanvas.Children.Add(lbl);
                }
            }
        }

        // ──────────────────────────────────────────────
        //  DataGrid binding
        // ──────────────────────────────────────────────

        private void BindDataGrid(IEnumerable data, List<DataGridColumn> columns)
        {
            _lastLoadedData = data;

            dgReportes.Columns.Clear();
            foreach (var col in columns)
                dgReportes.Columns.Add(col);

            dgReportes.ItemsSource = data;

            var count = (data as ICollection)?.Count ?? 0;
            txtReportSubtitle.Text = $"{count} registro{(count == 1 ? "" : "s")} encontrado{(count == 1 ? "" : "s")}";
            txtReportCount.Text = $"{count} registro{(count == 1 ? "" : "s")}";
            kpiBar.Visibility = Visibility.Visible;
        }

        // ──────────────────────────────────────────────
        //  KPI updates per report type
        // ──────────────────────────────────────────────

        private void SetKpi(string label1, string value1, string label2, string value2, string label3, string value3)
        {
            kpiLabel1.Text = label1; kpiValue1.Text = value1;
            kpiLabel2.Text = label2; kpiValue2.Text = value2;
            kpiLabel3.Text = label3; kpiValue3.Text = value3;
        }

        private void UpdateKpiIngresos(List<ReporteIngresoDiario> data)
        {
            var total = data.Sum(d => d.Total);
            var avg = data.Count > 0 ? total / data.Count : 0;
            var max = data.Count > 0 ? data.Max(d => d.Total) : 0;
            SetKpi("Total ingresos", $"Bs {total:N0}", "Promedio diario", $"Bs {avg:N0}", "Mejor día", $"Bs {max:N0}");
        }

        private void UpdateKpiProductos(List<ReporteProductosVendidos> data)
        {
            var total = data.Sum(d => d.TotalIngresos);
            var units = data.Sum(d => d.TotalVendido);
            SetKpi("Ingresos totales", $"Bs {total:N0}", "Unidades vendidas", $"{units:N0}", "Productos", $"{data.Count}");
        }

        private void UpdateKpiInventario(List<ReporteInventario> data)
        {
            var stock = data.Sum(d => d.StockTotal);
            var avg = data.Count > 0 ? stock / data.Count : 0;
            SetKpi("Stock total", $"{stock:N0}", "Promedio", $"{avg:N0}", "Productos", $"{data.Count}");
        }

        private void UpdateKpiClientes(List<ReporteClientes> data)
        {
            var total = data.Sum(d => d.TotalGastado);
            var avg = data.Count > 0 ? total / data.Count : 0;
            SetKpi("Total gastado", $"Bs {total:N0}", "Promedio", $"Bs {avg:N0}", "Clientes", $"{data.Count}");
        }

        private void UpdateKpiVentas(List<ReporteVentas> data)
        {
            var total = data.Sum(d => d.Monto);
            var avg = data.Count > 0 ? total / data.Count : 0;
            SetKpi("Total ventas", $"Bs {total:N0}", "Promedio", $"Bs {avg:N0}", "Ventas", $"{data.Count}");
        }

        private void UpdateKpiFacturacion(List<ReporteFacturacion> data)
        {
            var total = data.Sum(d => d.Total);
            var avg = data.Count > 0 ? total / data.Count : 0;
            SetKpi("Total facturado", $"Bs {total:N0}", "Promedio", $"Bs {avg:N0}", "Facturas", $"{data.Count}");
        }

        private void UpdateKpiVehiculos(List<ReporteVehiculos> data)
        {
            var vigentes = data.Count(v => v.SoatEstado == "Vigente");
            var vencidos = data.Count(v => v.SoatEstado == "Vencido");
            SetKpi("Total vehículos", $"{data.Count}", "SOAT vigente", $"{vigentes}", "SOAT vencido", $"{vencidos}");
        }

        private void UpdateKpiDepositos(List<ReporteDepositos> data)
        {
            var stock = data.Sum(d => d.StockTotal);
            var avg = data.Count > 0 ? stock / data.Count : 0;
            SetKpi("Stock total", $"{stock:N0}", "Promedio", $"{avg:N0}", "Depósitos", $"{data.Count}");
        }

        private void UpdateKpiEmpleados(List<ReporteEmpleados> data)
        {
            var areas = data.Select(e => e.Area).Where(a => !string.IsNullOrEmpty(a)).Distinct().Count();
            SetKpi("Total empleados", $"{data.Count}", "Áreas", $"{areas}", "Roles activos", "—");
        }

        private void UpdateKpiProduccion(List<ReporteProduccion> data)
        {
            var costo = data.Sum(d => d.CostoTotal ?? 0);
            var completadas = data.Count(d => d.Estado == "Completado");
            var pct = data.Count > 0 ? (completadas * 100 / data.Count) : 0;
            SetKpi("Costo total", $"Bs {costo:N0}", "Completadas", $"{completadas}/{data.Count} ({pct}%)", "Producciones", $"{data.Count}");
        }

        private void UpdateKpiOrdenes(List<ReporteOrdenCompra> data)
        {
            var monto = data.Sum(d => d.Monto);
            var recibidas = data.Count(d => d.Estado == "Recibido");
            SetKpi("Monto total", $"Bs {monto:N0}", "Recibidas", $"{recibidas}/{data.Count}", "Órdenes", $"{data.Count}");
        }

        // ──────────────────────────────────────────────
        //  Column definitions
        // ──────────────────────────────────────────────

        private static List<DataGridColumn> GetIngresosColumns()
        {
            return new List<DataGridColumn>
            {
                TCol("Fecha", "FechaDisplay", 110),
                TCol("Ventas", "VentasCount", 80),
                TCol("Total Bs", "TotalDisplay", 130),
            };
        }

        private static List<DataGridColumn> GetProductosColumns()
        {
            return new List<DataGridColumn>
            {
                TCol("ID", "Id", 60),
                TCol("Producto", "Nombre", 180),
                TCol("Familia", "Familia", 120),
                TCol("Vendido", "TotalVendidoDisplay", 100),
                TCol("Ingresos", "TotalIngresosDisplay", 130),
            };
        }

        private static List<DataGridColumn> GetInventarioColumns()
        {
            return new List<DataGridColumn>
            {
                TCol("ID", "Id", 60),
                TCol("Producto", "Nombre", 180),
                TCol("Familia", "Familia", 120),
                TCol("Stock", "StockDisplay", 100),
                TCol("Depósitos", "DepositosCount", 90),
                TCol("Estado", "Estado", 100),
            };
        }

        private static List<DataGridColumn> GetClientesColumns()
        {
            return new List<DataGridColumn>
            {
                TCol("CI", "Ci", 100),
                TCol("Nombre", "NombreCompleto", 200),
                TCol("Teléfono", "Telefono", 120),
                TCol("Compras", "TotalCompras", 90),
                TCol("Total", "TotalGastadoDisplay", 130),
            };
        }

        private static List<DataGridColumn> GetEmpleadosColumns()
        {
            return new List<DataGridColumn>
            {
                TCol("CI", "Ci", 100),
                TCol("Nombre", "NombreCompleto", 180),
                TCol("Área", "Area", 120),
                TCol("Turno", "Turno", 100),
                TCol("Rol", "RolNombre", 130),
                TCol("Teléfono", "Telefono", 110),
                TCol("Correo", "Correo", 180),
            };
        }

        private static List<DataGridColumn> GetVentasColumns()
        {
            return new List<DataGridColumn>
            {
                TCol("ID", "Id", 60),
                TCol("Fecha", "FechaDisplay", 100),
                TCol("Hora", "HoraDisplay", 70),
                TCol("Cliente", "Cliente", 180),
                TCol("Tipo", "Tipo", 90),
                TCol("Estado", "Estado", 90),
                TCol("Monto", "MontoDisplay", 110),
            };
        }

        private static List<DataGridColumn> GetFacturacionColumns()
        {
            return new List<DataGridColumn>
            {
                TCol("ID", "Id", 60),
                TCol("Fecha", "FechaDisplay", 140),
                TCol("Cliente", "NombreCompleto", 180),
                TCol("NIT", "Nit", 110),
                TCol("Subtotal", "SubtotalDisplay", 100),
                TCol("Dto", "Descuento", 80),
                TCol("Total", "TotalDisplay", 110),
            };
        }

        private static List<DataGridColumn> GetVehiculosColumns()
        {
            return new List<DataGridColumn>
            {
                TCol("Placa", "Placa", 100),
                TCol("Marca", "Marca", 120),
                TCol("Modelo", "Modelo", 120),
                TCol("Tipo", "Tipo", 100),
                TCol("KM", "Kilometraje", 90),
                TCol("SOAT", "SoatDisplay", 110),
                TCol("Estado", "SoatEstado", 100),
                TCol("Repartidor", "Repartidor", 160),
            };
        }

        private static List<DataGridColumn> GetDepositosColumns()
        {
            return new List<DataGridColumn>
            {
                TCol("ID", "Id", 60),
                TCol("Nombre", "Nombre", 160),
                TCol("Dirección", "Direccion", 200),
                TCol("Ubicación", "Ubicacion", 120),
                TCol("Productos", "ProductosCount", 90),
                TCol("Stock", "StockDisplay", 100),
            };
        }

        private static List<DataGridColumn> GetProduccionColumns()
        {
            return new List<DataGridColumn>
            {
                TCol("ID", "Id", 60),
                TCol("Fecha Inicio", "FechaDisplay", 110),
                TCol("Fecha Fin", "FechaFinDisplay", 110),
                TCol("Estado", "Estado", 100),
                TCol("Costo", "CostoDisplay", 110),
                TCol("Insumos", "InsumosCount", 80),
                TCol("Productos", "ProductosCount", 90),
            };
        }

        private static List<DataGridColumn> GetOrdenesColumns()
        {
            return new List<DataGridColumn>
            {
                TCol("ID", "Id", 60),
                TCol("Fecha", "FechaDisplay", 100),
                TCol("Estado", "Estado", 100),
                TCol("Proveedor", "Proveedor", 180),
                TCol("Monto", "MontoDisplay", 110),
                TCol("Items", "ItemsCount", 70),
            };
        }

        private static DataGridTextColumn TCol(string header, string binding, double width)
        {
            return new DataGridTextColumn
            {
                Header = header,
                Binding = new Binding(binding),
                Width = width,
            };
        }

        // ──────────────────────────────────────────────
        //  Event handlers
        // ──────────────────────────────────────────────

        private void DpFilter_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_activeReportButton != null && !_isLoading)
                _ = LoadReport(_activeReportButton.Tag?.ToString() ?? "");
        }

        private void BtnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            dpDesde.SelectedDate = null;
            dpHasta.SelectedDate = null;
            if (_activeReportButton != null && !_isLoading)
                _ = LoadReport(_activeReportButton.Tag?.ToString() ?? "");
        }

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            if (_lastLoadedData == null)
            {
                MessageBox.Show("No hay datos para exportar. Seleccione un reporte primero.",
                    "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv",
                    FileName = $"Reporte_{txtReportTitle.Text.Replace(" ", "_")}_{DateTime.Today:yyyyMMdd}",
                };

                if (dialog.ShowDialog() == true)
                {
                    if (dialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        ExportCsv(dialog.FileName);
                    else
                        ExportExcel(dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportCsv(string path)
        {
            var lines = new List<string>();
            var headers = dgReportes.Columns.Select(c => EscapeCsv(c.Header?.ToString() ?? "")).ToList();
            lines.Add(string.Join(",", headers));

            foreach (var item in dgReportes.Items)
            {
                var values = dgReportes.Columns.Select(col =>
                {
                    if (col is DataGridTextColumn tc && tc.Binding is Binding b)
                        return EscapeCsv(item?.GetType()?.GetProperty(b.Path?.Path ?? "")?.GetValue(item)?.ToString() ?? "");
                    return "";
                }).ToList();
                lines.Add(string.Join(",", values));
            }

            File.WriteAllText(path, string.Join(Environment.NewLine, lines));
            MessageBox.Show($"Exportado a:\n{path}", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportExcel(string path)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Reporte");

            for (int c = 0; c < dgReportes.Columns.Count; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = dgReportes.Columns[c].Header?.ToString() ?? "";
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x2DAAE1);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 2;
            foreach (var item in dgReportes.Items)
            {
                for (int c = 0; c < dgReportes.Columns.Count; c++)
                {
                    if (dgReportes.Columns[c] is DataGridTextColumn tc && tc.Binding is Binding b)
                    {
                        var val = item?.GetType()?.GetProperty(b.Path?.Path ?? "")?.GetValue(item);
                        ws.Cell(row, c + 1).Value = val?.ToString() ?? "";
                    }
                }
                row++;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(path);
            MessageBox.Show($"Exportado a:\n{path}", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
