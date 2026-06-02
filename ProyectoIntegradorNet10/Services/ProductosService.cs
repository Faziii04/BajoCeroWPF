using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class ProductosService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        private const string SelectColumns =
            "p.id, p.nombre, p.precio_venta, p.estado, p.url, " +
            "COALESCE(SUM(pd.cantidad), 0) AS stock_total";

        private const string FromClause =
            "FROM producto p " +
            "LEFT JOIN producto_deposito pd ON pd.producto_id = p.id";

        private const string GroupBy =
            "GROUP BY p.id, p.nombre, p.precio_venta, p.estado, p.url";

        public static async Task<List<ProductoModel>> GetAll()
        {
            var list = new List<ProductoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                $"SELECT {SelectColumns} {FromClause} {GroupBy} ORDER BY p.nombre", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }
            return list;
        }

        public static async Task<ProductoModel?> GetById(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                $"SELECT {SelectColumns} {FromClause} WHERE p.id = @id {GroupBy}", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return Map(reader);
            return null;
        }

        public static async Task<int> Insert(ProductoModel p)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO producto (nombre, precio_venta, estado, url) " +
                "VALUES (@nombre, @precio_venta, @estado, @url) RETURNING id", conn);
            AddParams(cmd, p);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public static async Task Update(ProductoModel p)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE producto SET nombre = @nombre, " +
                "precio_venta = @precio_venta, estado = @estado, url = @url WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", p.Id);
            AddParams(cmd, p);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Delete(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE producto SET estado = 'Inactivo' WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<ProductoModel>> Search(string term)
        {
            var list = new List<ProductoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                $"SELECT {SelectColumns} {FromClause} " +
                "WHERE LOWER(p.nombre) LIKE @term " +
                $"{GroupBy} ORDER BY p.nombre", conn);
            cmd.Parameters.AddWithValue("@term", $"%{term.ToLower()}%");
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }
            return list;
        }

        private static ProductoModel Map(NpgsqlDataReader r)
        {
            return new ProductoModel
            {
                Id = r.GetInt32(0),
                Nombre = r.IsDBNull(1) ? string.Empty : r.GetString(1),
                PrecioVenta = r.IsDBNull(2) ? null : r.GetDecimal(2),
                Estado = r.IsDBNull(3) ? null : r.GetString(3),
                Url = r.IsDBNull(4) ? null : r.GetString(4),
                StockTotal = r.GetDecimal(5),
            };
        }

        private static void AddParams(NpgsqlCommand cmd, ProductoModel p)
        {
            cmd.Parameters.AddWithValue("@nombre", p.Nombre);
            cmd.Parameters.AddWithValue("@precio_venta", (object?)p.PrecioVenta ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@estado", (object?)p.Estado ?? "Activo");
            cmd.Parameters.AddWithValue("@url", (object?)p.Url ?? DBNull.Value);
        }
    }
}
