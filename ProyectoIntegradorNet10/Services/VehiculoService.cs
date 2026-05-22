using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class VehiculoService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        public static async Task<List<VehiculoModel>> GetAll()
        {
            var list = new List<VehiculoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT placa, marca, modelo, tipo, kilometraje, soat_vencimiento, ultima_actualizacion " +
                "FROM vehiculo ORDER BY marca, modelo", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(Map(reader));
            return list;
        }

        public static async Task<VehiculoModel?> GetByPlaca(string placa)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT placa, marca, modelo, tipo, kilometraje, soat_vencimiento, ultima_actualizacion " +
                "FROM vehiculo WHERE placa = @placa", conn);
            cmd.Parameters.AddWithValue("@placa", placa);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return Map(reader);
            return null;
        }

        public static async Task Insert(VehiculoModel v)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO vehiculo (placa, marca, modelo, tipo, kilometraje, soat_vencimiento, ultima_actualizacion) " +
                "VALUES (@placa, @marca, @modelo, @tipo, @kilometraje, @soat_vencimiento, @ultima_actualizacion)", conn);
            cmd.Parameters.AddWithValue("@placa", v.Placa);
            AddParams(cmd, v);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Update(VehiculoModel v)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE vehiculo SET marca = @marca, modelo = @modelo, tipo = @tipo, " +
                "kilometraje = @kilometraje, soat_vencimiento = @soat_vencimiento, " +
                "ultima_actualizacion = @ultima_actualizacion WHERE placa = @placa", conn);
            cmd.Parameters.AddWithValue("@placa", v.Placa);
            AddParams(cmd, v);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Delete(string placa)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "DELETE FROM vehiculo WHERE placa = @placa", conn);
            cmd.Parameters.AddWithValue("@placa", placa);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<VehiculoModel>> Search(string term)
        {
            var list = new List<VehiculoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT placa, marca, modelo, tipo, kilometraje, soat_vencimiento, ultima_actualizacion " +
                "FROM vehiculo WHERE LOWER(placa) LIKE @term OR LOWER(marca) LIKE @term " +
                "OR LOWER(modelo) LIKE @term OR LOWER(tipo) LIKE @term " +
                "ORDER BY marca, modelo", conn);
            cmd.Parameters.AddWithValue("@term", $"%{term.ToLower()}%");
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(Map(reader));
            return list;
        }

        private static VehiculoModel Map(NpgsqlDataReader r)
        {
            return new VehiculoModel
            {
                Placa = r.IsDBNull(0) ? string.Empty : r.GetString(0),
                Marca = r.IsDBNull(1) ? null : r.GetString(1),
                Modelo = r.IsDBNull(2) ? null : r.GetString(2),
                Tipo = r.IsDBNull(3) ? null : r.GetString(3),
                Kilometraje = r.IsDBNull(4) ? null : r.GetDecimal(4),
                SoatVencimiento = r.IsDBNull(5) ? null : r.GetDateTime(5),
                UltimaActualizacion = r.IsDBNull(6) ? null : r.GetDateTime(6),
            };
        }

        private static void AddParams(NpgsqlCommand cmd, VehiculoModel v)
        {
            cmd.Parameters.AddWithValue("@marca", (object?)v.Marca ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@modelo", (object?)v.Modelo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tipo", (object?)v.Tipo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@kilometraje", (object?)v.Kilometraje ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@soat_vencimiento", (object?)v.SoatVencimiento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ultima_actualizacion", (object?)v.UltimaActualizacion ?? DBNull.Value);
        }
    }
}
