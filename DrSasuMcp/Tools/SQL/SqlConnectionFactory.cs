using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.SQL
{
    public class SqlConnectionFactory: ISqlConnectionFactory
    {
        public async Task<SqlConnection> GetOpenConnectionAsync()
        {
            var connectionString = GetConnectionString();

            // Let ADO.Net handle connection pooling
            var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            return conn;
        }

        private static string GetConnectionString()
        {
            var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");

            return string.IsNullOrEmpty(connectionString)
                ? throw new InvalidOperationException("Connection string is not set in the environment variable 'SQL_CONNECTION_STRING'.\n\nHINT: Have a local SQL Server, with a database called 'test', from console, run `SET SQL_CONNECTION_STRING=Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True` and the load the .sln file")
                : connectionString;
        }
    }
}
