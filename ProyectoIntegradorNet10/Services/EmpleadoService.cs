using System;
using System.Collections.Generic;
using Npgsql;
using ProyectoIntegradorNet10.Models;

namespace ProyectoIntegradorNet10.Services
{
    public static class EmpleadoService
    {
        private static NpgsqlDataSource DS => DatabaseConnection.DataSource;

        // ────────────────────────────── EMPLEADOS ──────────────────────────────

        public static List<EmpleadoModel> GetAllEmpleados()
        {
            var list = new List<EmpleadoModel>();
            using var conn = DS.OpenConnection();
            using var cmd = new NpgsqlCommand(
                "SELECT ci, nombre, apellido, direccion, correo, area, telefono, " +
                "usuario, contrasena, url, turno FROM empleado ORDER BY nombre, apellido", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapEmpleado(reader));
            }
            return list;
        }

        public static EmpleadoModel? GetEmpleadoByCi(string ci)
        {
            using var conn = DS.OpenConnection();
            using var cmd = new NpgsqlCommand(
                "SELECT ci, nombre, apellido, direccion, correo, area, telefono, " +
                "usuario, contrasena, url, turno FROM empleado WHERE ci = @ci", conn);
            cmd.Parameters.AddWithValue("@ci", ci);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return MapEmpleado(reader);
            return null;
        }

        public static void InsertEmpleado(EmpleadoModel emp)
        {
            using var conn = DS.OpenConnection();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO empleado (ci, nombre, apellido, direccion, correo, area, telefono, " +
                "usuario, contrasena, url, turno) " +
                "VALUES (@ci, @nombre, @apellido, @direccion, @correo, @area, @telefono, " +
                "@usuario, @contrasena, @url, @turno)", conn);
            AddParameters(cmd, emp);
            cmd.ExecuteNonQuery();
        }

        public static void UpdateEmpleado(EmpleadoModel emp)
        {
            using var conn = DS.OpenConnection();
            using var cmd = new NpgsqlCommand(
                "UPDATE empleado SET nombre = @nombre, apellido = @apellido, " +
                "direccion = @direccion, correo = @correo, area = @area, telefono = @telefono, " +
                "usuario = @usuario, contrasena = @contrasena, url = @url, turno = @turno " +
                "WHERE ci = @ci", conn);
            AddParameters(cmd, emp);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteEmpleado(string ci)
        {
            using var conn = DS.OpenConnection();

            // Delete related empleado_rol first
            using (var delRoles = new NpgsqlCommand(
                "DELETE FROM empleado_rol WHERE empleado_ci = @ci", conn))
            {
                delRoles.Parameters.AddWithValue("@ci", ci);
                delRoles.ExecuteNonQuery();
            }

            // Delete repartidor if exists
            using (var delRepartidor = new NpgsqlCommand(
                "DELETE FROM repartidor WHERE empleado_ci = @ci", conn))
            {
                delRepartidor.Parameters.AddWithValue("@ci", ci);
                delRepartidor.ExecuteNonQuery();
            }

            // Delete the employee
            using var cmd = new NpgsqlCommand(
                "DELETE FROM empleado WHERE ci = @ci", conn);
            cmd.Parameters.AddWithValue("@ci", ci);
            cmd.ExecuteNonQuery();
        }

        public static List<EmpleadoModel> SearchEmpleados(string term)
        {
            var list = new List<EmpleadoModel>();
            using var conn = DS.OpenConnection();
            using var cmd = new NpgsqlCommand(
                "SELECT ci, nombre, apellido, direccion, correo, area, telefono, " +
                "usuario, contrasena, url, turno FROM empleado " +
                "WHERE LOWER(nombre) LIKE @term OR LOWER(apellido) LIKE @term " +
                "OR LOWER(ci) LIKE @term OR LOWER(usuario) LIKE @term " +
                "ORDER BY nombre, apellido", conn);
            cmd.Parameters.AddWithValue("@term", $"%{term.ToLowerInvariant()}%");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapEmpleado(reader));
            }
            return list;
        }

        // ────────────────────────────── ROLES ──────────────────────────────

        public static List<RolModel> GetAllRoles()
        {
            var list = new List<RolModel>();
            using var conn = DS.OpenConnection();
            using var cmd = new NpgsqlCommand(
                "SELECT id, nombre, descripcion FROM rol ORDER BY nombre", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
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

        public static List<EmpleadoRolModel> GetRolesByEmpleado(string ci)
        {
            var list = new List<EmpleadoRolModel>();
            using var conn = DS.OpenConnection();
            using var cmd = new NpgsqlCommand(
                "SELECT er.empleado_ci, er.rol_id, er.estado, er.fecha_hora_asigacion, " +
                "er.fecha_hora_fin, r.nombre " +
                "FROM empleado_rol er " +
                "JOIN rol r ON r.id = er.rol_id " +
                "WHERE er.empleado_ci = @ci " +
                "ORDER BY r.nombre", conn);
            cmd.Parameters.AddWithValue("@ci", ci);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new EmpleadoRolModel
                {
                    EmpleadoCi = reader.GetString(0),
                    RolId = reader.GetInt32(1),
                    Estado = reader.IsDBNull(2) ? null : reader.GetString(2),
                    FechaHoraAsignacion = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    FechaHoraFin = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    RolNombre = reader.GetString(5)
                });
            }
            return list;
        }

        public static void AssignRoleToEmpleado(string ci, int rolId)
        {
            using var conn = DS.OpenConnection();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO empleado_rol (empleado_ci, rol_id, estado, fecha_hora_asigacion) " +
                "VALUES (@ci, @rolId, 'Activo', @now) " +
                "ON CONFLICT (empleado_ci, rol_id) DO UPDATE SET " +
                "estado = 'Activo', fecha_hora_fin = NULL, fecha_hora_asigacion = @now", conn);
            cmd.Parameters.AddWithValue("@ci", ci);
            cmd.Parameters.AddWithValue("@rolId", rolId);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            cmd.ExecuteNonQuery();
        }

        public static void RemoveRoleFromEmpleado(string ci, int rolId)
        {
            using var conn = DS.OpenConnection();
            using var cmd = new NpgsqlCommand(
                "UPDATE empleado_rol SET estado = 'Inactivo', fecha_hora_fin = @now " +
                "WHERE empleado_ci = @ci AND rol_id = @rolId", conn);
            cmd.Parameters.AddWithValue("@ci", ci);
            cmd.Parameters.AddWithValue("@rolId", rolId);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            cmd.ExecuteNonQuery();
        }

        // ────────────────────────────── HELPERS ──────────────────────────────

        private static EmpleadoModel MapEmpleado(NpgsqlDataReader reader)
        {
            return new EmpleadoModel
            {
                Ci = reader.GetString(0),
                Nombre = reader.GetString(1),
                Apellido = reader.GetString(2),
                Direccion = reader.IsDBNull(3) ? null : reader.GetString(3),
                Correo = reader.GetString(4),
                Area = reader.IsDBNull(5) ? null : reader.GetString(5),
                Telefono = reader.IsDBNull(6) ? null : reader.GetString(6),
                Usuario = reader.GetString(7),
                Contrasena = reader.GetString(8),
                Url = reader.IsDBNull(9) ? null : reader.GetString(9),
                Turno = reader.IsDBNull(10) ? null : reader.GetString(10)
            };
        }

        private static void AddParameters(NpgsqlCommand cmd, EmpleadoModel emp)
        {
            cmd.Parameters.AddWithValue("@ci", emp.Ci);
            cmd.Parameters.AddWithValue("@nombre", emp.Nombre);
            cmd.Parameters.AddWithValue("@apellido", emp.Apellido);
            cmd.Parameters.AddWithValue("@direccion", (object?)emp.Direccion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@correo", emp.Correo);
            cmd.Parameters.AddWithValue("@area", (object?)emp.Area ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@telefono", (object?)emp.Telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@usuario", emp.Usuario);
            cmd.Parameters.AddWithValue("@contrasena", emp.Contrasena);
            cmd.Parameters.AddWithValue("@url", (object?)emp.Url ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@turno", (object?)emp.Turno ?? DBNull.Value);
        }
    }
}
