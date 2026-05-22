using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class RolesPermisosService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        // ════════════════════════════════════════════════════════════════
        //  ROLES
        // ════════════════════════════════════════════════════════════════

        public static async Task<List<RolModel>> GetAllRoles()
        {
            var list = new List<RolModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, nombre, descripcion FROM rol ORDER BY nombre", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new RolModel
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
            return list;
        }

        public static async Task<RolModel?> GetRolById(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, nombre, descripcion FROM rol WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new RolModel
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2)
                };
            }
            return null;
        }

        public static async Task InsertRol(RolModel rol)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO rol (nombre, descripcion) VALUES (@nombre, @descripcion)", conn);
            cmd.Parameters.AddWithValue("@nombre", rol.Nombre);
            cmd.Parameters.AddWithValue("@descripcion", (object?)rol.Descripcion ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task UpdateRol(RolModel rol)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE rol SET nombre = @nombre, descripcion = @descripcion WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", rol.Id);
            cmd.Parameters.AddWithValue("@nombre", rol.Nombre);
            cmd.Parameters.AddWithValue("@descripcion", (object?)rol.Descripcion ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task DeleteRol(int id)
        {
            using var conn = await DS.OpenConnectionAsync();

            // Delete related empleado_rol records first
            using (var delEmpRol = new NpgsqlCommand(
                "DELETE FROM empleado_rol WHERE rol_id = @id", conn))
            {
                delEmpRol.Parameters.AddWithValue("@id", id);
                await delEmpRol.ExecuteNonQueryAsync();
            }

            // Delete related rol_permisos records
            using (var delRolPerm = new NpgsqlCommand(
                "DELETE FROM rol_permisos WHERE rol_id = @id", conn))
            {
                delRolPerm.Parameters.AddWithValue("@id", id);
                await delRolPerm.ExecuteNonQueryAsync();
            }

            // Delete the role itself
            using var cmd = new NpgsqlCommand(
                "DELETE FROM rol WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<RolModel>> SearchRoles(string term)
        {
            var list = new List<RolModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, nombre, descripcion FROM rol " +
                "WHERE LOWER(nombre) LIKE @term OR LOWER(descripcion) LIKE @term " +
                "ORDER BY nombre", conn);
            cmd.Parameters.AddWithValue("@term", $"%{term.ToLowerInvariant()}%");
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new RolModel
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  PERMISOS
        // ════════════════════════════════════════════════════════════════

        public static async Task<List<PermisoModel>> GetAllPermisos()
        {
            var list = new List<PermisoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, permiso, descripcion FROM permiso ORDER BY permiso", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new PermisoModel
                {
                    Id = reader.GetInt32(0),
                    Permiso = reader.GetString(1),
                    Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
            return list;
        }

        public static async Task<PermisoModel?> GetPermisoById(int id)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, permiso, descripcion FROM permiso WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new PermisoModel
                {
                    Id = reader.GetInt32(0),
                    Permiso = reader.GetString(1),
                    Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2)
                };
            }
            return null;
        }

        public static async Task InsertPermiso(PermisoModel permiso)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO permiso (permiso, descripcion) VALUES (@permiso, @descripcion)", conn);
            cmd.Parameters.AddWithValue("@permiso", permiso.Permiso);
            cmd.Parameters.AddWithValue("@descripcion", (object?)permiso.Descripcion ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task UpdatePermiso(PermisoModel permiso)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE permiso SET permiso = @permiso, descripcion = @descripcion WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", permiso.Id);
            cmd.Parameters.AddWithValue("@permiso", permiso.Permiso);
            cmd.Parameters.AddWithValue("@descripcion", (object?)permiso.Descripcion ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task DeletePermiso(int id)
        {
            using var conn = await DS.OpenConnectionAsync();

            // Delete related rol_permisos records first
            using (var delRolPerm = new NpgsqlCommand(
                "DELETE FROM rol_permisos WHERE permiso_id = @id", conn))
            {
                delRolPerm.Parameters.AddWithValue("@id", id);
                await delRolPerm.ExecuteNonQueryAsync();
            }

            // Delete the permission itself
            using var cmd = new NpgsqlCommand(
                "DELETE FROM permiso WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<PermisoModel>> SearchPermisos(string term)
        {
            var list = new List<PermisoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, permiso, descripcion FROM permiso " +
                "WHERE LOWER(permiso) LIKE @term OR LOWER(descripcion) LIKE @term " +
                "ORDER BY permiso", conn);
            cmd.Parameters.AddWithValue("@term", $"%{term.ToLowerInvariant()}%");
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new PermisoModel
                {
                    Id = reader.GetInt32(0),
                    Permiso = reader.GetString(1),
                    Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  ROL ↔ PERMISO ASSIGNMENT
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns all permissions assigned to a specific role (active ones).
        /// </summary>
        public static async Task<List<RolPermisoModel>> GetPermisosByRol(int rolId)
        {
            var list = new List<RolPermisoModel>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT rp.rol_id, rp.permiso_id, rp.estado, rp.fecha_asigacion, " +
                "rp.fecha_fin, p.permiso " +
                "FROM rol_permisos rp " +
                "JOIN permiso p ON p.id = rp.permiso_id " +
                "WHERE rp.rol_id = @rolId " +
                "ORDER BY p.permiso", conn);
            cmd.Parameters.AddWithValue("@rolId", rolId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new RolPermisoModel
                {
                    RolId = reader.GetInt32(0),
                    PermisoId = reader.GetInt32(1),
                    Estado = reader.IsDBNull(2) ? null : reader.GetString(2),
                    FechaAsignacion = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    FechaFin = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    PermisoNombre = reader.GetString(5)
                });
            }
            return list;
        }

        /// <summary>
        /// Assigns a permission to a role (sets estado = 'Activo').
        /// Uses upsert logic: if already exists, reactivates it.
        /// </summary>
        public static async Task AssignPermisoToRol(int rolId, int permisoId)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO rol_permisos (rol_id, permiso_id, estado, fecha_asigacion) " +
                "VALUES (@rolId, @permisoId, 'Activo', @now) " +
                "ON CONFLICT (rol_id, permiso_id) DO UPDATE SET " +
                "estado = 'Activo', fecha_fin = NULL, fecha_asigacion = @now", conn);
            cmd.Parameters.AddWithValue("@rolId", rolId);
            cmd.Parameters.AddWithValue("@permisoId", permisoId);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Removes a permission from a role (sets estado = 'Inactivo' and fecha_fin).
        /// </summary>
        public static async Task RemovePermisoFromRol(int rolId, int permisoId)
        {
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE rol_permisos SET estado = 'Inactivo', fecha_fin = @now " +
                "WHERE rol_id = @rolId AND permiso_id = @permisoId", conn);
            cmd.Parameters.AddWithValue("@rolId", rolId);
            cmd.Parameters.AddWithValue("@permisoId", permisoId);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            await cmd.ExecuteNonQueryAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //  EMPLOYEE → PERMISSIONS (for nav visibility)
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns all active permission names for a given employee CI,
        /// gathered from all active roles assigned to that employee.
        /// </summary>
        public static async Task<HashSet<string>> GetPermisoNombresByEmpleadoCi(string ci)
        {
            var result = new HashSet<string>();
            using var conn = await DS.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT DISTINCT p.permiso " +
                "FROM empleado_rol er " +
                "JOIN rol_permisos rp ON rp.rol_id = er.rol_id " +
                "JOIN permiso p ON p.id = rp.permiso_id " +
                "WHERE er.empleado_ci = @ci " +
                "  AND er.estado = 'Activo' " +
                "  AND rp.estado = 'Activo' " +
                "ORDER BY p.permiso", conn);
            cmd.Parameters.AddWithValue("@ci", ci);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }
    }
}
