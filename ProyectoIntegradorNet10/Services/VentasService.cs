using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class VentasService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        // ──────────── VENTAS ────────────

        public static async Task<List<VentaModel>> GetAllVentas()
        {
            var list = new List<VentaModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT v.id, v.fecha, v.hora, v.tipo, v.estado, v.porcentaje_descuento, " +
                "v.repartidor_id, v.cliente_ci, " +
                "COALESCE((SELECT SUM(vd.cantidad * vd.precio_unitario) FROM venta_detalles vd WHERE vd.venta_id = v.id), 0) AS monto, " +
                "COALESCE(c.nombre || ' ' || c.apellido, '') AS cliente_nombre, " +
                "v.pagado, v.entregado, v.nit, v.delivery, " +
                "v.fecha_entrega, v.hora_entrega, v.fecha_entregado, v.hora_entregado " +
                "FROM venta v " +
                "LEFT JOIN cliente c ON c.ci = v.cliente_ci " +
                "ORDER BY v.id DESC", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapVenta(reader));
            }
            return list;
        }

        /// <summary>
        /// Returns ventas filtered by delivery estado.
        /// Only returns ventas with Delivery = true.
        /// </summary>
        public static async Task<List<VentaModel>> GetVentasByEstado(string estado)
        {
            var list = new List<VentaModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT v.id, v.fecha, v.hora, v.tipo, v.estado, v.porcentaje_descuento, " +
                "v.repartidor_id, v.cliente_ci, " +
                "COALESCE((SELECT SUM(vd.cantidad * vd.precio_unitario) FROM venta_detalles vd WHERE vd.venta_id = v.id), 0) AS monto, " +
                "COALESCE(c.nombre || ' ' || c.apellido, '') AS cliente_nombre, " +
                "v.pagado, v.entregado, v.nit, v.delivery, " +
                "v.fecha_entrega, v.hora_entrega, v.fecha_entregado, v.hora_entregado, " +
                "COALESCE(e.nombre || ' ' || e.apellido, '') AS repartidor_nombre " +
                "FROM venta v " +
                "LEFT JOIN cliente c ON c.ci = v.cliente_ci " +
                "LEFT JOIN repartidor r ON r.id = v.repartidor_id " +
                "LEFT JOIN empleado e ON e.ci = r.empleado_ci " +
                "WHERE v.delivery = true AND v.estado = @estado " +
                "ORDER BY v.id DESC", conn);
            cmd.Parameters.AddWithValue("@estado", estado);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapVenta(reader));
            }
            return list;
        }

        public static async Task<VentaModel?> GetVentaById(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT v.id, v.fecha, v.hora, v.tipo, v.estado, v.porcentaje_descuento, " +
                "v.repartidor_id, v.cliente_ci, " +
                "COALESCE((SELECT SUM(vd.cantidad * vd.precio_unitario) FROM venta_detalles vd WHERE vd.venta_id = v.id), 0) AS monto, " +
                "COALESCE(c.nombre || ' ' || c.apellido, '') AS cliente_nombre, " +
                "v.pagado, v.entregado, v.nit, v.delivery, " +
                "v.fecha_entrega, v.hora_entrega, v.fecha_entregado, v.hora_entregado " +
                "FROM venta v " +
                "LEFT JOIN cliente c ON c.ci = v.cliente_ci " +
                "WHERE v.id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return MapVenta(reader);
            return null;
        }

        public static async Task<int> InsertVenta(VentaModel v)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO venta (fecha, hora, tipo, estado, porcentaje_descuento, repartidor_id, cliente_ci, pagado, entregado, nit, delivery, fecha_entrega, hora_entrega, fecha_entregado, hora_entregado) " +
                "VALUES (@fecha, @hora, @tipo, @estado, @descuento, @repartidor, @cliente, @pagado, @entregado, @nit, @delivery, @fechaEntrega, @horaEntrega, @fechaEntregado, @horaEntregado) RETURNING id", conn);
            cmd.Parameters.AddWithValue("@fecha", v.Fecha);
            cmd.Parameters.AddWithValue("@hora", v.Hora);
            cmd.Parameters.AddWithValue("@tipo", (object?)v.Tipo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@estado", (object?)v.Estado ?? "Pedido");
            cmd.Parameters.AddWithValue("@descuento", (object?)v.PorcentajeDescuento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@repartidor", (object?)v.RepartidorId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cliente", (object?)v.ClienteCi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pagado", v.Pagado);
            cmd.Parameters.AddWithValue("@entregado", v.Entregado);
            cmd.Parameters.AddWithValue("@nit", (object?)v.Nit ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@delivery", v.Delivery);
            cmd.Parameters.AddWithValue("@fechaEntrega", (object?)v.FechaEntrega ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@horaEntrega", (object?)v.HoraEntrega ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fechaEntregado", (object?)v.FechaEntregado ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@horaEntregado", (object?)v.HoraEntregado ?? DBNull.Value);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public static async Task UpdateVenta(VentaModel v)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE venta SET fecha = @fecha, hora = @hora, tipo = @tipo, estado = @estado, " +
                "porcentaje_descuento = @descuento, repartidor_id = @repartidor, cliente_ci = @cliente, " +
                "pagado = @pagado, entregado = @entregado, nit = @nit, delivery = @delivery, " +
                "fecha_entrega = @fechaEntrega, hora_entrega = @horaEntrega, " +
                "fecha_entregado = @fechaEntregado, hora_entregado = @horaEntregado " +
                "WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", v.Id);
            cmd.Parameters.AddWithValue("@fecha", v.Fecha);
            cmd.Parameters.AddWithValue("@hora", v.Hora);
            cmd.Parameters.AddWithValue("@tipo", (object?)v.Tipo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@estado", (object?)v.Estado ?? "Pedido");
            cmd.Parameters.AddWithValue("@descuento", (object?)v.PorcentajeDescuento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@repartidor", (object?)v.RepartidorId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cliente", (object?)v.ClienteCi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pagado", v.Pagado);
            cmd.Parameters.AddWithValue("@entregado", v.Entregado);
            cmd.Parameters.AddWithValue("@nit", (object?)v.Nit ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@delivery", v.Delivery);
            cmd.Parameters.AddWithValue("@fechaEntrega", (object?)v.FechaEntrega ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@horaEntrega", (object?)v.HoraEntrega ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fechaEntregado", (object?)v.FechaEntregado ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@horaEntregado", (object?)v.HoraEntregado ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task DeleteVenta(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd1 = new NpgsqlCommand(
                "DELETE FROM pago_venta WHERE venta_id = @id", conn);
            cmd1.Parameters.AddWithValue("@id", id);
            await cmd1.ExecuteNonQueryAsync();

            using var cmd2 = new NpgsqlCommand(
                "DELETE FROM venta_detalles WHERE venta_id = @id", conn);
            cmd2.Parameters.AddWithValue("@id", id);
            await cmd2.ExecuteNonQueryAsync();

            using var cmd3 = new NpgsqlCommand(
                "DELETE FROM venta WHERE id = @id", conn);
            cmd3.Parameters.AddWithValue("@id", id);
            await cmd3.ExecuteNonQueryAsync();
        }

        public static async Task<List<VentaModel>> SearchVentas(string term)
        {
            var list = new List<VentaModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT v.id, v.fecha, v.hora, v.tipo, v.estado, v.porcentaje_descuento, " +
                "v.repartidor_id, v.cliente_ci, " +
                "COALESCE((SELECT SUM(vd.cantidad * vd.precio_unitario) FROM venta_detalles vd WHERE vd.venta_id = v.id), 0) AS monto, " +
                "COALESCE(c.nombre || ' ' || c.apellido, '') AS cliente_nombre, " +
                "v.pagado, v.entregado, v.nit, v.delivery, " +
                "v.fecha_entrega, v.hora_entrega, v.fecha_entregado, v.hora_entregado " +
                "FROM venta v " +
                "LEFT JOIN cliente c ON c.ci = v.cliente_ci " +
                "WHERE CAST(v.id AS TEXT) LIKE @term " +
                "   OR LOWER(c.nombre) LIKE @term " +
                "   OR LOWER(c.apellido) LIKE @term " +
                "ORDER BY v.id DESC", conn);
            cmd.Parameters.AddWithValue("@term", $"%{term.ToLower()}%");
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapVenta(reader));
            }
            return list;
        }

        // ──────────── VENTA DETALLES ────────────

        public static async Task<List<VentaDetalleModel>> GetDetallesByVenta(int ventaId)
        {
            var list = new List<VentaDetalleModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT vd.producto_id, vd.venta_id, vd.cantidad, vd.precio_unitario, " +
                "COALESCE(p.nombre, '') AS producto_nombre " +
                "FROM venta_detalles vd " +
                "LEFT JOIN producto p ON p.id = vd.producto_id " +
                "WHERE vd.venta_id = @ventaId", conn);
            cmd.Parameters.AddWithValue("@ventaId", ventaId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapDetalle(reader));
            }
            return list;
        }

        public static async Task InsertDetalle(VentaDetalleModel d)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO venta_detalles (producto_id, venta_id, cantidad, precio_unitario) " +
                "VALUES (@productoId, @ventaId, @cantidad, @precio)", conn);
            cmd.Parameters.AddWithValue("@productoId", d.ProductoId);
            cmd.Parameters.AddWithValue("@ventaId", d.VentaId);
            cmd.Parameters.AddWithValue("@cantidad", d.Cantidad ?? 1);
            cmd.Parameters.AddWithValue("@precio", d.PrecioUnitario ?? 0);
            await cmd.ExecuteNonQueryAsync();
        }

        // ──────────── PAGOS ────────────

        public static async Task<List<PagoModel>> GetPagosByVenta(int ventaId)
        {
            var list = new List<PagoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT p.pago_id, p.fecha, p.hora, p.monto, p.metodo, p.estado, pv.venta_id " +
                "FROM pago p " +
                "JOIN pago_venta pv ON pv.pago_id = p.pago_id " +
                "WHERE pv.venta_id = @ventaId " +
                "ORDER BY p.fecha", conn);
            cmd.Parameters.AddWithValue("@ventaId", ventaId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapPago(reader));
            }
            return list;
        }

        public static async Task<int> InsertPago(PagoModel p)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO pago (fecha, hora, monto, metodo, estado) " +
                "VALUES (@fecha, @hora, @monto, @metodo, @estado) RETURNING pago_id", conn);
            cmd.Parameters.AddWithValue("@fecha", p.Fecha);
            cmd.Parameters.AddWithValue("@hora", p.Hora);
            cmd.Parameters.AddWithValue("@monto", p.Monto);
            cmd.Parameters.AddWithValue("@metodo", (object?)p.Metodo ?? "Efectivo");
            cmd.Parameters.AddWithValue("@estado", (object?)p.Estado ?? "Pendiente");
            var result = await cmd.ExecuteScalarAsync();
            int pagoId = Convert.ToInt32(result);
            using var cmd2 = new NpgsqlCommand(
                "INSERT INTO pago_venta (pago_id, venta_id) VALUES (@pagoId, @ventaId)", conn);
            cmd2.Parameters.AddWithValue("@pagoId", pagoId);
            cmd2.Parameters.AddWithValue("@ventaId", p.VentaId);
            await cmd2.ExecuteNonQueryAsync();
            return pagoId;
        }

        public static async Task UpdatePago(PagoModel p)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE pago SET fecha = @fecha, hora = @hora, monto = @monto, " +
                "metodo = @metodo, estado = @estado WHERE pago_id = @pagoId", conn);
            cmd.Parameters.AddWithValue("@pagoId", p.PagoId);
            cmd.Parameters.AddWithValue("@fecha", p.Fecha);
            cmd.Parameters.AddWithValue("@hora", p.Hora);
            cmd.Parameters.AddWithValue("@monto", p.Monto);
            cmd.Parameters.AddWithValue("@metodo", (object?)p.Metodo ?? "Efectivo");
            cmd.Parameters.AddWithValue("@estado", (object?)p.Estado ?? "Pendiente");
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task DeletePago(int pagoId)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd1 = new NpgsqlCommand(
                "DELETE FROM pago_venta WHERE pago_id = @pagoId", conn);
            cmd1.Parameters.AddWithValue("@pagoId", pagoId);
            await cmd1.ExecuteNonQueryAsync();
            using var cmd2 = new NpgsqlCommand(
                "DELETE FROM pago WHERE pago_id = @pagoId", conn);
            cmd2.Parameters.AddWithValue("@pagoId", pagoId);
            await cmd2.ExecuteNonQueryAsync();
        }

        // ──────────── HELPERS ────────────

        public static async Task<List<ProductoModel>> GetAllProductos()
        {
            var list = new List<ProductoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                @"SELECT p.id, p.nombre, p.precio_venta, p.estado, p.url,
                         COALESCE(SUM(pd.cantidad), 0) AS stock_total
                  FROM producto p
                  LEFT JOIN producto_deposito pd ON pd.producto_id = p.id
                  GROUP BY p.id, p.nombre, p.precio_venta, p.estado, p.url
                  ORDER BY p.nombre", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ProductoModel
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    PrecioVenta = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                    Estado = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Url = reader.IsDBNull(4) ? null : reader.GetString(4),
                    StockTotal = reader.GetDecimal(5),
                });
            }
            return list;
        }

        public static async Task<List<ClienteModel>> GetAllClientes()
        {
            var list = new List<ClienteModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT ci, nombre, apellido, direccion, nit, telefono FROM cliente ORDER BY nombre", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ClienteModel
                {
                    Ci = reader.GetString(0),
                    Nombre = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Apellido = reader.IsDBNull(2) ? null : reader.GetString(2),
                });
            }
            return list;
        }

        private static VentaModel MapVenta(NpgsqlDataReader r)
        {
            var model = new VentaModel
            {
                Id = r.GetInt32(0),
                Fecha = r.GetDateTime(1),
                Hora = r.GetTimeSpan(2),
                Tipo = r.IsDBNull(3) ? null : r.GetString(3),
                Estado = r.IsDBNull(4) ? null : r.GetString(4),
                PorcentajeDescuento = r.IsDBNull(5) ? null : r.GetDecimal(5),
                RepartidorId = r.IsDBNull(6) ? null : r.GetInt32(6),
                ClienteCi = r.IsDBNull(7) ? null : r.GetString(7),
                MontoFromDb = r.IsDBNull(8) ? null : r.GetDecimal(8),
                ClienteNombre = r.IsDBNull(9) ? null : r.GetString(9),
                Pagado = r.IsDBNull(10) ? false : r.GetBoolean(10),
                Entregado = r.IsDBNull(11) ? false : r.GetBoolean(11),
                Nit = r.IsDBNull(12) ? null : r.GetString(12),
                Delivery = r.IsDBNull(13) ? false : r.GetBoolean(13),
            };
            // Map extended fields
            if (r.FieldCount > 14)
            {
                if (!r.IsDBNull(14)) model.FechaEntrega = r.GetDateTime(14);
                if (!r.IsDBNull(15)) model.HoraEntrega = r.GetTimeSpan(15);
                if (!r.IsDBNull(16)) model.FechaEntregado = r.GetDateTime(16);
                if (!r.IsDBNull(17)) model.HoraEntregado = r.GetTimeSpan(17);
            }
            // Map repartidor_nombre from GetVentasByEstado (extra column at index 18)
            if (r.FieldCount > 18 && !r.IsDBNull(18))
                model.RepartidorNombre = r.GetString(18);
            return model;
        }

        private static VentaDetalleModel MapDetalle(NpgsqlDataReader r)
        {
            return new VentaDetalleModel
            {
                ProductoId = r.GetInt32(0),
                VentaId = r.GetInt32(1),
                Cantidad = r.IsDBNull(2) ? null : r.GetInt32(2),
                PrecioUnitario = r.IsDBNull(3) ? null : r.GetDecimal(3),
                ProductoNombre = r.IsDBNull(4) ? null : r.GetString(4),
            };
        }

        private static PagoModel MapPago(NpgsqlDataReader r)
        {
            return new PagoModel
            {
                PagoId = r.GetInt32(0),
                Fecha = r.GetDateTime(1),
                Hora = r.GetTimeSpan(2),
                Monto = r.GetDecimal(3),
                Metodo = r.IsDBNull(4) ? null : r.GetString(4),
                Estado = r.IsDBNull(5) ? null : r.GetString(5),
                VentaId = r.GetInt32(6),
            };
        }
    }
}
