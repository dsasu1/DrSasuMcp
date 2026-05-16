using Npgsql;

namespace DrSasuMcp.PostgreSQL.PostgreSQL
{
    public class PostgreSqlConnectionFactory : IPostgreSqlConnectionFactory
    {
        public async Task<NpgsqlConnection> GetOpenConnectionAsync()
        {
            var connectionString = GetConnectionString();
            var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            return conn;
        }

        private static string GetConnectionString()
        {
            var connectionString = Environment.GetEnvironmentVariable(PostgreSQLToolConstants.ConnectionStringEnvVar);

            return string.IsNullOrEmpty(connectionString)
                ? throw new InvalidOperationException(
                    $"Connection string is not set in the environment variable '{PostgreSQLToolConstants.ConnectionStringEnvVar}'.\n\n" +
                    "HINT: Set POSTGRES_CONNECTION_STRING, e.g. " +
                    "Host=localhost;Database=test;Username=postgres;Password=yourpassword")
                : connectionString;
        }
    }
}
