using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class PrestamosService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        // ────────────────────────────────────────────────────────────────
        //  GET ALL (list for DataGrid)
        // ────────────────────────────────────────────────────────────────
        public static async Task<List<PrestamoModel>> GetAll()
        {
            var list = new List<PrestamoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT p.id, p.fecha, p.estado,
                       c.nombre || ' ' || c.apellido AS cliente_nombre,
                       COALESCE(SUM(pd.valor_reposicion), 0) AS total
                FROM prestamo p
                LEFT JOIN prestamo_detalle pd ON pd.prestamo_id = p.id
                LEFT JOIN cliente c ON c.ci = pd.cliente_ci
                GROUP BY p.id, p.fecha, p.estado, c.nombre, c.apellido
                ORDER BY p.fecha DESC", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapPrestamo(reader));
            }
            return list;
        }

        // ────────────────────────────────────────────────────────────────
        //  GET FILTERED (smart filters for the UC)
        // ────────────────────────────────────────────────────────────────
        public static async Task<List<PrestamoModel>> GetFiltered(
            string? estado = null,
            string? clienteSearch = null,
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            var sql = new StringBuilder(@"
                SELECT p.id, p.fecha, p.estado,
                       c.nombre || ' ' || c.apellido AS cliente_nombre,
                       COALESCE(SUM(pd.valor_reposicion), 0) AS total
                FROM prestamo p
                LEFT JOIN prestamo_detalle pd ON pd.prestamo_id = p.id
                LEFT JOIN cliente c ON c.ci = pd.cliente_ci
                WHERE 1=1");

            var parameters = new List<NpgsqlParameter>();

            if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            {
                sql.Append(" AND p.estado = @estado");
                parameters.Add(new NpgsqlParameter("@estado", estado));
            }

            if (!string.IsNullOrWhiteSpace(clienteSearch))
            {
                sql.Append(" AND (LOWER(c.nombre || ' ' || c.apellido) LIKE @clienteSearch OR LOWER(c.ci) LIKE @clienteSearch2)");
                parameters.Add(new NpgsqlParameter("@clienteSearch", $"%{clienteSearch.ToLower()}%"));
                parameters.Add(new NpgsqlParameter("@clienteSearch2", $"%{clienteSearch.ToLower()}%"));
            }

            if (desde.HasValue)
            {
                sql.Append(" AND p.fecha >= @desde");
                parameters.Add(new NpgsqlParameter("@desde", desde.Value));
            }

            if (hasta.HasValue)
            {
                sql.Append(" AND p.fecha <= @hasta");
                parameters.Add(new NpgsqlParameter("@hasta", hasta.Value.AddDays(1))); // include full day
            }

            sql.Append(@" GROUP BY p.id, p.fecha, p.estado, c.nombre, c.apellido
                           ORDER BY p.fecha DESC");

            var list = new List<PrestamoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(sql.ToString(), conn);
            cmd.Parameters.AddRange(parameters.ToArray());
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapPrestamo(reader));
            }
            return list;
        }

        // ────────────────────────────────────────────────────────────────
        //  GET BY ID (single loan + its detalles)
        // ────────────────────────────────────────────────────────────────
        public static async Task<(PrestamoModel? prestamo, List<PrestamoDetalleModel> detalles)> GetById(int id)
        {
            PrestamoModel? prestamo = null;
            var detalles = new List<PrestamoDetalleModel>();

            using var conn = await DS.OpenConnectionAsync();

            // 1) Load loan header
            using var cmdPrestamo = new NpgsqlCommand(@"
                SELECT p.id, p.fecha, p.estado,
                       c.nombre || ' ' || c.apellido AS cliente_nombre,
                       COALESCE(SUM(pd2.valor_reposicion), 0) AS total
                FROM prestamo p
                LEFT JOIN prestamo_detalle pd2 ON pd2.prestamo_id = p.id
                LEFT JOIN cliente c ON c.ci = pd2.cliente_ci
                WHERE p.id = @id
                GROUP BY p.id, p.fecha, p.estado, c.nombre, c.apellido", conn);
            cmdPrestamo.Parameters.AddWithValue("@id", id);
            using var readerP = await cmdPrestamo.ExecuteReaderAsync();
            if (await readerP.ReadAsync())
            {
                prestamo = MapPrestamo(readerP);
            }
            readerP.Close();

            if (prestamo == null)
                return (null, detalles);

            // 2) Load detalles
            using var cmdDetalles = new NpgsqlCommand(@"
                SELECT pd.cliente_ci, pd.producto_id, pd.prestamo_id,
                       pd.cantidad, pd.valor_reposicion,
                       pr.nombre AS producto_nombre, pr.precio_venta AS producto_precio
                FROM prestamo_detalle pd
                JOIN producto pr ON pr.id = pd.producto_id
                WHERE pd.prestamo_id = @id
                ORDER BY pr.nombre", conn);
            cmdDetalles.Parameters.AddWithValue("@id", id);
            using var readerD = await cmdDetalles.ExecuteReaderAsync();
            while (await readerD.ReadAsync())
            {
                detalles.Add(MapDetalle(readerD));
            }

            return (prestamo, detalles);
        }

        // ────────────────────────────────────────────────────────────────
        //  INSERT (transactional — prestamo + detalles)
        // ────────────────────────────────────────────────────────────────
        public static async Task<int> Insert(
            string clienteCi,
            string estado,
            List<PrestamoDetalleModel> detalles)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Insert prestamo header
                using var cmd = new NpgsqlCommand(@"
                    INSERT INTO prestamo (fecha, estado)
                    VALUES (CURRENT_TIMESTAMP, @estado)
                    RETURNING id", conn, tx);
                cmd.Parameters.AddWithValue("@estado", (object?)estado ?? "Activo");
                var result = await cmd.ExecuteScalarAsync();
                int prestamoId = Convert.ToInt32(result);

                // Insert each detalle
                foreach (var d in detalles)
                {
                    using var cmdD = new NpgsqlCommand(@"
                        INSERT INTO prestamo_detalle (cliente_ci, producto_id, prestamo_id, cantidad, valor_reposicion)
                        VALUES (@cliente_ci, @producto_id, @prestamo_id, @cantidad, @valor_reposicion)", conn, tx);
                    cmdD.Parameters.AddWithValue("@cliente_ci", d.ClienteCi);
                    cmdD.Parameters.AddWithValue("@producto_id", d.ProductoId);
                    cmdD.Parameters.AddWithValue("@prestamo_id", prestamoId);
                    cmdD.Parameters.AddWithValue("@cantidad", (object?)d.Cantidad ?? DBNull.Value);
                    cmdD.Parameters.AddWithValue("@valor_reposicion", (object?)d.ValorReposicion ?? DBNull.Value);
                    await cmdD.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return prestamoId;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ────────────────────────────────────────────────────────────────
        //  UPDATE (transactional — delete old detalles + re-insert)
        // ────────────────────────────────────────────────────────────────
        public static async Task Update(
            int prestamoId,
            string clienteCi,
            string estado,
            List<PrestamoDetalleModel> detalles)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Update prestamo header (estado can change, fecha stays)
                using var cmdUpd = new NpgsqlCommand(@"
                    UPDATE prestamo SET estado = @estado
                    WHERE id = @id", conn, tx);
                cmdUpd.Parameters.AddWithValue("@estado", (object?)estado ?? "Activo");
                cmdUpd.Parameters.AddWithValue("@id", prestamoId);
                await cmdUpd.ExecuteNonQueryAsync();

                // Delete old detalles
                using var cmdDel = new NpgsqlCommand(
                    "DELETE FROM prestamo_detalle WHERE prestamo_id = @id", conn, tx);
                cmdDel.Parameters.AddWithValue("@id", prestamoId);
                await cmdDel.ExecuteNonQueryAsync();

                // Insert new detalles
                foreach (var d in detalles)
                {
                    using var cmdD = new NpgsqlCommand(@"
                        INSERT INTO prestamo_detalle (cliente_ci, producto_id, prestamo_id, cantidad, valor_reposicion)
                        VALUES (@cliente_ci, @producto_id, @prestamo_id, @cantidad, @valor_reposicion)", conn, tx);
                    cmdD.Parameters.AddWithValue("@cliente_ci", d.ClienteCi);
                    cmdD.Parameters.AddWithValue("@producto_id", d.ProductoId);
                    cmdD.Parameters.AddWithValue("@prestamo_id", prestamoId);
                    cmdD.Parameters.AddWithValue("@cantidad", (object?)d.Cantidad ?? DBNull.Value);
                    cmdD.Parameters.AddWithValue("@valor_reposicion", (object?)d.ValorReposicion ?? DBNull.Value);
                    await cmdD.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ────────────────────────────────────────────────────────────────
        //  DELETE (transactional)
        // ────────────────────────────────────────────────────────────────
        public static async Task Delete(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                using var cmdDet = new NpgsqlCommand(
                    "DELETE FROM prestamo_detalle WHERE prestamo_id = @id", conn, tx);
                cmdDet.Parameters.AddWithValue("@id", id);
                await cmdDet.ExecuteNonQueryAsync();

                using var cmdPre = new NpgsqlCommand(
                    "DELETE FROM prestamo WHERE id = @id", conn, tx);
                cmdPre.Parameters.AddWithValue("@id", id);
                await cmdPre.ExecuteNonQueryAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ────────────────────────────────────────────────────────────────
        //  MAPPERS
        // ────────────────────────────────────────────────────────────────
        private static PrestamoModel MapPrestamo(NpgsqlDataReader r)
        {
            return new PrestamoModel
            {
                Id = r.GetInt32(0),
                Fecha = r.GetDateTime(1),
                Estado = r.IsDBNull(2) ? null : r.GetString(2),
                ClienteNombre = r.IsDBNull(3) ? null : r.GetString(3),
                ValorTotal = r.GetDecimal(4),
            };
        }

        private static PrestamoDetalleModel MapDetalle(NpgsqlDataReader r)
        {
            return new PrestamoDetalleModel
            {
                ClienteCi = r.GetString(0),
                ProductoId = r.GetInt32(1),
                PrestamoId = r.GetInt32(2),
                Cantidad = r.IsDBNull(3) ? null : r.GetInt32(3),
                ValorReposicion = r.IsDBNull(4) ? null : r.GetDecimal(4),
                ProductoNombre = r.IsDBNull(5) ? null : r.GetString(5),
                ProductoPrecio = r.IsDBNull(6) ? null : r.GetDecimal(6),
            };
        }
    }
}
