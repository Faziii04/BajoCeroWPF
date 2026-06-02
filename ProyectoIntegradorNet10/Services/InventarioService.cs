using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class InventarioService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        public static async Task<List<InventarioModel>> GetAll()
        {
            var list = new List<InventarioModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                @"SELECT i.producto_id, i.deposito_id, i.cantidad,
                         p.nombre AS producto_nombre,
                         d.nombre AS deposito_nombre
                  FROM producto_deposito i
                  JOIN producto p ON p.id = i.producto_id
                  JOIN deposito d ON d.id = i.deposito_id
                  ORDER BY p.nombre, d.nombre", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }
            return list;
        }

        public static async Task<List<InventarioModel>> GetByProducto(int productoId)
        {
            var list = new List<InventarioModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                @"SELECT i.producto_id, i.deposito_id, i.cantidad,
                         p.nombre AS producto_nombre,
                         d.nombre AS deposito_nombre
                  FROM producto_deposito i
                  JOIN producto p ON p.id = i.producto_id
                  JOIN deposito d ON d.id = i.deposito_id
                  WHERE i.producto_id = @producto_id
                  ORDER BY d.nombre", conn);
            cmd.Parameters.AddWithValue("@producto_id", productoId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }
            return list;
        }

        public static async Task<InventarioModel?> GetByProductoAndDeposito(int productoId, int depositoId)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                @"SELECT i.producto_id, i.deposito_id, i.cantidad,
                         p.nombre AS producto_nombre,
                         d.nombre AS deposito_nombre
                  FROM producto_deposito i
                  JOIN producto p ON p.id = i.producto_id
                  JOIN deposito d ON d.id = i.deposito_id
                  WHERE i.producto_id = @producto_id AND i.deposito_id = @deposito_id", conn);
            cmd.Parameters.AddWithValue("@producto_id", productoId);
            cmd.Parameters.AddWithValue("@deposito_id", depositoId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return Map(reader);
            return null;
        }

        /// <summary>
        /// Adds stock to a product in a deposit. If the record exists, increments cantidad.
        /// If not, inserts a new record with the given cantidad.
        /// </summary>
        public static async Task AddStock(int productoId, int depositoId, decimal cantidad)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                @"INSERT INTO producto_deposito (producto_id, deposito_id, cantidad)
                  VALUES (@producto_id, @deposito_id, @cantidad)
                  ON CONFLICT (producto_id, deposito_id)
                  DO UPDATE SET cantidad = producto_deposito.cantidad + @cantidad", conn);
            cmd.Parameters.AddWithValue("@producto_id", productoId);
            cmd.Parameters.AddWithValue("@deposito_id", depositoId);
            cmd.Parameters.AddWithValue("@cantidad", cantidad);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Sets exact stock quantity for a product in a deposit (overwrites).
        /// </summary>
        public static async Task SetStock(int productoId, int depositoId, decimal cantidad)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                @"INSERT INTO producto_deposito (producto_id, deposito_id, cantidad)
                  VALUES (@producto_id, @deposito_id, @cantidad)
                  ON CONFLICT (producto_id, deposito_id)
                  DO UPDATE SET cantidad = @cantidad", conn);
            cmd.Parameters.AddWithValue("@producto_id", productoId);
            cmd.Parameters.AddWithValue("@deposito_id", depositoId);
            cmd.Parameters.AddWithValue("@cantidad", cantidad);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Removes stock (subtracts) from a product in a deposit. Prevents negative stock.
        /// </summary>
        public static async Task RemoveStock(int productoId, int depositoId, decimal cantidad)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                @"UPDATE producto_deposito SET cantidad = GREATEST(cantidad - @cantidad, 0)
                  WHERE producto_id = @producto_id AND deposito_id = @deposito_id", conn);
            cmd.Parameters.AddWithValue("@producto_id", productoId);
            cmd.Parameters.AddWithValue("@deposito_id", depositoId);
            cmd.Parameters.AddWithValue("@cantidad", cantidad);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Delete(int productoId, int depositoId)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "DELETE FROM producto_deposito WHERE producto_id = @producto_id AND deposito_id = @deposito_id", conn);
            cmd.Parameters.AddWithValue("@producto_id", productoId);
            cmd.Parameters.AddWithValue("@deposito_id", depositoId);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<InventarioModel>> Search(string term)
        {
            var list = new List<InventarioModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                @"SELECT i.producto_id, i.deposito_id, i.cantidad,
                         p.nombre AS producto_nombre,
                         d.nombre AS deposito_nombre
                  FROM producto_deposito i
                  JOIN producto p ON p.id = i.producto_id
                  JOIN deposito d ON d.id = i.deposito_id
                  WHERE LOWER(p.nombre) LIKE @term OR LOWER(d.nombre) LIKE @term
                  ORDER BY p.nombre, d.nombre", conn);
            cmd.Parameters.AddWithValue("@term", $"%{term.ToLower()}%");
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }
            return list;
        }

        public static async Task<List<ProductoModel>> GetAllProductos()
        {
            var list = new List<ProductoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, nombre, precio_venta, estado FROM producto WHERE estado = 'Activo' ORDER BY nombre", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ProductoModel
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    PrecioVenta = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                    Estado = reader.IsDBNull(3) ? null : reader.GetString(3),
                });
            }
            return list;
        }

        public static async Task<List<DepositoModel>> GetAllDepositos()
        {
            var list = new List<DepositoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, nombre, direccion FROM deposito ORDER BY nombre", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DepositoModel
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Direccion = reader.IsDBNull(2) ? null : reader.GetString(2),
                });
            }
            return list;
        }

        private static InventarioModel Map(NpgsqlDataReader r)
        {
            return new InventarioModel
            {
                ProductoId = r.GetInt32(0),
                DepositoId = r.GetInt32(1),
                Cantidad = r.IsDBNull(2) ? null : r.GetDecimal(2),
                ProductoNombre = r.IsDBNull(3) ? string.Empty : r.GetString(3),
                DepositoNombre = r.IsDBNull(4) ? string.Empty : r.GetString(4),
            };
        }
    }

    public class DepositoModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Direccion { get; set; }
    }
}
