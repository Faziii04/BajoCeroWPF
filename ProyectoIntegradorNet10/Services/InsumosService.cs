using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class InsumosService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;
        private const string Select = "id, nombre, descripcion, unidad_medida, precio_unitario, cantidad_stock";

        public static async Task<List<InsumoModel>> GetAll()
        {
            var list = new List<InsumoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand($"SELECT {Select} FROM insumos ORDER BY nombre", conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(Map(r));
            return list;
        }

        public static async Task<InsumoModel?> GetById(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand($"SELECT {Select} FROM insumos WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync()) return Map(r);
            return null;
        }

        public static async Task<int> Insert(InsumoModel m)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO insumos (nombre, descripcion, unidad_medida, precio_unitario, cantidad_stock) " +
                "VALUES (@nombre, @descripcion, @unidad_medida, @precio_unitario, @cantidad_stock) RETURNING id", conn);
            AddParams(cmd, m);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public static async Task Update(InsumoModel m)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE insumos SET nombre=@nombre, descripcion=@descripcion, unidad_medida=@unidad_medida, " +
                "precio_unitario=@precio_unitario, cantidad_stock=@cantidad_stock WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", m.Id);
            AddParams(cmd, m);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task AddStock(int id, decimal cantidad)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE insumos SET cantidad_stock = COALESCE(cantidad_stock, 0) + @cantidad WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@cantidad", cantidad);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Delete(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM insumos WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<InsumoModel>> Search(string term)
        {
            var list = new List<InsumoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                $"SELECT {Select} FROM insumos WHERE LOWER(nombre) LIKE @term OR LOWER(descripcion) LIKE @term ORDER BY nombre", conn);
            cmd.Parameters.AddWithValue("@term", $"%{term.ToLower()}%");
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(Map(r));
            return list;
        }

        private static InsumoModel Map(NpgsqlDataReader r) => new()
        {
            Id = r.GetInt32(0),
            Nombre = r.GetString(1),
            Descripcion = r.IsDBNull(2) ? null : r.GetString(2),
            UnidadMedida = r.IsDBNull(3) ? null : r.GetString(3),
            PrecioUnitario = r.IsDBNull(4) ? null : r.GetDecimal(4),
            CantidadStock = r.IsDBNull(5) ? null : r.GetDecimal(5),
        };

        private static void AddParams(NpgsqlCommand cmd, InsumoModel m)
        {
            cmd.Parameters.AddWithValue("@nombre", m.Nombre);
            cmd.Parameters.AddWithValue("@descripcion", (object?)m.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@unidad_medida", (object?)m.UnidadMedida ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@precio_unitario", (object?)m.PrecioUnitario ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cantidad_stock", (object?)m.CantidadStock ?? DBNull.Value);
        }
    }
}
