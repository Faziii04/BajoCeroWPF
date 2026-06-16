using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class IncidenciaService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        public static async Task<List<IncidenciaModel>> GetByVentaId(int ventaId)
        {
            var list = new List<IncidenciaModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, fecha, hora, motivo, resuelto, notas, venta_id " +
                "FROM incidencia WHERE venta_id = @ventaId ORDER BY fecha DESC, hora DESC", conn);
            cmd.Parameters.AddWithValue("@ventaId", ventaId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }
            return list;
        }

        public static async Task<int> Insert(IncidenciaModel model)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO incidencia (fecha, hora, motivo, resuelto, notas, venta_id) " +
                "VALUES (@fecha, @hora, @motivo, @resuelto, @notas, @ventaId) RETURNING id", conn);
            cmd.Parameters.AddWithValue("@fecha", model.Fecha);
            cmd.Parameters.AddWithValue("@hora", model.Hora);
            cmd.Parameters.AddWithValue("@motivo", model.Motivo);
            cmd.Parameters.AddWithValue("@resuelto", model.Resuelto);
            cmd.Parameters.AddWithValue("@notas", (object?)model.Notas ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ventaId", model.VentaId ?? 0);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public static async Task Update(IncidenciaModel model)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE incidencia SET resuelto = @resuelto, notas = @notas WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", model.Id);
            cmd.Parameters.AddWithValue("@resuelto", model.Resuelto);
            cmd.Parameters.AddWithValue("@notas", (object?)model.Notas ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        private static IncidenciaModel Map(NpgsqlDataReader r)
        {
            return new IncidenciaModel
            {
                Id = r.GetInt32(0),
                Fecha = r.GetDateTime(1),
                Hora = r.GetTimeSpan(2),
                Motivo = r.IsDBNull(3) ? "" : r.GetString(3),
                Resuelto = r.IsDBNull(4) ? false : r.GetBoolean(4),
                Notas = r.IsDBNull(5) ? null : r.GetString(5),
                VentaId = r.IsDBNull(6) ? null : r.GetInt32(6),
            };
        }
    }
}
