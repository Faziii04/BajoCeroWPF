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
        /// <summary>
        /// Tracks the currently selected report type button for visual highlighting.
        /// </summary>
        private Button? _activeReportButton;

        /// <summary>
        /// Stores the last loaded data for CSV export.
        /// </summary>
        private IEnumerable? _lastLoadedData;

        /// <summary>
        /// Whether a load is currently in progress (debounce).
        /// </summary>
        private bool _isLoading;

        public ReportesUC()
        {
            InitializeComponent();

            // Set default date range: last 30 days
            dpDesde.SelectedDate = DateTime.Today.AddDays(-30);
            dpHasta.SelectedDate = DateTime.Today;
        }

        // ──────────────────────────────────────────────
        //  Report type selection
        // ──────────────────────────────────────────────

        private async void BtnReporte_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || _isLoading)
                return;

            // Highlight the selected button
            SetActiveReportButton(btn);

            var tag = btn.Tag?.ToString() ?? "";

            // Update header
            var (icon, title) = GetReportMeta(tag);
            txtReportIcon.Text = icon;
            txtReportTitle.Text = title;
            txtReportSubtitle.Text = "Cargando datos...";

            // Load data
            await LoadReport(tag);
        }

        private void SetActiveReportButton(Button button)
        {
            // Reset previous
            if (_activeReportButton != null && _activeReportButton != button)
            {
                _activeReportButton.ClearValue(BackgroundProperty);
                _activeReportButton.ClearValue(BorderBrushProperty);
                _activeReportButton.ClearValue(BorderThicknessProperty);
                _activeReportButton.Foreground = (Brush)FindResource("NavTextColor");
            }

            // Set new active — use GridRowSelectedBrush for contrast in both themes
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
                "Productos"   => ("📦", "Productos más vendidos"),
                "Inventario"  => ("📋", "Inventario actual"),
                "Clientes"    => ("👥", "Clientes frecuentes"),
                "Empleados"   => ("👤", "Empleados por área"),
                "Ventas"      => ("💰", "Ventas por período"),
                "Facturacion" => ("🧾", "Facturación"),
                "Roles"       => ("🔐", "Roles y permisos"),
                "Vehiculos"   => ("🚛", "Vehículos"),
                "Depositos"   => ("🏭", "Depósitos"),
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

            try
            {
                DateTime? desde = dpDesde.SelectedDate;
                DateTime? hasta = dpHasta.SelectedDate;

                switch (tag)
                {
                    case "Productos":
                        var prodData = await ReportesService.GetProductosMasVendidos(desde, hasta);
                        BindDataGrid(prodData, GetProductosColumns());
                        break;

                    case "Inventario":
                        var invData = await ReportesService.GetInventarioActual();
                        BindDataGrid(invData, GetInventarioColumns());
                        break;

                    case "Clientes":
                        var cliData = await ReportesService.GetClientesFrecuentes();
                        BindDataGrid(cliData, GetClientesColumns());
                        break;

                    case "Empleados":
                        var empData = await ReportesService.GetEmpleadosPorArea();
                        BindDataGrid(empData, GetEmpleadosColumns());
                        break;

                    case "Ventas":
                        var venData = await ReportesService.GetVentasPorPeriodo(desde, hasta);
                        BindDataGrid(venData, GetVentasColumns());
                        break;

                    case "Facturacion":
                        var facData = await ReportesService.GetFacturacion(desde, hasta);
                        BindDataGrid(facData, GetFacturacionColumns());
                        break;

                    case "Roles":
                        var rolData = await ReportesService.GetRolesPermisos();
                        BindDataGrid(rolData, GetRolesColumns());
                        break;

                    case "Vehiculos":
                        var vehData = await ReportesService.GetVehiculos();
                        BindDataGrid(vehData, GetVehiculosColumns());
                        break;

                    case "Depositos":
                        var depData = await ReportesService.GetDepositos();
                        BindDataGrid(depData, GetDepositosColumns());
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el reporte: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                emptyState.Visibility = Visibility.Visible;
                kpiBar.Visibility = Visibility.Collapsed;
            }
            finally
            {
                _isLoading = false;
                badgeLoading.Visibility = Visibility.Collapsed;
            }
        }

        private void BindDataGrid(IEnumerable data, List<DataGridColumn> columns)
        {
            _lastLoadedData = data;

            dgReportes.Columns.Clear();
            foreach (var col in columns)
                dgReportes.Columns.Add(col);

            dgReportes.ItemsSource = data;

            // Update count — use non-generic ICollection to handle any List<T>
            var count = (data as ICollection)?.Count ?? 0;
            txtReportSubtitle.Text = $"{count} registro{(count == 1 ? "" : "s")} encontrado{(count == 1 ? "" : "s")}";
            kpiCount.Text = $"{count} registro{(count == 1 ? "" : "s")}";
            kpiBar.Visibility = Visibility.Visible;
        }

        // ──────────────────────────────────────────────
        //  Column definitions per report type
        // ──────────────────────────────────────────────

        private static List<DataGridColumn> GetProductosColumns()
        {
            return new List<DataGridColumn>
            {
                TextCol("ID", "Id", 60),
                TextCol("Producto", "Nombre", 180),
                TextCol("Categoría", "Categoria", 120),
                TextCol("Total vendido", "TotalVendidoDisplay", 120),
                TextCol("Ingresos totales", "TotalIngresosDisplay", 140),
            };
        }

        private static List<DataGridColumn> GetInventarioColumns()
        {
            return new List<DataGridColumn>
            {
                TextCol("ID", "Id", 60),
                TextCol("Producto", "Nombre", 180),
                TextCol("Categoría", "Categoria", 120),
                TextCol("Stock total", "StockDisplay", 110),
                TextCol("Depósitos", "DepositosCount", 100),
                TextCol("Estado", "Estado", 100),
            };
        }

        private static List<DataGridColumn> GetClientesColumns()
        {
            return new List<DataGridColumn>
            {
                TextCol("CI", "Ci", 100),
                TextCol("Nombre completo", "NombreCompleto", 200),
                TextCol("Teléfono", "Telefono", 120),
                TextCol("Compras", "TotalCompras", 90),
                TextCol("Total gastado", "TotalGastadoDisplay", 130),
            };
        }

        private static List<DataGridColumn> GetEmpleadosColumns()
        {
            return new List<DataGridColumn>
            {
                TextCol("CI", "Ci", 100),
                TextCol("Nombre completo", "NombreCompleto", 180),
                TextCol("Área", "Area", 120),
                TextCol("Turno", "Turno", 100),
                TextCol("Rol", "RolNombre", 130),
                TextCol("Teléfono", "Telefono", 110),
                TextCol("Correo", "Correo", 180),
            };
        }

        private static List<DataGridColumn> GetVentasColumns()
        {
            return new List<DataGridColumn>
            {
                TextCol("ID", "Id", 60),
                TextCol("Fecha", "FechaDisplay", 100),
                TextCol("Hora", "HoraDisplay", 70),
                TextCol("Cliente", "Cliente", 180),
                TextCol("Tipo", "Tipo", 90),
                TextCol("Estado", "Estado", 90),
                TextCol("Monto", "MontoDisplay", 110),
            };
        }

        private static List<DataGridColumn> GetFacturacionColumns()
        {
            return new List<DataGridColumn>
            {
                TextCol("ID", "Id", 60),
                TextCol("Fecha", "FechaDisplay", 140),
                TextCol("Cliente", "NombreCompleto", 180),
                TextCol("NIT", "Nit", 110),
                TextCol("Subtotal", "SubtotalDisplay", 100),
                TextCol("Descuento", "Descuento", 90),
                TextCol("Total", "TotalDisplay", 110),
            };
        }

        private static List<DataGridColumn> GetRolesColumns()
        {
            return new List<DataGridColumn>
            {
                TextCol("ID", "Id", 60),
                TextCol("Rol", "Nombre", 160),
                TextCol("Descripción", "Descripcion", 250),
                TextCol("Empleados", "EmpleadosCount", 100),
                TextCol("Permisos", "PermisosCount", 100),
            };
        }

        private static List<DataGridColumn> GetVehiculosColumns()
        {
            return new List<DataGridColumn>
            {
                TextCol("Placa", "Placa", 100),
                TextCol("Marca", "Marca", 120),
                TextCol("Modelo", "Modelo", 120),
                TextCol("Tipo", "Tipo", 100),
                TextCol("Kilometraje", "Kilometraje", 100),
                TextCol("SOAT vence", "SoatDisplay", 110),
                TextCol("Estado SOAT", "SoatEstado", 100),
                TextCol("Repartidor", "Repartidor", 160),
            };
        }

        private static List<DataGridColumn> GetDepositosColumns()
        {
            return new List<DataGridColumn>
            {
                TextCol("ID", "Id", 60),
                TextCol("Nombre", "Nombre", 160),
                TextCol("Dirección", "Direccion", 200),
                TextCol("Ubicación", "Ubicacion", 120),
                TextCol("Productos", "ProductosCount", 100),
                TextCol("Stock total", "StockDisplay", 110),
            };
        }

        // ──────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────

        private static DataGridTextColumn TextCol(string header, string binding, double width)
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

        private void DgReportes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: handle row selection for detail view
        }

        private void DpDesde_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // Auto-reload if a report is already selected
            if (_activeReportButton != null && !_isLoading)
            {
                _ = LoadReport(_activeReportButton.Tag?.ToString() ?? "");
            }
        }

        private void DpHasta_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_activeReportButton != null && !_isLoading)
            {
                _ = LoadReport(_activeReportButton.Tag?.ToString() ?? "");
            }
        }

        private void BtnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            dpDesde.SelectedDate = null;
            dpHasta.SelectedDate = null;

            if (_activeReportButton != null && !_isLoading)
            {
                _ = LoadReport(_activeReportButton.Tag?.ToString() ?? "");
            }
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
                    Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    FileName = $"Reporte_{txtReportTitle.Text.Replace(" ", "_")}_{DateTime.Today:yyyyMMdd}",
                };

                if (dialog.ShowDialog() == true)
                {
                    var path = dialog.FileName;
                    if (path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        ExportCsv(path);
                    else
                        ExportExcel(path);
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

            // Header
            var headers = dgReportes.Columns
                .Select(c => EscapeCsv(c.Header?.ToString() ?? ""))
                .ToList();
            lines.Add(string.Join(",", headers));

            // Data rows
            foreach (var item in dgReportes.Items)
            {
                var values = dgReportes.Columns
                    .Select(col =>
                    {
                        if (col is DataGridTextColumn textCol && textCol.Binding is Binding binding)
                        {
                            var value = item?.GetType()?.GetProperty(binding.Path?.Path ?? "")?.GetValue(item);
                            return EscapeCsv(value?.ToString() ?? "");
                        }
                        return "";
                    })
                    .ToList();
                lines.Add(string.Join(",", values));
            }

            File.WriteAllText(path, string.Join(Environment.NewLine, lines));

            MessageBox.Show($"Reporte exportado exitosamente a:\n{path}",
                "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportExcel(string path)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Reporte");

            // Header row with styling
            for (int c = 0; c < dgReportes.Columns.Count; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = dgReportes.Columns[c].Header?.ToString() ?? "";
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x2DAAE1);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.BottomBorderColor = XLColor.FromArgb(0x1A4A7A);
            }

            // Data rows
            int row = 2;
            foreach (var item in dgReportes.Items)
            {
                for (int c = 0; c < dgReportes.Columns.Count; c++)
                {
                    if (dgReportes.Columns[c] is DataGridTextColumn textCol && textCol.Binding is Binding binding)
                    {
                        var value = item?.GetType()?.GetProperty(binding.Path?.Path ?? "")?.GetValue(item);
                        var cell = ws.Cell(row, c + 1);
                        cell.Value = value?.ToString() ?? "";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    }
                }
                row++;
            }

            // Auto-fit columns
            ws.Columns().AdjustToContents();

            // Save
            workbook.SaveAs(path);

            MessageBox.Show($"Reporte exportado exitosamente a:\n{path}",
                "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
