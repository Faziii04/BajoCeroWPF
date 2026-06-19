using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class ProduccionService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        // ────────────────────────────── GET ALL ──────────────────────────────

        public static async Task<List<ProduccionModel>> GetAll()
        {
            var list = new List<ProduccionModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, fecha_inicio, fecha_fin, costo_total, estado " +
                "FROM produccion ORDER BY fecha_inicio DESC, id DESC", conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(Map(r));
            return list;
        }

        // ────────────────────────────── GET BY ID ──────────────────────────────

        public static async Task<ProduccionModel?> GetById(int id)
        {
            ProduccionModel? model = null;
            using var conn = await DS.OpenConnectionAsync();

            // Header
            using (var cmdH = new NpgsqlCommand(
                "SELECT id, fecha_inicio, fecha_fin, costo_total, estado FROM produccion WHERE id = @id", conn))
            {
                cmdH.Parameters.AddWithValue("@id", id);
                using var rH = await cmdH.ExecuteReaderAsync();
                if (await rH.ReadAsync()) model = Map(rH);
            }
            if (model == null) return null;

            // Insumos
            using (var cmdI = new NpgsqlCommand(@"
                SELECT ip.produccion_id, ip.insumo_id, ip.cantidad,
                       i.nombre AS insumo_nombre, i.precio_unitario, i.unidad_medida,
                       i.cantidad_stock
                FROM insumo_produccion ip
                JOIN insumos i ON i.id = ip.insumo_id
                WHERE ip.produccion_id = @id
                ORDER BY i.nombre", conn))
            {
                cmdI.Parameters.AddWithValue("@id", id);
                using var rI = await cmdI.ExecuteReaderAsync();
                while (await rI.ReadAsync())
                {
                    model.Insumos.Add(new ProduccionInsumoModel
                    {
                        ProduccionId = rI.GetInt32(0),
                        InsumoId = rI.GetInt32(1),
                        Cantidad = rI.IsDBNull(2) ? null : rI.GetDecimal(2),
                        InsumoNombre = rI.IsDBNull(3) ? null : rI.GetString(3),
                        InsumoPrecio = rI.IsDBNull(4) ? null : rI.GetDecimal(4),
                        UnidadMedida = rI.IsDBNull(5) ? null : rI.GetString(5),
                        StockDisponible = rI.IsDBNull(6) ? null : rI.GetDecimal(6),
                    });
                }
            }

            // Productos
            using (var cmdP = new NpgsqlCommand(@"
                SELECT pp.producto_id, pp.produccion_id, pp.cantidad,
                       p.nombre AS producto_nombre, p.precio_venta
                FROM produccion_producto pp
                JOIN producto p ON p.id = pp.producto_id
                WHERE pp.produccion_id = @id
                ORDER BY p.nombre", conn))
            {
                cmdP.Parameters.AddWithValue("@id", id);
                using var rP = await cmdP.ExecuteReaderAsync();
                while (await rP.ReadAsync())
                {
                    model.Productos.Add(new ProduccionProductoModel
                    {
                        ProductoId = rP.GetInt32(0),
                        ProduccionId = rP.GetInt32(1),
                        Cantidad = rP.IsDBNull(2) ? null : rP.GetDecimal(2),
                        ProductoNombre = rP.IsDBNull(3) ? null : rP.GetString(3),
                        PrecioVenta = rP.IsDBNull(4) ? null : rP.GetDecimal(4),
                    });
                }
            }

            return model;
        }

        // ────────────────────────────── INSERT (Planificado) ──────────────────────────────

        /// <summary>
        /// Creates a new production run with estado = "Planificado".
        /// No stock is affected at this stage.
        /// </summary>
        public static async Task<int> Insert(
            DateTime fechaInicio,
            decimal? costoTotal,
            List<ProduccionInsumoModel> insumos,
            List<ProduccionProductoModel> productos)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                using var cmd = new NpgsqlCommand(
                    "INSERT INTO produccion (fecha_inicio, costo_total, estado) " +
                    "VALUES (@fecha_inicio, @costo_total, 'Planificado') RETURNING id", conn, tx);
                cmd.Parameters.AddWithValue("@fecha_inicio", fechaInicio);
                cmd.Parameters.AddWithValue("@costo_total", (object?)costoTotal ?? DBNull.Value);
                int prodId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                await InsertInsumos(conn, tx, prodId, insumos);
                await InsertProductos(conn, tx, prodId, productos);

                await tx.CommitAsync();
                return prodId;
            }
            catch { await tx.RollbackAsync(); throw; }
        }

        // ────────────────────────────── UPDATE (Planificado only) ──────────────────────────────

        /// <summary>
        /// Updates a production run. Only allowed when estado = "Planificado".
        /// No stock effects (nothing has been deducted yet).
        /// </summary>
        public static async Task Update(
            int id,
            DateTime fechaInicio,
            decimal? costoTotal,
            List<ProduccionInsumoModel> insumos,
            List<ProduccionProductoModel> productos)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                using var cmd = new NpgsqlCommand(
                    "UPDATE produccion SET fecha_inicio = @fecha_inicio, costo_total = @costo_total WHERE id = @id", conn, tx);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@fecha_inicio", fechaInicio);
                cmd.Parameters.AddWithValue("@costo_total", (object?)costoTotal ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();

                // Replace insumos
                using (var delI = new NpgsqlCommand("DELETE FROM insumo_produccion WHERE produccion_id = @id", conn, tx))
                {
                    delI.Parameters.AddWithValue("@id", id);
                    await delI.ExecuteNonQueryAsync();
                }
                await InsertInsumos(conn, tx, id, insumos);

                // Replace productos
                using (var delP = new NpgsqlCommand("DELETE FROM produccion_producto WHERE produccion_id = @id", conn, tx))
                {
                    delP.Parameters.AddWithValue("@id", id);
                    await delP.ExecuteNonQueryAsync();
                }
                await InsertProductos(conn, tx, id, productos);

                await tx.CommitAsync();
            }
            catch { await tx.RollbackAsync(); throw; }
        }

        // ────────────────────────────── STATE TRANSITIONS ──────────────────────────────

        /// <summary>
        /// Transitions a production from "Planificado" → "En proceso".
        /// Deducts insumos from stock immediately. Validates stock availability first.
        /// </summary>
        public static async Task<(bool Success, string? Error)> Iniciar(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Load current insumos with stock info
                var insumos = new List<(int insumoId, decimal cantidad, string nombre, decimal stock)>();

                using (var cmdI = new NpgsqlCommand(@"
                    SELECT ip.insumo_id, COALESCE(ip.cantidad, 0), i.nombre, COALESCE(i.cantidad_stock, 0)
                    FROM insumo_produccion ip
                    JOIN insumos i ON i.id = ip.insumo_id
                    WHERE ip.produccion_id = @id", conn, tx))
                {
                    cmdI.Parameters.AddWithValue("@id", id);
                    using var rI = await cmdI.ExecuteReaderAsync();
                    while (await rI.ReadAsync())
                        insumos.Add((rI.GetInt32(0), rI.GetDecimal(1), rI.GetString(2), rI.GetDecimal(3)));
                }

                // Validate stock for each insumo
                foreach (var (_, cantidad, nombre, stock) in insumos)
                {
                    if (cantidad > stock)
                        return (false, $"Stock insuficiente de \"{nombre}\": solicitado {cantidad:N0}, disponible {stock:N0}");
                }

                // Deduct insumos
                foreach (var (insumoId, cantidad, _, _) in insumos)
                {
                    using var cmdD = new NpgsqlCommand(
                        "UPDATE insumos SET cantidad_stock = cantidad_stock - @cantidad WHERE id = @insumo_id",
                        conn, tx);
                    cmdD.Parameters.AddWithValue("@insumo_id", insumoId);
                    cmdD.Parameters.AddWithValue("@cantidad", cantidad);
                    await cmdD.ExecuteNonQueryAsync();
                }

                // Update estado
                using (var cmdU = new NpgsqlCommand(
                    "UPDATE produccion SET estado = 'En proceso', fecha_fin = NULL WHERE id = @id", conn, tx))
                {
                    cmdU.Parameters.AddWithValue("@id", id);
                    await cmdU.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, $"Error al iniciar producción: {ex.Message}");
            }
        }

        /// <summary>
        /// Transitions a production from "En proceso" → "Completado".
        /// Adds productos to the selected deposito stock and recalculates costo_total.
        /// </summary>
        public static async Task<(bool Success, string? Error)> Completar(int id, int depositoId)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Collect productos to add
                var productos = new List<(int productoId, decimal cantidad, string nombre)>();
                using (var cmdP = new NpgsqlCommand(@"
                    SELECT pp.producto_id, COALESCE(pp.cantidad, 0), COALESCE(p.nombre, '')
                    FROM produccion_producto pp
                    JOIN producto p ON p.id = pp.producto_id
                    WHERE pp.produccion_id = @id", conn, tx))
                {
                    cmdP.Parameters.AddWithValue("@id", id);
                    using var rP = await cmdP.ExecuteReaderAsync();
                    while (await rP.ReadAsync())
                        productos.Add((rP.GetInt32(0), rP.GetDecimal(1), rP.GetString(2)));
                }

                // Add productos to deposito stock using existing service
                foreach (var (productoId, cantidad, _) in productos)
                {
                    // Use raw SQL directly to stay within the same transaction
                    using var cmdS = new NpgsqlCommand(@"
                        INSERT INTO producto_deposito (producto_id, deposito_id, cantidad)
                        VALUES (@producto_id, @deposito_id, @cantidad)
                        ON CONFLICT (producto_id, deposito_id)
                        DO UPDATE SET cantidad = producto_deposito.cantidad + @cantidad", conn, tx);
                    cmdS.Parameters.AddWithValue("@producto_id", productoId);
                    cmdS.Parameters.AddWithValue("@deposito_id", depositoId);
                    cmdS.Parameters.AddWithValue("@cantidad", cantidad);
                    await cmdS.ExecuteNonQueryAsync();
                }

                // Recalculate costo_total from current insumo costs
                decimal costo;
                using (var cmdC = new NpgsqlCommand(@"
                    SELECT COALESCE(SUM(ip.cantidad * i.precio_unitario), 0)
                    FROM insumo_produccion ip
                    JOIN insumos i ON i.id = ip.insumo_id
                    WHERE ip.produccion_id = @id", conn, tx))
                {
                    cmdC.Parameters.AddWithValue("@id", id);
                    costo = Convert.ToDecimal(await cmdC.ExecuteScalarAsync());
                }

                // Update estado
                using (var cmdU = new NpgsqlCommand(
                    "UPDATE produccion SET estado = 'Completado', fecha_fin = NOW(), costo_total = @costo WHERE id = @id", conn, tx))
                {
                    cmdU.Parameters.AddWithValue("@id", id);
                    cmdU.Parameters.AddWithValue("@costo", costo);
                    await cmdU.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, $"Error al completar producción: {ex.Message}");
            }
        }

        /// <summary>
        /// Transitions a production to "Cancelado".
        /// Reverses any stock effects based on current estado:
        /// - Planificado → Cancelado: no reversal needed
        /// - En proceso → Cancelado: reverses insumo deductions
        /// - Completado → Cancelado: reverses both insumo and producto stock
        /// </summary>
        public static async Task<(bool Success, string? Error)> Cancelar(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                string? currentEstado;
                using (var cmdE = new NpgsqlCommand("SELECT estado FROM produccion WHERE id = @id", conn, tx))
                {
                    cmdE.Parameters.AddWithValue("@id", id);
                    currentEstado = (await cmdE.ExecuteScalarAsync())?.ToString();
                }
                if (currentEstado == null)
                    return (false, "Producción no encontrada");
                if (currentEstado == "Cancelado")
                    return (false, "La producción ya está cancelada");

                // Reverse insumo deductions if was En proceso or Completado
                if (currentEstado == "En proceso" || currentEstado == "Completado")
                {
                    var reversals = new List<(int insumoId, decimal cantidad)>();
                    using (var cmdI = new NpgsqlCommand(
                        "SELECT insumo_id, COALESCE(cantidad, 0) FROM insumo_produccion WHERE produccion_id = @id", conn, tx))
                    {
                        cmdI.Parameters.AddWithValue("@id", id);
                        using var rI = await cmdI.ExecuteReaderAsync();
                        while (await rI.ReadAsync())
                            reversals.Add((rI.GetInt32(0), rI.GetDecimal(1)));
                    }

                    foreach (var (insumoId, cantidad) in reversals)
                    {
                        using var cmdR = new NpgsqlCommand(
                            "UPDATE insumos SET cantidad_stock = COALESCE(cantidad_stock, 0) + @cantidad WHERE id = @insumo_id",
                            conn, tx);
                        cmdR.Parameters.AddWithValue("@insumo_id", insumoId);
                        cmdR.Parameters.AddWithValue("@cantidad", cantidad);
                        await cmdR.ExecuteNonQueryAsync();
                    }
                }

                // Reverse producto additions if was Completado
                if (currentEstado == "Completado")
                {
                    var prodReversals = new List<(int productoId, decimal cantidad)>();
                    using (var cmdP = new NpgsqlCommand(
                        "SELECT producto_id, COALESCE(cantidad, 0) FROM produccion_producto WHERE produccion_id = @id", conn, tx))
                    {
                        cmdP.Parameters.AddWithValue("@id", id);
                        using var rP = await cmdP.ExecuteReaderAsync();
                        while (await rP.ReadAsync())
                            prodReversals.Add((rP.GetInt32(0), rP.GetDecimal(1)));
                    }

                    foreach (var (productoId, cantidad) in prodReversals)
                    {
                        // Subtract from all depositos using GREATEST to avoid negative
                        using var cmdRS = new NpgsqlCommand(
                            "UPDATE producto_deposito SET cantidad = GREATEST(cantidad - @cantidad, 0) " +
                            "WHERE producto_id = @producto_id", conn, tx);
                        cmdRS.Parameters.AddWithValue("@producto_id", productoId);
                        cmdRS.Parameters.AddWithValue("@cantidad", cantidad);
                        await cmdRS.ExecuteNonQueryAsync();
                    }
                }

                // Update estado
                using (var cmdU = new NpgsqlCommand(
                    "UPDATE produccion SET estado = 'Cancelado', fecha_fin = NOW() WHERE id = @id", conn, tx))
                {
                    cmdU.Parameters.AddWithValue("@id", id);
                    await cmdU.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, $"Error al cancelar producción: {ex.Message}");
            }
        }

        // ────────────────────────────── DELETE ──────────────────────────────

        /// <summary>
        /// Deletes a production run. Reverses any stock effects based on current estado:
        /// - Planificado: just delete (no stock effects)
        /// - En proceso: reverse insumo deductions
        /// - Completado: reverse insumo deductions
        /// </summary>
        public static async Task<(bool Success, string? Error)> Delete(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Get current estado
                string? currentEstado;
                using (var cmdE = new NpgsqlCommand("SELECT estado FROM produccion WHERE id = @id", conn, tx))
                {
                    cmdE.Parameters.AddWithValue("@id", id);
                    currentEstado = (await cmdE.ExecuteScalarAsync())?.ToString();
                }
                if (currentEstado == null)
                    return (false, "Producción no encontrada");

                // Collect insumos to reverse (if En proceso or Completado)
                if (currentEstado == "En proceso" || currentEstado == "Completado")
                {
                    var reversals = new List<(int insumoId, decimal cantidad)>();
                    using (var cmdI = new NpgsqlCommand(
                        "SELECT insumo_id, COALESCE(cantidad, 0) FROM insumo_produccion WHERE produccion_id = @id", conn, tx))
                    {
                        cmdI.Parameters.AddWithValue("@id", id);
                        using var rI = await cmdI.ExecuteReaderAsync();
                        while (await rI.ReadAsync())
                            reversals.Add((rI.GetInt32(0), rI.GetDecimal(1)));
                    }

                    foreach (var (insumoId, cantidad) in reversals)
                    {
                        using var cmdR = new NpgsqlCommand(
                            "UPDATE insumos SET cantidad_stock = COALESCE(cantidad_stock, 0) + @cantidad WHERE id = @insumo_id",
                            conn, tx);
                        cmdR.Parameters.AddWithValue("@insumo_id", insumoId);
                        cmdR.Parameters.AddWithValue("@cantidad", cantidad);
                        await cmdR.ExecuteNonQueryAsync();
                    }
                }

                // Delete pivot rows
                using (var delIP = new NpgsqlCommand("DELETE FROM insumo_produccion WHERE produccion_id = @id", conn, tx))
                {
                    delIP.Parameters.AddWithValue("@id", id);
                    await delIP.ExecuteNonQueryAsync();
                }

                using (var delPP = new NpgsqlCommand("DELETE FROM produccion_producto WHERE produccion_id = @id", conn, tx))
                {
                    delPP.Parameters.AddWithValue("@id", id);
                    await delPP.ExecuteNonQueryAsync();
                }

                // Delete production
                using (var delP = new NpgsqlCommand("DELETE FROM produccion WHERE id = @id", conn, tx))
                {
                    delP.Parameters.AddWithValue("@id", id);
                    await delP.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, $"Error al eliminar producción: {ex.Message}");
            }
        }

        // ────────────────────────────── FILTER/SEARCH ──────────────────────────────

        public static async Task<List<ProduccionModel>> GetFiltered(
            string? estado = null,
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            var sql = new StringBuilder("SELECT id, fecha_inicio, fecha_fin, costo_total, estado FROM produccion WHERE 1=1");
            var pars = new List<NpgsqlParameter>();

            if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            {
                sql.Append(" AND estado = @estado");
                pars.Add(new NpgsqlParameter("@estado", estado));
            }

            if (desde.HasValue)
            {
                sql.Append(" AND fecha_inicio >= @desde");
                pars.Add(new NpgsqlParameter("@desde", desde.Value));
            }

            if (hasta.HasValue)
            {
                sql.Append(" AND fecha_inicio <= @hasta");
                pars.Add(new NpgsqlParameter("@hasta", hasta.Value.AddDays(1)));
            }

            sql.Append(" ORDER BY fecha_inicio DESC, id DESC");

            var list = new List<ProduccionModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(sql.ToString(), conn);
            cmd.Parameters.AddRange(pars.ToArray());
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(Map(r));
            return list;
        }

        public static async Task<List<ProduccionModel>> Search(string term)
        {
            var list = new List<ProduccionModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT id, fecha_inicio, fecha_fin, costo_total, estado FROM produccion
                WHERE CAST(id AS TEXT) LIKE @term
                OR LOWER(estado) LIKE @term
                ORDER BY fecha_inicio DESC, id DESC", conn);
            cmd.Parameters.AddWithValue("@term", $"%{term.ToLower()}%");
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(Map(r));
            return list;
        }

        // ────────────────────────────── HELPERS ──────────────────────────────

        /// <summary>
        /// Pre-validates stock availability for all insumos before starting.
        /// Returns null if OK, or error message listing insufficient items.
        /// </summary>
        public static async Task<string?> ValidateStock(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT i.nombre, ip.cantidad, i.cantidad_stock
                FROM insumo_produccion ip
                JOIN insumos i ON i.id = ip.insumo_id
                WHERE ip.produccion_id = @id
                  AND (i.cantidad_stock IS NULL OR i.cantidad_stock < ip.cantidad)", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                string nombre = r.IsDBNull(0) ? "?" : r.GetString(0);
                decimal requested = r.IsDBNull(1) ? 0 : r.GetDecimal(1);
                decimal available = r.IsDBNull(2) ? 0 : r.GetDecimal(2);
                return $"Stock insuficiente de \"{nombre}\": solicitado {requested:N0}, disponible {available:N0}";
            }
            return null;
        }

        private static ProduccionModel Map(NpgsqlDataReader r) => new()
        {
            Id = r.GetInt32(0),
            FechaInicio = r.GetDateTime(1),
            FechaFin = r.IsDBNull(2) ? null : r.GetDateTime(2),
            CostoTotal = r.IsDBNull(3) ? null : r.GetDecimal(3),
            Estado = r.IsDBNull(4) ? null : r.GetString(4),
        };

        private static async Task InsertInsumos(NpgsqlConnection conn, NpgsqlTransaction tx, int prodId, List<ProduccionInsumoModel> insumos)
        {
            foreach (var ins in insumos)
            {
                using var cmd = new NpgsqlCommand(
                    "INSERT INTO insumo_produccion (produccion_id, insumo_id, cantidad) VALUES (@prod_id, @insumo_id, @cantidad)",
                    conn, tx);
                cmd.Parameters.AddWithValue("@prod_id", prodId);
                cmd.Parameters.AddWithValue("@insumo_id", ins.InsumoId);
                cmd.Parameters.AddWithValue("@cantidad", (object?)ins.Cantidad ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static async Task InsertProductos(NpgsqlConnection conn, NpgsqlTransaction tx, int prodId, List<ProduccionProductoModel> productos)
        {
            foreach (var prod in productos)
            {
                using var cmd = new NpgsqlCommand(
                    "INSERT INTO produccion_producto (producto_id, produccion_id, cantidad) VALUES (@producto_id, @prod_id, @cantidad)",
                    conn, tx);
                cmd.Parameters.AddWithValue("@producto_id", prod.ProductoId);
                cmd.Parameters.AddWithValue("@prod_id", prodId);
                cmd.Parameters.AddWithValue("@cantidad", (object?)prod.Cantidad ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
