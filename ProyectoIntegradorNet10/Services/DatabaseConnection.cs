using System;
using System.Collections.Generic;
using System.Text;

namespace ProyectoIntegradorNet10.Services
{
    static class DatabaseConnection
    {
        static string connectionString = "postgresql://postgres.kipxcnfckvulzsjukbws:fioawenfioaw213@aws-1-us-east-2.pooler.supabase.com:5432/postgres";

        static private void setConnectionString(string newConnectionString)
        {
            connectionString = newConnectionString;
        }

        static private string getConnectionString()
        {
            return connectionString;
        }

    }
}
