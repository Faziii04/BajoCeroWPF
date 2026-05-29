using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class ClientesService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        private const string SelectColumns = "ci, nombre, apellido, direccion, nit, telefono, url";

        public static async Task<List<ClienteModel>> GetAll()
        {
            var list = new List<ClienteModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                $"SELECT {SelectColumns} FROM cliente ORDER BY nombre, apellido", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }
            return list;
        }

        public static async Task<ClienteModel?> GetByCi(string ci)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                $"SELECT {SelectColumns} FROM cliente WHERE ci = @ci", conn);
            cmd.Parameters.AddWithValue("@ci", ci);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return Map(reader);
            return null;
        }

        public static async Task Insert(ClienteModel c)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO cliente (ci, nombre, apellido, direccion, nit, telefono, url) " +
                "VALUES (@ci, @nombre, @apellido, @direccion, @nit, @telefono, @url)", conn);
            AddParams(cmd, c);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Update(ClienteModel c)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE cliente SET nombre = @nombre, apellido = @apellido, direccion = @direccion, " +
                "nit = @nit, telefono = @telefono, url = @url WHERE ci = @ci", conn);
            AddParams(cmd, c);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Delete(string ci)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM cliente WHERE ci = @ci", conn);
            cmd.Parameters.AddWithValue("@ci", ci);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<ClienteModel>> Search(string term)
        {
            var list = new List<ClienteModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                $"SELECT {SelectColumns} FROM cliente " +
                "WHERE LOWER(nombre) LIKE @term OR LOWER(apellido) LIKE @term OR LOWER(ci) LIKE @term " +
                "ORDER BY nombre, apellido", conn);
            cmd.Parameters.AddWithValue("@term", $"%{term.ToLower()}%");
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }
            return list;
        }

        private static ClienteModel Map(NpgsqlDataReader r)
        {
            return new ClienteModel
            {
                Ci = r.GetString(0),
                Nombre = r.IsDBNull(1) ? null : r.GetString(1),
                Apellido = r.IsDBNull(2) ? null : r.GetString(2),
                Direccion = r.IsDBNull(3) ? null : r.GetString(3),
                Nit = r.IsDBNull(4) ? null : r.GetString(4),
                Telefono = r.IsDBNull(5) ? null : r.GetString(5),
                Url = r.IsDBNull(6) ? null : r.GetString(6),
            };
        }

        private static void AddParams(NpgsqlCommand cmd, ClienteModel c)
        {
            cmd.Parameters.AddWithValue("@ci", c.Ci);
            cmd.Parameters.AddWithValue("@nombre", (object?)c.Nombre ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@apellido", (object?)c.Apellido ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@direccion", (object?)c.Direccion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nit", (object?)c.Nit ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@telefono", (object?)c.Telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@url", (object?)c.Url ?? DBNull.Value);
        }
    }
}
