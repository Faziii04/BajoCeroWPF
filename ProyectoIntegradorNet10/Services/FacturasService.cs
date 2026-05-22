using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class FacturasService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        // ──────────── CRUD ────────────

        public static async Task<List<FacturaModel>> GetAll()
        {
            var list = new List<FacturaModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT f.id, f.venta_id, f.subtotal, f.total, f.descuento, f.fecha_emision, " +
                "f.nombre_completo, f.nit, f.descuento_tipo, " +
                "COALESCE(c.nombre || ' ' || c.apellido, '') AS cliente_nombre, " +
                "COALESCE(v.tipo, '') AS venta_tipo, " +
                "COALESCE(v.estado, '') AS venta_estado " +
                "FROM factura f " +
                "LEFT JOIN venta v ON v.id = f.venta_id " +
                "LEFT JOIN cliente c ON c.ci = v.cliente_ci " +
                "ORDER BY f.id DESC", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(Map(reader));
            return list;
        }

        public static async Task<FacturaModel?> GetById(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT f.id, f.venta_id, f.subtotal, f.total, f.descuento, f.fecha_emision, " +
                "f.nombre_completo, f.nit, f.descuento_tipo, " +
                "COALESCE(c.nombre || ' ' || c.apellido, '') AS cliente_nombre, " +
                "COALESCE(v.tipo, '') AS venta_tipo, " +
                "COALESCE(v.estado, '') AS venta_estado " +
                "FROM factura f " +
                "LEFT JOIN venta v ON v.id = f.venta_id " +
                "LEFT JOIN cliente c ON c.ci = v.cliente_ci " +
                "WHERE f.id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return Map(reader);
            return null;
        }

        public static async Task<int> Insert(FacturaModel f)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO factura (venta_id, subtotal, total, descuento, fecha_emision, " +
                "nombre_completo, nit, descuento_tipo) " +
                "VALUES (@ventaId, @subtotal, @total, @descuento, @fecha, @nombre, @nit, @descTipo) RETURNING id", conn);
            cmd.Parameters.AddWithValue("@ventaId", f.VentaId);
            cmd.Parameters.AddWithValue("@subtotal", (object?)f.Subtotal ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@total", (object?)f.Total ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@descuento", (object?)f.Descuento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fecha", f.FechaEmision);
            cmd.Parameters.AddWithValue("@nombre", (object?)f.NombreCompleto ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nit", (object?)f.Nit ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@descTipo", (object?)f.DescuentoTipo ?? DBNull.Value);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public static async Task Delete(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "DELETE FROM factura WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<FacturaModel>> Search(string term)
        {
            var list = new List<FacturaModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT f.id, f.venta_id, f.subtotal, f.total, f.descuento, f.fecha_emision, " +
                "f.nombre_completo, f.nit, f.descuento_tipo, " +
                "COALESCE(c.nombre || ' ' || c.apellido, '') AS cliente_nombre, " +
                "COALESCE(v.tipo, '') AS venta_tipo, " +
                "COALESCE(v.estado, '') AS venta_estado " +
                "FROM factura f " +
                "LEFT JOIN venta v ON v.id = f.venta_id " +
                "LEFT JOIN cliente c ON c.ci = v.cliente_ci " +
                "WHERE LOWER(COALESCE(c.nombre, '')) LIKE @term " +
                "   OR LOWER(COALESCE(c.apellido, '')) LIKE @term " +
                "   OR LOWER(COALESCE(f.nombre_completo, '')) LIKE @term " +
                "   OR CAST(f.venta_id AS TEXT) LIKE @term " +
                "ORDER BY f.id DESC", conn);
            cmd.Parameters.AddWithValue("@term", $"%{term.ToLower()}%");
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(Map(reader));
            return list;
        }

        // ──────────── HELPERS ────────────

        /// <summary>
        /// Checks if a factura already exists for the given venta.
        /// </summary>
        public static async Task<bool> ExistsByVentaId(int ventaId)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT COUNT(1) FROM factura WHERE venta_id = @ventaId", conn);
            cmd.Parameters.AddWithValue("@ventaId", ventaId);
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        /// <summary>
        /// Gets the factura for a specific venta (if it exists).
        /// </summary>
        public static async Task<FacturaModel?> GetByVentaId(int ventaId)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT f.id, f.venta_id, f.subtotal, f.total, f.descuento, f.fecha_emision, " +
                "f.nombre_completo, f.nit, f.descuento_tipo, " +
                "COALESCE(c.nombre || ' ' || c.apellido, '') AS cliente_nombre, " +
                "COALESCE(v.tipo, '') AS venta_tipo, " +
                "COALESCE(v.estado, '') AS venta_estado " +
                "FROM factura f " +
                "LEFT JOIN venta v ON v.id = f.venta_id " +
                "LEFT JOIN cliente c ON c.ci = v.cliente_ci " +
                "WHERE f.venta_id = @ventaId", conn);
            cmd.Parameters.AddWithValue("@ventaId", ventaId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return Map(reader);
            return null;
        }

        private static FacturaModel Map(NpgsqlDataReader r)
        {
            return new FacturaModel
            {
                Id = r.GetInt32(0),
                VentaId = r.GetInt32(1),
                Subtotal = r.IsDBNull(2) ? null : r.GetDecimal(2),
                Total = r.IsDBNull(3) ? null : r.GetDecimal(3),
                Descuento = r.IsDBNull(4) ? null : r.GetDecimal(4),
                FechaEmision = r.GetDateTime(5),
                NombreCompleto = r.IsDBNull(6) ? null : r.GetString(6),
                Nit = r.IsDBNull(7) ? null : r.GetString(7),
                DescuentoTipo = r.IsDBNull(8) ? null : r.GetString(8),
                ClienteNombre = r.IsDBNull(9) ? null : r.GetString(9),
                VentaTipo = r.IsDBNull(10) ? null : r.GetString(10),
                VentaEstado = r.IsDBNull(11) ? null : r.GetString(11),
            };
        }
    }
}
