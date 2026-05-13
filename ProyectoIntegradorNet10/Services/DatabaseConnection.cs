using System;
using Npgsql;

namespace ProyectoIntegradorNet10.Services
{
    static class DatabaseConnection
    {
        // Npgsql requires key-value format, not URI format.
        // Original URI: postgresql://postgres.kipxcnfckvulzsjukbws:fioawenfioaw213@aws-1-us-east-2.pooler.supabase.com:5432/postgres
        static string connectionString = "Host=aws-1-us-east-2.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.kipxcnfckvulzsjukbws;Password=fioawenfioaw213;SSL Mode=Require;Trust Server Certificate=true";

        // Lazy singleton DataSource for connection pooling
        private static readonly Lazy<NpgsqlDataSource> _dataSource = new(() =>
            new NpgsqlDataSourceBuilder(connectionString).Build());

        public static NpgsqlDataSource DataSource => _dataSource.Value;

        static private void setConnectionString(string newConnectionString)
        {
            connectionString = newConnectionString;
        }

        static public string GetConnectionString()
        {
            return connectionString;
        }

        static public bool testConnection()
        {
            try
            {
                using var conn = DataSource.OpenConnection();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
