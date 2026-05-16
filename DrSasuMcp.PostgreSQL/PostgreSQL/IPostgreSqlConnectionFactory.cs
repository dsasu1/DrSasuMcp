using Npgsql;

namespace DrSasuMcp.PostgreSQL.PostgreSQL
{
    public interface IPostgreSqlConnectionFactory
    {
        Task<NpgsqlConnection> GetOpenConnectionAsync();
    }
}
