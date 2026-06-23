using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace ProyectoIntegradorNet10.Services
{
    public static class ReportesService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        // ──────────────────────────────────────────────
        //  Reporte: Ingresos diarios (bar chart)
        // ──────────────────────────────────────────────
        public static async Task<List<ReporteIngresoDiario>> GetIngresos(DateTime? desde = null, DateTime? hasta = null)
        {
            var list = new List<ReporteIngresoDiario>();
            using var conn = await DS.OpenConnectionAsync();

            var sql = @"
                SELECT v.fecha,
                       COALESCE(SUM(vd.cantidad * vd.precio_unitario), 0) AS total,
                       COUNT(DISTINCT v.id) AS ventas_count
                FROM venta v
                JOIN venta_detalles vd ON vd.venta_id = v.id
                WHERE (v.fecha >= @desde OR @desde IS NULL)
                  AND (v.fecha <= @hasta OR @hasta IS NULL)
                GROUP BY v.fecha
                ORDER BY v.fecha";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@desde", (object?)desde ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@hasta", (object?)hasta ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ReporteIngresoDiario
                {
                    Fecha = reader.GetDateTime(0),
                    Total = reader.GetDecimal(1),
                    VentasCount = reader.GetInt32(2),
                });
            }
            return list;
        }

        // ──────────────────────────────────────────────
        //  Reporte: Productos más vendidos (FIXED)
        // ──────────────────────────────────────────────
        public static async Task<List<ReporteProductosVendidos>> GetProductosMasVendidos(DateTime? desde = null, DateTime? hasta = null)
        {
            var list = new List<ReporteProductosVendidos>();
            using var conn = await DS.OpenConnectionAsync();

            var sql = @"
                SELECT p.id, p.nombre,
                       COALESCE(pf.nombre, 'Sin familia') AS familia,
                       COALESCE(SUM(vd.cantidad), 0) AS total_vendido,
                       COALESCE(SUM(vd.cantidad * vd.precio_unitario), 0) AS total_ingresos
                FROM producto p
                LEFT JOIN miembros m ON m.producto_id = p.id
                LEFT JOIN producto_familia pf ON pf.id = m.familia_id
                LEFT JOIN venta_detalles vd ON vd.producto_id = p.id
                LEFT JOIN venta v ON v.id = vd.venta_id
                    AND (v.fecha >= @desde OR @desde IS NULL)
                    AND (v.fecha <= @hasta OR @hasta IS NULL)
                GROUP BY p.id, p.nombre, pf.nombre
                ORDER BY total_vendido DESC
                LIMIT 50";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@desde", (object?)desde ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@hasta", (object?)hasta ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ReporteProductosVendidos
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Familia = reader.IsDBNull(2) ? "Sin familia" : reader.GetString(2),
                    TotalVendido = reader.GetDecimal(3),
                    TotalIngresos = reader.GetDecimal(4),
                });
            }
            return list;
        }

        // ──────────────────────────────────────────────
        //  Reporte: Inventario actual (FIXED)
        // ──────────────────────────────────────────────
        public static async Task<List<ReporteInventario>> GetInventarioActual()
        {
            var list = new List<ReporteInventario>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT p.id, p.nombre, p.estado,
                       COALESCE(pf.nombre, 'Sin familia') AS familia,
                       COALESCE(SUM(pd.cantidad), 0) AS stock_total,
                       COUNT(DISTINCT pd.deposito_id) AS depositos_count
                FROM producto p
                LEFT JOIN miembros m ON m.producto_id = p.id
                LEFT JOIN producto_familia pf ON pf.id = m.familia_id
                LEFT JOIN producto_deposito pd ON pd.producto_id = p.id
                GROUP BY p.id, p.nombre, p.estado, pf.nombre
                ORDER BY p.nombre", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ReporteInventario
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Estado = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Familia = reader.IsDBNull(3) ? "Sin familia" : reader.GetString(3),
                    StockTotal = reader.GetDecimal(4),
                    DepositosCount = reader.GetInt32(5),
                });
            }
            return list;
        }

        // ──────────────────────────────────────────────
        //  Reporte: Clientes frecuentes
        // ──────────────────────────────────────────────
        public static async Task<List<ReporteClientes>> GetClientesFrecuentes()
        {
            var list = new List<ReporteClientes>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT c.ci, c.nombre, c.apellido, c.telefono,
                       COUNT(v.id) AS total_compras,
                       COALESCE(SUM(vd.cantidad * vd.precio_unitario), 0) AS total_gastado
                FROM cliente c
                LEFT JOIN venta v ON v.cliente_ci = c.ci
                LEFT JOIN venta_detalles vd ON vd.venta_id = v.id
                GROUP BY c.ci, c.nombre, c.apellido, c.telefono
                ORDER BY total_compras DESC
                LIMIT 50", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ReporteClientes
                {
                    Ci = reader.GetString(0),
                    Nombre = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Apellido = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Telefono = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    TotalCompras = reader.GetInt32(4),
                    TotalGastado = reader.GetDecimal(5),
                });
            }
            return list;
        }

        // ──────────────────────────────────────────────
        //  Reporte: Empleados por área
        // ──────────────────────────────────────────────
        public static async Task<List<ReporteEmpleados>> GetEmpleadosPorArea()
        {
            var list = new List<ReporteEmpleados>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT e.ci, e.nombre, e.apellido, e.area, e.turno, e.telefono, e.correo,
                       COALESCE(r.nombre, 'Sin rol') AS rol_nombre
                FROM empleado e
                LEFT JOIN empleado_rol er ON er.empleado_ci = e.ci AND er.estado = 'Activo'
                LEFT JOIN rol r ON r.id = er.rol_id
                ORDER BY e.area, e.nombre", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ReporteEmpleados
                {
                    Ci = reader.GetString(0),
                    Nombre = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Apellido = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Area = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Turno = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Telefono = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Correo = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    RolNombre = reader.IsDBNull(7) ? "Sin rol" : reader.GetString(7),
                });
            }
            return list;
        }

        // ──────────────────────────────────────────────
        //  Reporte: Ventas por período
        // ──────────────────────────────────────────────
        public static async Task<List<ReporteVentas>> GetVentasPorPeriodo(DateTime? desde = null, DateTime? hasta = null)
        {
            var list = new List<ReporteVentas>();
            using var conn = await DS.OpenConnectionAsync();

            var sql = @"
                SELECT v.id, v.fecha, v.hora, v.tipo, v.estado,
                       COALESCE(v.porcentaje_descuento, 0) AS descuento,
                       COALESCE(c.nombre || ' ' || c.apellido, 'Sin cliente') AS cliente,
                       COALESCE(SUM(vd.cantidad * vd.precio_unitario), 0) AS monto
                FROM venta v
                LEFT JOIN cliente c ON c.ci = v.cliente_ci
                LEFT JOIN venta_detalles vd ON vd.venta_id = v.id
                WHERE (v.fecha >= @desde OR @desde IS NULL)
                  AND (v.fecha <= @hasta OR @hasta IS NULL)
                GROUP BY v.id, v.fecha, v.hora, v.tipo, v.estado, v.porcentaje_descuento, c.nombre, c.apellido
                ORDER BY v.fecha DESC, v.hora DESC
                LIMIT 100";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@desde", (object?)desde ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@hasta", (object?)hasta ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ReporteVentas
                {
                    Id = reader.GetInt32(0),
                    Fecha = reader.GetDateTime(1),
                    Hora = reader.GetTimeSpan(2),
                    Tipo = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Estado = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Descuento = reader.GetDecimal(5),
                    Cliente = reader.IsDBNull(6) ? "Sin cliente" : reader.GetString(6),
                    Monto = reader.GetDecimal(7),
                });
            }
            return list;
        }

        // ──────────────────────────────────────────────
        //  Reporte: Facturación
        // ──────────────────────────────────────────────
        public static async Task<List<ReporteFacturacion>> GetFacturacion(DateTime? desde = null, DateTime? hasta = null)
        {
            var list = new List<ReporteFacturacion>();
            using var conn = await DS.OpenConnectionAsync();

            var sql = @"
                SELECT f.id, f.fecha_emision, f.nombre_completo, f.nit,
                       f.subtotal, f.descuento, f.total, f.descuento_tipo,
                       f.venta_id
                FROM factura f
                WHERE (f.fecha_emision >= @desde OR @desde IS NULL)
                  AND (f.fecha_emision <= @hasta OR @hasta IS NULL)
                ORDER BY f.fecha_emision DESC
                LIMIT 100";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@desde", (object?)desde ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@hasta", (object?)hasta ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ReporteFacturacion
                {
                    Id = reader.GetInt32(0),
                    FechaEmision = reader.GetDateTime(1),
                    NombreCompleto = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Nit = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Subtotal = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                    Descuento = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                    Total = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                    DescuentoTipo = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    VentaId = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                });
            }
            return list;
        }

        // ──────────────────────────────────────────────
        //  Reporte: Vehículos
        // ──────────────────────────────────────────────
        public static async Task<List<ReporteVehiculos>> GetVehiculos()
        {
            var list = new List<ReporteVehiculos>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT v.placa, v.modelo, v.marca, v.tipo, v.kilometraje, v.soat_vencimiento,
                       COALESCE(r.nombre || ' ' || r.apellido, 'Sin repartidor') AS repartidor
                FROM vehiculo v
                LEFT JOIN repartidor_vehiculo rv ON rv.vehiculo_placa = v.placa AND rv.estado = 'Activo'
                LEFT JOIN repartidor rep ON rep.id = rv.repartidor_id
                LEFT JOIN empleado r ON r.ci = rep.empleado_ci
                ORDER BY v.placa", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ReporteVehiculos
                {
                    Placa = reader.GetString(0),
                    Modelo = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Marca = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Tipo = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Kilometraje = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                    SoatVencimiento = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    Repartidor = reader.IsDBNull(6) ? "Sin repartidor" : reader.GetString(6),
                });
            }
            return list;
        }

        // ──────────────────────────────────────────────
        //  Reporte: Depósitos con stock
        // ──────────────────────────────────────────────
        public static async Task<List<ReporteDepositos>> GetDepositos()
        {
            var list = new List<ReporteDepositos>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT d.id, d.nombre, d.direccion, d.ubicacion,
                       COUNT(DISTINCT pd.producto_id) AS productos_count,
                       COALESCE(SUM(pd.cantidad), 0) AS stock_total
                FROM deposito d
                LEFT JOIN producto_deposito pd ON pd.deposito_id = d.id
                GROUP BY d.id, d.nombre, d.direccion, d.ubicacion
                ORDER BY d.nombre", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ReporteDepositos
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Direccion = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Ubicacion = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    ProductosCount = reader.GetInt32(4),
                    StockTotal = reader.GetDecimal(5),
                });
            }
            return list;
        }

        // ──────────────────────────────────────────────
        //  Reporte: Producción (NEW)
        // ──────────────────────────────────────────────
        public static async Task<List<ReporteProduccion>> GetProduccion(DateTime? desde = null, DateTime? hasta = null)
        {
            var list = new List<ReporteProduccion>();
            using var conn = await DS.OpenConnectionAsync();

            var sql = @"
                SELECT p.id, p.fecha_inicio, p.fecha_fin, p.estado, p.costo_total,
                       COUNT(DISTINCT ip.insumo_id) AS insumos_count,
                       COUNT(DISTINCT pp.producto_id) AS productos_count
                FROM produccion p
                LEFT JOIN insumo_produccion ip ON ip.produccion_id = p.id
                LEFT JOIN produccion_producto pp ON pp.produccion_id = p.id
                WHERE (p.fecha_inicio::date >= @desde OR @desde IS NULL)
                  AND (p.fecha_inicio::date <= @hasta OR @hasta IS NULL)
                GROUP BY p.id, p.fecha_inicio, p.fecha_fin, p.estado, p.costo_total
                ORDER BY p.fecha_inicio DESC
                LIMIT 100";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@desde", (object?)desde ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@hasta", (object?)hasta ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ReporteProduccion
                {
                    Id = reader.GetInt32(0),
                    FechaInicio = reader.GetDateTime(1),
                    FechaFin = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                    Estado = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    CostoTotal = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                    InsumosCount = reader.GetInt32(5),
                    ProductosCount = reader.GetInt32(6),
                });
            }
            return list;
        }

        // ──────────────────────────────────────────────
        //  Reporte: Órdenes de Compra (NEW)
        // ──────────────────────────────────────────────
        public static async Task<List<ReporteOrdenCompra>> GetOrdenesCompra(DateTime? desde = null, DateTime? hasta = null)
        {
            var list = new List<ReporteOrdenCompra>();
            using var conn = await DS.OpenConnectionAsync();

            var sql = @"
                SELECT oc.id, oc.fecha_pedido, oc.estado, oc.monto,
                       pr.nombre AS proveedor,
                       COUNT(DISTINCT do.insumo_id) AS items_count
                FROM orden_compra oc
                LEFT JOIN proveedor pr ON pr.id = oc.proveedor
                LEFT JOIN detalles_orden do ON do.orden_id = oc.id
                WHERE (oc.fecha_pedido >= @desde OR @desde IS NULL)
                  AND (oc.fecha_pedido <= @hasta OR @hasta IS NULL)
                GROUP BY oc.id, oc.fecha_pedido, oc.estado, oc.monto, pr.nombre
                ORDER BY oc.fecha_pedido DESC
                LIMIT 100";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@desde", (object?)desde ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@hasta", (object?)hasta ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ReporteOrdenCompra
                {
                    Id = reader.GetInt32(0),
                    FechaPedido = reader.GetDateTime(1),
                    Estado = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Monto = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                    Proveedor = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    ItemsCount = reader.GetInt32(5),
                });
            }
            return list;
        }
    }

    // ──────────────────────────────────────────────
    //  DTOs for each report type
    // ──────────────────────────────────────────────

    public class ReporteIngresoDiario
    {
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public int VentasCount { get; set; }
        public string FechaDisplay => Fecha.ToString("dd/MM/yyyy");
        public string TotalDisplay => Total.ToString("N2");
    }

    public class ReporteProductosVendidos
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Familia { get; set; } = "";
        public decimal TotalVendido { get; set; }
        public decimal TotalIngresos { get; set; }
        public string TotalVendidoDisplay => TotalVendido.ToString("N0");
        public string TotalIngresosDisplay => TotalIngresos.ToString("N2");
    }

    public class ReporteInventario
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Estado { get; set; } = "";
        public string Familia { get; set; } = "";
        public decimal StockTotal { get; set; }
        public int DepositosCount { get; set; }
        public string StockDisplay => StockTotal.ToString("N0");
    }

    public class ReporteClientes
    {
        public string Ci { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string Telefono { get; set; } = "";
        public int TotalCompras { get; set; }
        public decimal TotalGastado { get; set; }
        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
        public string TotalGastadoDisplay => TotalGastado.ToString("N2");
    }

    public class ReporteEmpleados
    {
        public string Ci { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string Area { get; set; } = "";
        public string Turno { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Correo { get; set; } = "";
        public string RolNombre { get; set; } = "";
        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
    }

    public class ReporteVentas
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
        public string Tipo { get; set; } = "";
        public string Estado { get; set; } = "";
        public decimal Descuento { get; set; }
        public string Cliente { get; set; } = "";
        public decimal Monto { get; set; }
        public string FechaDisplay => Fecha.ToString("dd/MM/yyyy");
        public string HoraDisplay => Hora.ToString(@"hh\:mm");
        public string MontoDisplay => Monto.ToString("N2");
    }

    public class ReporteFacturacion
    {
        public int Id { get; set; }
        public DateTime FechaEmision { get; set; }
        public string NombreCompleto { get; set; } = "";
        public string Nit { get; set; } = "";
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
        public string DescuentoTipo { get; set; } = "";
        public int VentaId { get; set; }
        public string FechaDisplay => FechaEmision.ToString("dd/MM/yyyy HH:mm");
        public string TotalDisplay => Total.ToString("N2");
        public string SubtotalDisplay => Subtotal.ToString("N2");
    }

    public class ReporteVehiculos
    {
        public string Placa { get; set; } = "";
        public string Modelo { get; set; } = "";
        public string Marca { get; set; } = "";
        public string Tipo { get; set; } = "";
        public decimal Kilometraje { get; set; }
        public DateTime? SoatVencimiento { get; set; }
        public string Repartidor { get; set; } = "";
        public string SoatDisplay => SoatVencimiento?.ToString("dd/MM/yyyy") ?? "Sin registro";
        public string SoatEstado
        {
            get
            {
                if (SoatVencimiento == null) return "Sin registro";
                var days = (SoatVencimiento.Value - DateTime.Today).Days;
                if (days < 0) return "Vencido";
                if (days <= 30) return "Por vencer";
                return "Vigente";
            }
        }
    }

    public class ReporteDepositos
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Direccion { get; set; } = "";
        public string Ubicacion { get; set; } = "";
        public int ProductosCount { get; set; }
        public decimal StockTotal { get; set; }
        public string StockDisplay => StockTotal.ToString("N0");
    }

    public class ReporteProduccion
    {
        public int Id { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Estado { get; set; } = "";
        public decimal? CostoTotal { get; set; }
        public int InsumosCount { get; set; }
        public int ProductosCount { get; set; }
        public string FechaDisplay => FechaInicio.ToString("dd/MM/yyyy");
        public string FechaFinDisplay => FechaFin?.ToString("dd/MM/yyyy") ?? "—";
        public string CostoDisplay => CostoTotal?.ToString("N2") ?? "—";
    }

    public class ReporteOrdenCompra
    {
        public int Id { get; set; }
        public DateTime FechaPedido { get; set; }
        public string Estado { get; set; } = "";
        public decimal Monto { get; set; }
        public string Proveedor { get; set; } = "";
        public int ItemsCount { get; set; }
        public string FechaDisplay => FechaPedido.ToString("dd/MM/yyyy");
        public string MontoDisplay => Monto.ToString("N2");
    }
}
