using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace ProyectoIntegradorNet10.Services
{
    public static class DepositosService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        public static async Task<List<DepositoModel>> GetAll()
        {
            var list = new List<DepositoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, nombre, direccion FROM deposito ORDER BY nombre", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }
            return list;
        }

        public static async Task<DepositoModel?> GetById(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, nombre, direccion FROM deposito WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return Map(reader);
            return null;
        }

        public static async Task Insert(DepositoModel d)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                @"INSERT INTO deposito (nombre, direccion)
                  VALUES (@nombre, @direccion)", conn);
            AddParams(cmd, d);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Update(DepositoModel d)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                @"UPDATE deposito SET nombre = @nombre, direccion = @direccion
                  WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", d.Id);
            AddParams(cmd, d);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Delete(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "DELETE FROM deposito WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<DepositoModel>> Search(string term)
        {
            var list = new List<DepositoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                @"SELECT id, nombre, direccion FROM deposito
                  WHERE LOWER(nombre) LIKE @term OR LOWER(direccion) LIKE @term
                  ORDER BY nombre", conn);
            cmd.Parameters.AddWithValue("@term", $"%{term.ToLower()}%");
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }
            return list;
        }

        private static DepositoModel Map(NpgsqlDataReader r)
        {
            return new DepositoModel
            {
                Id = r.GetInt32(0),
                Nombre = r.IsDBNull(1) ? string.Empty : r.GetString(1),
                Direccion = r.IsDBNull(2) ? null : r.GetString(2),
            };
        }

        private static void AddParams(NpgsqlCommand cmd, DepositoModel d)
        {
            cmd.Parameters.AddWithValue("@nombre", d.Nombre ?? string.Empty);
            cmd.Parameters.AddWithValue("@direccion", d.Direccion ?? (object)DBNull.Value);
        }
    }

}
