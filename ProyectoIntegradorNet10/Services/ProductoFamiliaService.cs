using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class ProductoFamiliaService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        public static async Task<List<FamiliaModel>> GetAll()
        {
            var list = new List<FamiliaModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                @"SELECT pf.id, pf.nombre, pf.descripcion, pf.url,
                         COALESCE(m.cnt, 0) AS miembro_count
                  FROM producto_familia pf
                  LEFT JOIN (SELECT familia_id, COUNT(*) AS cnt FROM miembros GROUP BY familia_id) m
                    ON m.familia_id = pf.id
                  ORDER BY pf.nombre", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }
            return list;
        }

        public static async Task<FamiliaModel?> GetById(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                @"SELECT pf.id, pf.nombre, pf.descripcion, pf.url,
                         COALESCE(m.cnt, 0) AS miembro_count
                  FROM producto_familia pf
                  LEFT JOIN (SELECT familia_id, COUNT(*) AS cnt FROM miembros GROUP BY familia_id) m
                    ON m.familia_id = pf.id
                  WHERE pf.id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return Map(reader);
            return null;
        }

        public static async Task<int> Insert(FamiliaModel f)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO producto_familia (nombre, descripcion, url) " +
                "VALUES (@nombre, @descripcion, @url) RETURNING id", conn);
            AddParams(cmd, f);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public static async Task Update(FamiliaModel f)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE producto_familia SET nombre = @nombre, descripcion = @descripcion, url = @url WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", f.Id);
            AddParams(cmd, f);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Delete(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            // First delete all memberships
            using (var cmd = new NpgsqlCommand("DELETE FROM miembros WHERE familia_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
            }
            // Then delete the family itself
            using var cmd2 = new NpgsqlCommand("DELETE FROM producto_familia WHERE id = @id", conn);
            cmd2.Parameters.AddWithValue("@id", id);
            await cmd2.ExecuteNonQueryAsync();
        }

        public static async Task<List<FamiliaModel>> Search(string term)
        {
            var list = new List<FamiliaModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                @"SELECT pf.id, pf.nombre, pf.descripcion, pf.url,
                         COALESCE(m.cnt, 0) AS miembro_count
                  FROM producto_familia pf
                  LEFT JOIN (SELECT familia_id, COUNT(*) AS cnt FROM miembros GROUP BY familia_id) m
                    ON m.familia_id = pf.id
                  WHERE LOWER(pf.nombre) LIKE @term OR LOWER(pf.descripcion) LIKE @term
                  ORDER BY pf.nombre", conn);
            cmd.Parameters.AddWithValue("@term", $"%{term.ToLower()}%");
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }
            return list;
        }

        private static FamiliaModel Map(NpgsqlDataReader r)
        {
            return new FamiliaModel
            {
                Id = r.GetInt32(0),
                Nombre = r.IsDBNull(1) ? string.Empty : r.GetString(1),
                Descripcion = r.IsDBNull(2) ? null : r.GetString(2),
                Url = r.IsDBNull(3) ? null : r.GetString(3),
                MiembroCount = r.GetInt32(4),
            };
        }

        // ════════════════════════════════════════════════════════════════
        // MEMBERSHIP (miembros table) METHODS
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gets the IDs of all families a product belongs to.
        /// </summary>
        public static async Task<HashSet<int>> GetFamiliaIdsByProducto(int productoId)
        {
            var ids = new HashSet<int>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT familia_id FROM miembros WHERE producto_id = @productoId", conn);
            cmd.Parameters.AddWithValue("@productoId", productoId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ids.Add(reader.GetInt32(0));
            }
            return ids;
        }

        /// <summary>
        /// Replaces all family memberships for a product with the new set.
        /// </summary>
        public static async Task SetFamiliasForProducto(int productoId, HashSet<int> familiaIds)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Remove all existing memberships for this product
                using (var cmd = new NpgsqlCommand(
                    "DELETE FROM miembros WHERE producto_id = @productoId", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@productoId", productoId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Insert new memberships
                foreach (var familiaId in familiaIds)
                {
                    using var cmd = new NpgsqlCommand(
                        "INSERT INTO miembros (producto_id, familia_id) VALUES (@productoId, @familiaId)", conn, tx);
                    cmd.Parameters.AddWithValue("@productoId", productoId);
                    cmd.Parameters.AddWithValue("@familiaId", familiaId);
                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Gets all products linked to a specific family.
        /// </summary>
        public static async Task<List<ProductoModel>> GetProductosByFamilia(int familiaId)
        {
            var list = new List<ProductoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                @"SELECT p.id, p.nombre, p.precio_venta, p.estado, p.url,
                         COALESCE(SUM(pd.cantidad), 0) AS stock_total
                  FROM producto p
                  INNER JOIN miembros m ON m.producto_id = p.id
                  LEFT JOIN producto_deposito pd ON pd.producto_id = p.id
                  WHERE m.familia_id = @familiaId
                  GROUP BY p.id, p.nombre, p.precio_venta, p.estado, p.url
                  ORDER BY p.nombre", conn);
            cmd.Parameters.AddWithValue("@familiaId", familiaId);
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

        /// <summary>
        /// Removes a single product from a family.
        /// </summary>
        public static async Task RemoveProductoFromFamilia(int productoId, int familiaId)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "DELETE FROM miembros WHERE producto_id = @productoId AND familia_id = @familiaId", conn);
            cmd.Parameters.AddWithValue("@productoId", productoId);
            cmd.Parameters.AddWithValue("@familiaId", familiaId);
            await cmd.ExecuteNonQueryAsync();
        }

        private static void AddParams(NpgsqlCommand cmd, FamiliaModel f)
        {
            cmd.Parameters.AddWithValue("@nombre", f.Nombre);
            cmd.Parameters.AddWithValue("@descripcion", (object?)f.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@url", (object?)f.Url ?? DBNull.Value);
        }
    }
}
