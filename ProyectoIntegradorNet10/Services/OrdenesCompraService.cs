using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class OrdenesCompraService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        public static async Task<List<OrdenCompraModel>> GetAll()
        {
            var list = new List<OrdenCompraModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT oc.id, oc.fecha_pedido, oc.hora_pedido, oc.estado, oc.monto,
                       oc.proveedor, p.nombre AS proveedor_nombre,
                       oc.fecha_llegada, oc.hora_llegada
                FROM orden_compra oc LEFT JOIN proveedor p ON p.id = oc.proveedor
                ORDER BY oc.fecha_pedido DESC, oc.hora_pedido DESC", conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(MapOrden(r));
            return list;
        }

        public static async Task<(OrdenCompraModel? orden, List<DetalleOrdenModel> detalles)> GetById(int id)
        {
            OrdenCompraModel? orden = null;
            var detalles = new List<DetalleOrdenModel>();
            using var conn = await DS.OpenConnectionAsync();

            using var cmdO = new NpgsqlCommand(@"
                SELECT oc.id, oc.fecha_pedido, oc.hora_pedido, oc.estado, oc.monto,
                       oc.proveedor, p.nombre AS proveedor_nombre,
                       oc.fecha_llegada, oc.hora_llegada
                FROM orden_compra oc LEFT JOIN proveedor p ON p.id = oc.proveedor WHERE oc.id = @id", conn);
            cmdO.Parameters.AddWithValue("@id", id);
            using var rO = await cmdO.ExecuteReaderAsync();
            if (await rO.ReadAsync()) orden = MapOrden(rO);
            rO.Close();
            if (orden == null) return (null, detalles);

            using var cmdD = new NpgsqlCommand(@"
                SELECT d.orden_id, d.insumo_id, d.cantidad, i.nombre AS insumo_nombre, i.precio_unitario
                FROM detalles_orden d JOIN insumos i ON i.id = d.insumo_id WHERE d.orden_id = @id ORDER BY i.nombre", conn);
            cmdD.Parameters.AddWithValue("@id", id);
            using var rD = await cmdD.ExecuteReaderAsync();
            while (await rD.ReadAsync()) detalles.Add(MapDetalle(rD));
            return (orden, detalles);
        }

        public static async Task<int> Insert(int proveedorId, string estado, DateTime fechaPedido, TimeSpan horaPedido,
            List<DetalleOrdenModel> detalles, DateTime? fechaLlegada = null, TimeSpan? horaLlegada = null)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                decimal monto = 0;
                foreach (var d in detalles) monto += (d.Cantidad.GetValueOrDefault() * d.InsumoPrecio.GetValueOrDefault());

                using var cmd = new NpgsqlCommand(@"
                    INSERT INTO orden_compra (fecha_pedido, hora_pedido, estado, monto, proveedor, fecha_llegada, hora_llegada)
                    VALUES (@fecha_pedido, @hora_pedido, @estado, @monto, @proveedor, @fecha_llegada, @hora_llegada) RETURNING id", conn, tx);
                cmd.Parameters.AddWithValue("@fecha_pedido", fechaPedido);
                cmd.Parameters.AddWithValue("@hora_pedido", horaPedido);
                cmd.Parameters.AddWithValue("@estado", (object?)estado ?? "Pendiente");
                cmd.Parameters.AddWithValue("@monto", monto);
                cmd.Parameters.AddWithValue("@proveedor", proveedorId);
                cmd.Parameters.AddWithValue("@fecha_llegada", (object?)fechaLlegada ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@hora_llegada", (object?)horaLlegada ?? DBNull.Value);
                int ordenId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                foreach (var d in detalles)
                {
                    using var cmdD = new NpgsqlCommand(
                        "INSERT INTO detalles_orden (orden_id, insumo_id, cantidad) VALUES (@orden_id, @insumo_id, @cantidad)", conn, tx);
                    cmdD.Parameters.AddWithValue("@orden_id", ordenId);
                    cmdD.Parameters.AddWithValue("@insumo_id", d.InsumoId);
                    cmdD.Parameters.AddWithValue("@cantidad", (object?)d.Cantidad ?? DBNull.Value);
                    await cmdD.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
                return ordenId;
            }
            catch { await tx.RollbackAsync(); throw; }
        }

        public static async Task Update(int ordenId, int proveedorId, string estado, DateTime fechaPedido, TimeSpan horaPedido,
            List<DetalleOrdenModel> detalles, DateTime? fechaLlegada = null, TimeSpan? horaLlegada = null)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                decimal monto = 0;
                foreach (var d in detalles) monto += (d.Cantidad.GetValueOrDefault() * d.InsumoPrecio.GetValueOrDefault());

                using var cmdU = new NpgsqlCommand(@"
                    UPDATE orden_compra SET fecha_pedido=@fecha_pedido, hora_pedido=@hora_pedido, estado=@estado,
                    monto=@monto, proveedor=@proveedor, fecha_llegada=@fecha_llegada, hora_llegada=@hora_llegada WHERE id=@id", conn, tx);
                cmdU.Parameters.AddWithValue("@id", ordenId);
                cmdU.Parameters.AddWithValue("@fecha_pedido", fechaPedido);
                cmdU.Parameters.AddWithValue("@hora_pedido", horaPedido);
                cmdU.Parameters.AddWithValue("@estado", (object?)estado ?? "Pendiente");
                cmdU.Parameters.AddWithValue("@monto", monto);
                cmdU.Parameters.AddWithValue("@proveedor", proveedorId);
                cmdU.Parameters.AddWithValue("@fecha_llegada", (object?)fechaLlegada ?? DBNull.Value);
                cmdU.Parameters.AddWithValue("@hora_llegada", (object?)horaLlegada ?? DBNull.Value);
                await cmdU.ExecuteNonQueryAsync();

                using var cmdDel = new NpgsqlCommand("DELETE FROM detalles_orden WHERE orden_id = @id", conn, tx);
                cmdDel.Parameters.AddWithValue("@id", ordenId);
                await cmdDel.ExecuteNonQueryAsync();

                foreach (var d in detalles)
                {
                    using var cmdD = new NpgsqlCommand(
                        "INSERT INTO detalles_orden (orden_id, insumo_id, cantidad) VALUES (@orden_id, @insumo_id, @cantidad)", conn, tx);
                    cmdD.Parameters.AddWithValue("@orden_id", ordenId);
                    cmdD.Parameters.AddWithValue("@insumo_id", d.InsumoId);
                    cmdD.Parameters.AddWithValue("@cantidad", (object?)d.Cantidad ?? DBNull.Value);
                    await cmdD.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
            }
            catch { await tx.RollbackAsync(); throw; }
        }

        public static async Task UpdateEstado(int id, string estado)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand("UPDATE orden_compra SET estado = @estado WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@estado", estado);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Delete(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                using var cmdD = new NpgsqlCommand("DELETE FROM detalles_orden WHERE orden_id = @id", conn, tx);
                cmdD.Parameters.AddWithValue("@id", id); await cmdD.ExecuteNonQueryAsync();
                using var cmdO = new NpgsqlCommand("DELETE FROM orden_compra WHERE id = @id", conn, tx);
                cmdO.Parameters.AddWithValue("@id", id); await cmdO.ExecuteNonQueryAsync();
                await tx.CommitAsync();
            }
            catch { await tx.RollbackAsync(); throw; }
        }

        public static async Task<List<OrdenCompraModel>> GetFiltered(
            string? estado = null,
            string? proveedorSearch = null,
            DateTime? llegadaDesde = null,
            DateTime? llegadaHasta = null)
        {
            var sql = new System.Text.StringBuilder(@"
                SELECT oc.id, oc.fecha_pedido, oc.hora_pedido, oc.estado, oc.monto,
                       oc.proveedor, p.nombre AS proveedor_nombre,
                       oc.fecha_llegada, oc.hora_llegada
                FROM orden_compra oc LEFT JOIN proveedor p ON p.id = oc.proveedor
                WHERE 1=1");

            var pars = new List<NpgsqlParameter>();

            if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            {
                sql.Append(" AND oc.estado = @estado");
                pars.Add(new NpgsqlParameter("@estado", estado));
            }

            if (!string.IsNullOrWhiteSpace(proveedorSearch))
            {
                sql.Append(" AND LOWER(p.nombre) LIKE @provSearch");
                pars.Add(new NpgsqlParameter("@provSearch", $"%{proveedorSearch.ToLower()}%"));
            }

            if (llegadaDesde.HasValue)
            {
                sql.Append(" AND oc.fecha_llegada >= @llegadaDesde");
                pars.Add(new NpgsqlParameter("@llegadaDesde", llegadaDesde.Value));
            }

            if (llegadaHasta.HasValue)
            {
                sql.Append(" AND oc.fecha_llegada <= @llegadaHasta");
                pars.Add(new NpgsqlParameter("@llegadaHasta", llegadaHasta.Value.AddDays(1)));
            }

            sql.Append(" ORDER BY oc.fecha_pedido DESC, oc.hora_pedido DESC");

            var list = new List<OrdenCompraModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(sql.ToString(), conn);
            cmd.Parameters.AddRange(pars.ToArray());
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(MapOrden(r));
            return list;
        }

        public static async Task<List<OrdenCompraModel>> Search(string term)
        {
            var list = new List<OrdenCompraModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT oc.id, oc.fecha_pedido, oc.hora_pedido, oc.estado, oc.monto,
                       oc.proveedor, p.nombre AS proveedor_nombre,
                       oc.fecha_llegada, oc.hora_llegada
                FROM orden_compra oc LEFT JOIN proveedor p ON p.id = oc.proveedor
                WHERE CAST(oc.id AS TEXT) LIKE @term OR LOWER(p.nombre) LIKE @term OR LOWER(oc.estado) LIKE @term
                ORDER BY oc.fecha_pedido DESC, oc.hora_pedido DESC", conn);
            cmd.Parameters.AddWithValue("@term", $"%{term.ToLower()}%");
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(MapOrden(r));
            return list;
        }

        private static OrdenCompraModel MapOrden(NpgsqlDataReader r) => new()
        {
            Id = r.GetInt32(0),
            FechaPedido = r.GetDateTime(1),
            HoraPedido = r.GetFieldValue<TimeSpan>(2),
            Estado = r.IsDBNull(3) ? null : r.GetString(3),
            Monto = r.IsDBNull(4) ? null : r.GetDecimal(4),
            ProveedorId = r.GetInt32(5),
            ProveedorNombre = r.IsDBNull(6) ? null : r.GetString(6),
            FechaLlegada = r.IsDBNull(7) ? null : r.GetDateTime(7),
            HoraLlegada = r.IsDBNull(8) ? null : r.GetFieldValue<TimeSpan>(8),
        };

        private static DetalleOrdenModel MapDetalle(NpgsqlDataReader r) => new()
        {
            OrdenId = r.GetInt32(0),
            InsumoId = r.GetInt32(1),
            Cantidad = r.IsDBNull(2) ? null : r.GetDecimal(2),
            InsumoNombre = r.IsDBNull(3) ? null : r.GetString(3),
            InsumoPrecio = r.IsDBNull(4) ? null : r.GetDecimal(4),
        };
    }
}
