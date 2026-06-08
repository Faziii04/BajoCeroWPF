using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class RepartidorService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        // ────────────────────────── REPARTIDORES ──────────────────────────

        /// <summary>
        /// Returns all repartidores with the employee name joined in.
        /// </summary>
        public static async Task<List<RepartidorModel>> GetAll()
        {
            var list = new List<RepartidorModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT r.id, r.estado, r.zona, r.licencia, r.empleado_ci, " +
                "COALESCE(e.nombre || ' ' || e.apellido, '—') AS empleado_nombre " +
                "FROM repartidor r " +
                "LEFT JOIN empleado e ON e.ci = r.empleado_ci " +
                "ORDER BY e.nombre, e.apellido", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(MapRepartidor(reader));
            return list;
        }

        /// <summary>
        /// Returns only repartidores with estado = 'Activo' (available for assignment).
        /// </summary>
        public static async Task<List<RepartidorModel>> GetActivos()
        {
            var list = new List<RepartidorModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT r.id, r.estado, r.zona, r.licencia, r.empleado_ci, " +
                "COALESCE(e.nombre || ' ' || e.apellido, '—') AS empleado_nombre " +
                "FROM repartidor r " +
                "LEFT JOIN empleado e ON e.ci = r.empleado_ci " +
                "WHERE r.estado = 'Activo' " +
                "ORDER BY e.nombre, e.apellido", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(MapRepartidor(reader));
            return list;
        }

        public static async Task<RepartidorModel?> GetById(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT r.id, r.estado, r.zona, r.licencia, r.empleado_ci, " +
                "COALESCE(e.nombre || ' ' || e.apellido, '—') AS empleado_nombre " +
                "FROM repartidor r " +
                "LEFT JOIN empleado e ON e.ci = r.empleado_ci " +
                "WHERE r.id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return MapRepartidor(reader);
            return null;
        }

        public static async Task Insert(RepartidorModel r)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO repartidor (estado, zona, licencia, empleado_ci) " +
                "VALUES (@estado, @zona, @licencia, @empleado_ci)", conn);
            AddRepartidorParams(cmd, r);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Update(RepartidorModel r)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE repartidor SET estado = @estado, zona = @zona, " +
                "licencia = @licencia, empleado_ci = @empleado_ci WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", r.Id);
            AddRepartidorParams(cmd, r);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Delete(int id)
        {
            using var conn = await DS.OpenConnectionAsync();

            // Delete related assignments first
            using (var delAssign = new NpgsqlCommand(
                "DELETE FROM repartidor_vehiculo WHERE repartidor_id = @id", conn))
            {
                delAssign.Parameters.AddWithValue("@id", id);
                await delAssign.ExecuteNonQueryAsync();
            }

            using var cmd = new NpgsqlCommand(
                "DELETE FROM repartidor WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        // ────────────────────── REPARTIDOR_VEHICULO (Assignments) ──────────────────────

        /// <summary>
        /// Returns the active assignment for a given vehicle, or null if none.
        /// </summary>
        public static async Task<RepartidorVehiculoModel?> GetActiveAssignmentByPlaca(string placa)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT rv.repartidor_id, rv.vehiculo_placa, rv.estado, " +
                "rv.fecha_hora_asigacion, rv.fecha_hora_fin, " +
                "COALESCE(e.nombre || ' ' || e.apellido, '—') AS repartidor_nombre, " +
                "r.licencia AS repartidor_licencia, r.zona AS repartidor_zona " +
                "FROM repartidor_vehiculo rv " +
                "JOIN repartidor r ON r.id = rv.repartidor_id " +
                "LEFT JOIN empleado e ON e.ci = r.empleado_ci " +
                "WHERE rv.vehiculo_placa = @placa AND rv.estado = 'Activo' " +
                "ORDER BY rv.fecha_hora_asigacion DESC LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@placa", placa);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return MapAssignment(reader);
            return null;
        }

        /// <summary>
        /// Assigns a repartidor to a vehicle. If the vehicle already has an active
        /// assignment, it sets that one to 'Inactivo' first (and sets that repartidor
        /// back to 'Activo'), then inserts the new one.
        /// </summary>
        public static async Task AssignRepartidorToVehiculo(int repartidorId, string placa)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1. Find & deactivate any existing active assignment for this vehicle
                //    Also free the previously assigned repartidor
                using (var getOld = new NpgsqlCommand(
                    "SELECT repartidor_id FROM repartidor_vehiculo " +
                    "WHERE vehiculo_placa = @placa AND estado = 'Activo'", conn))
                {
                    getOld.Parameters.AddWithValue("@placa", placa);
                    var oldId = await getOld.ExecuteScalarAsync();
                    if (oldId != null)
                    {
                        int oldRepartidorId = Convert.ToInt32(oldId);

                        // Deactivate old assignment
                        using (var deactivate = new NpgsqlCommand(
                            "UPDATE repartidor_vehiculo SET estado = 'Inactivo', fecha_hora_fin = @now " +
                            "WHERE vehiculo_placa = @placa AND estado = 'Activo'", conn))
                        {
                            deactivate.Parameters.AddWithValue("@placa", placa);
                            deactivate.Parameters.AddWithValue("@now", DateTime.Now);
                            await deactivate.ExecuteNonQueryAsync();
                        }

                        // Free the previous repartidor
                        using (var freeOld = new NpgsqlCommand(
                            "UPDATE repartidor SET estado = 'Activo' WHERE id = @id", conn))
                        {
                            freeOld.Parameters.AddWithValue("@id", oldRepartidorId);
                            await freeOld.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 2. Insert the new assignment
                using (var insert = new NpgsqlCommand(
                    "INSERT INTO repartidor_vehiculo (repartidor_id, vehiculo_placa, estado, fecha_hora_asigacion) " +
                    "VALUES (@repartidorId, @placa, 'Activo', @now)", conn))
                {
                    insert.Parameters.AddWithValue("@repartidorId", repartidorId);
                    insert.Parameters.AddWithValue("@placa", placa);
                    insert.Parameters.AddWithValue("@now", DateTime.Now);
                    await insert.ExecuteNonQueryAsync();
                }

                // 3. Set new repartidor estado to 'Ocupado'
                using (var updateRep = new NpgsqlCommand(
                    "UPDATE repartidor SET estado = 'Ocupado' WHERE id = @id", conn))
                {
                    updateRep.Parameters.AddWithValue("@id", repartidorId);
                    await updateRep.ExecuteNonQueryAsync();
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
        /// Removes (deactivates) the active assignment for a vehicle and sets the
        /// repartidor back to 'Activo'.
        /// </summary>
        public static async Task RemoveAssignment(string placa)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Get current active repartidor
                int? repartidorId = null;
                using (var getRep = new NpgsqlCommand(
                    "SELECT repartidor_id FROM repartidor_vehiculo " +
                    "WHERE vehiculo_placa = @placa AND estado = 'Activo'", conn))
                {
                    getRep.Parameters.AddWithValue("@placa", placa);
                    var result = await getRep.ExecuteScalarAsync();
                    if (result != null)
                        repartidorId = Convert.ToInt32(result);
                }

                // Deactivate the assignment
                using (var deactivate = new NpgsqlCommand(
                    "UPDATE repartidor_vehiculo SET estado = 'Inactivo', fecha_hora_fin = @now " +
                    "WHERE vehiculo_placa = @placa AND estado = 'Activo'", conn))
                {
                    deactivate.Parameters.AddWithValue("@placa", placa);
                    deactivate.Parameters.AddWithValue("@now", DateTime.Now);
                    await deactivate.ExecuteNonQueryAsync();
                }

                // Set repartidor back to 'Activo'
                if (repartidorId.HasValue)
                {
                    using (var updateRep = new NpgsqlCommand(
                        "UPDATE repartidor SET estado = 'Activo' WHERE id = @id", conn))
                    {
                        updateRep.Parameters.AddWithValue("@id", repartidorId.Value);
                        await updateRep.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ────────────────────────── HELPERS ──────────────────────────

        private static RepartidorModel MapRepartidor(NpgsqlDataReader r)
        {
            return new RepartidorModel
            {
                Id = r.GetInt32(0),
                Estado = r.IsDBNull(1) ? null : r.GetString(1),
                Zona = r.IsDBNull(2) ? null : r.GetString(2),
                Licencia = r.IsDBNull(3) ? null : r.GetString(3),
                EmpleadoCi = r.IsDBNull(4) ? null : r.GetString(4),
                EmpleadoNombre = r.GetString(5)
            };
        }

        private static RepartidorVehiculoModel MapAssignment(NpgsqlDataReader r)
        {
            return new RepartidorVehiculoModel
            {
                RepartidorId = r.GetInt32(0),
                VehiculoPlaca = r.GetString(1),
                Estado = r.IsDBNull(2) ? null : r.GetString(2),
                FechaHoraAsignacion = r.IsDBNull(3) ? null : r.GetDateTime(3),
                FechaHoraFin = r.IsDBNull(4) ? null : r.GetDateTime(4),
                RepartidorNombre = r.GetString(5),
                RepartidorLicencia = r.IsDBNull(6) ? null : r.GetString(6),
                RepartidorZona = r.IsDBNull(7) ? null : r.GetString(7)
            };
        }

        private static void AddRepartidorParams(NpgsqlCommand cmd, RepartidorModel r)
        {
            cmd.Parameters.AddWithValue("@estado", (object?)r.Estado ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@zona", (object?)r.Zona ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@licencia", (object?)r.Licencia ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@empleado_ci", (object?)r.EmpleadoCi ?? DBNull.Value);
        }
    }
}
