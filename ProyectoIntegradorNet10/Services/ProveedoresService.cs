using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class ProveedoresService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        private const string SelectColumns = "id, nombre, direccion, telefono, descripcion";

        public static async Task<List<ProveedorModel>> GetAll()
        {
            var list = new List<ProveedorModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand($"SELECT {SelectColumns} FROM proveedor ORDER BY nombre", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) list.Add(Map(reader));
            return list;
        }

        public static async Task<ProveedorModel?> GetById(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand($"SELECT {SelectColumns} FROM proveedor WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync()) return Map(reader);
            return null;
        }

        public static async Task<int> Insert(ProveedorModel p)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO proveedor (nombre, direccion, telefono, descripcion) " +
                "VALUES (@nombre, @direccion, @telefono, @descripcion) RETURNING id", conn);
            AddParams(cmd, p);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public static async Task Update(ProveedorModel p)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE proveedor SET nombre = @nombre, direccion = @direccion, " +
                "telefono = @telefono, descripcion = @descripcion WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", p.Id);
            AddParams(cmd, p);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Delete(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM proveedor WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<ProveedorModel>> Search(string term)
        {
            var list = new List<ProveedorModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                $"SELECT {SelectColumns} FROM proveedor WHERE LOWER(nombre) LIKE @term OR LOWER(direccion) LIKE @term OR LOWER(telefono) LIKE @term OR LOWER(descripcion) LIKE @term ORDER BY nombre", conn);
            cmd.Parameters.AddWithValue("@term", $"%{term.ToLower()}%");
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) list.Add(Map(reader));
            return list;
        }

        private static ProveedorModel Map(NpgsqlDataReader r) => new()
        {
            Id = r.GetInt32(0),
            Nombre = r.GetString(1),
            Direccion = r.IsDBNull(2) ? null : r.GetString(2),
            Telefono = r.IsDBNull(3) ? null : r.GetString(3),
            Descripcion = r.IsDBNull(4) ? null : r.GetString(4),
        };

        private static void AddParams(NpgsqlCommand cmd, ProveedorModel p)
        {
            cmd.Parameters.AddWithValue("@nombre", p.Nombre);
            cmd.Parameters.AddWithValue("@direccion", (object?)p.Direccion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@telefono", (object?)p.Telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@descripcion", (object?)p.Descripcion ?? DBNull.Value);
        }
    }
}
