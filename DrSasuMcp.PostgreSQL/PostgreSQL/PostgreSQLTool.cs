using DrSasuMcp.Common.Models;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Npgsql;
using System.ComponentModel;

namespace DrSasuMcp.PostgreSQL.PostgreSQL
{
    [McpServerToolType]
    public partial class PostgreSQLTool(IPostgreSqlConnectionFactory connectionFactory, ILogger<PostgreSQLTool> logger)
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory = connectionFactory;
        private readonly ILogger<PostgreSQLTool> _logger = logger;

        [McpServerTool(
            Title = "Postgres: Describe Table",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Returns table schema for a PostgreSQL table")]
        public async Task<OperationResult> PostgresDescribeTable(
            [Description("Name of table, optionally schema-qualified (e.g. public.users)")] string name)
        {
            string? schema = null;
            if (name.Contains('.'))
            {
                var parts = name.Split('.');
                if (parts.Length > 1)
                {
                    schema = parts[0];
                    name = parts[1];
                }
            }

            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    var result = new Dictionary<string, object>();

                    using (var cmd = new NpgsqlCommand(TableInfoQuery, conn))
                    {
                        _ = cmd.Parameters.AddWithValue("@TableName", name);
                        _ = cmd.Parameters.AddWithValue("@TableSchema", schema ?? (object)DBNull.Value);
                        using var reader = await cmd.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            result["table"] = new
                            {
                                id = reader["id"],
                                name = reader["name"],
                                schema = reader["schema"],
                                owner = reader["owner"],
                                type = reader["type"],
                                description = reader["description"] is DBNull ? null : reader["description"]
                            };
                        }
                        else
                        {
                            return new OperationResult(success: false, error: $"Table '{name}' not found.");
                        }
                    }

                    using (var cmd = new NpgsqlCommand(ColumnsQuery, conn))
                    {
                        _ = cmd.Parameters.AddWithValue("@TableName", name);
                        _ = cmd.Parameters.AddWithValue("@TableSchema", schema ?? (object)DBNull.Value);
                        using var reader = await cmd.ExecuteReaderAsync();
                        var columns = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            columns.Add(new
                            {
                                name = reader["name"],
                                type = reader["type"],
                                length = reader["length"] is DBNull ? null : reader["length"],
                                precision = reader["precision"] is DBNull ? null : reader["precision"],
                                scale = reader["scale"] is DBNull ? null : reader["scale"],
                                nullable = reader["nullable"] is DBNull ? true : (bool)reader["nullable"],
                                description = reader["description"] is DBNull ? null : reader["description"]
                            });
                        }
                        result["columns"] = columns;
                    }

                    using (var cmd = new NpgsqlCommand(IndexesQuery, conn))
                    {
                        _ = cmd.Parameters.AddWithValue("@TableName", name);
                        _ = cmd.Parameters.AddWithValue("@TableSchema", schema ?? (object)DBNull.Value);
                        using var reader = await cmd.ExecuteReaderAsync();
                        var indexes = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            indexes.Add(new
                            {
                                name = reader["name"],
                                type = reader["type"],
                                description = reader["description"] is DBNull ? null : reader["description"],
                                keys = reader["keys"] is DBNull ? null : reader["keys"]
                            });
                        }
                        result["indexes"] = indexes;
                    }

                    using (var cmd = new NpgsqlCommand(ConstraintsQuery, conn))
                    {
                        _ = cmd.Parameters.AddWithValue("@TableName", name);
                        _ = cmd.Parameters.AddWithValue("@TableSchema", schema ?? (object)DBNull.Value);
                        using var reader = await cmd.ExecuteReaderAsync();
                        var constraints = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            constraints.Add(new
                            {
                                name = reader["name"],
                                type = reader["type"],
                                keys = reader["keys"] is DBNull ? null : reader["keys"]
                            });
                        }
                        result["constraints"] = constraints;
                    }

                    using (var cmd = new NpgsqlCommand(ForeignKeyInformation, conn))
                    {
                        _ = cmd.Parameters.AddWithValue("@TableName", name);
                        _ = cmd.Parameters.AddWithValue("@TableSchema", schema ?? (object)DBNull.Value);
                        using var reader = await cmd.ExecuteReaderAsync();
                        var foreignKeys = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            foreignKeys.Add(new
                            {
                                name = reader["name"],
                                schema = reader["schema"],
                                table_name = reader["table_name"],
                                column_name = reader["column_names"],
                                referenced_schema = reader["referenced_schema"],
                                referenced_table = reader["referenced_table"],
                                referenced_column = reader["referenced_column_names"],
                            });
                        }
                        result["foreignKeys"] = foreignKeys;
                    }

                    return new OperationResult(success: true, data: result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgresDescribeTable failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
            Title = "Postgres: Read Data",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Executes SQL queries against PostgreSQL to read data")]
        public async Task<OperationResult> PostgresReadData(
            [Description("SQL query to execute")] string sql)
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    using var cmd = new NpgsqlCommand(sql, conn);
                    using var reader = await cmd.ExecuteReaderAsync();
                    var results = new List<Dictionary<string, object?>>();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>();
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        results.Add(row);
                    }
                    return new OperationResult(success: true, data: results);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgresReadData failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
            Title = "Postgres: List Tables",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Lists all tables in the PostgreSQL database.")]
        public async Task<OperationResult> PostgresListTables()
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    using var cmd = new NpgsqlCommand(ListTablesQuery, conn);
                    var tables = new List<string>();
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        tables.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
                    }
                    return new OperationResult(success: true, data: tables);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgresListTables failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
            Title = "Postgres: Insert Data",
            ReadOnly = false,
            Destructive = false),
            Description("Inserts data into a PostgreSQL table. Expects a valid INSERT SQL statement.")]
        public async Task<OperationResult> PostgresInsertData(
            [Description("INSERT SQL statement")] string sql)
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    using var cmd = new NpgsqlCommand(sql, conn);
                    var rows = await cmd.ExecuteNonQueryAsync();
                    return new OperationResult(success: true, rowsAffected: rows);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgresInsertData failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
            Title = "Postgres: Drop Table",
            ReadOnly = false,
            Destructive = true),
            Description("Drops a table in PostgreSQL. Expects a valid DROP TABLE SQL statement.")]
        public async Task<OperationResult> PostgresDropTable(
            [Description("DROP TABLE SQL statement")] string sql)
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    using var cmd = new NpgsqlCommand(sql, conn);
                    _ = await cmd.ExecuteNonQueryAsync();
                    return new OperationResult(success: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgresDropTable failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
            Title = "Postgres: Create Table",
            ReadOnly = false,
            Destructive = false),
            Description("Creates a new table in PostgreSQL. Expects a valid CREATE TABLE SQL statement.")]
        public async Task<OperationResult> PostgresCreateTable(
            [Description("CREATE TABLE SQL statement")] string sql)
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    using var cmd = new NpgsqlCommand(sql, conn);
                    _ = await cmd.ExecuteNonQueryAsync();
                    return new OperationResult(success: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgresCreateTable failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
            Title = "Postgres: Update Data",
            ReadOnly = false,
            Destructive = true),
            Description("Updates data in a PostgreSQL table. Expects a valid UPDATE SQL statement.")]
        public async Task<OperationResult> PostgresUpdateData(
            [Description("UPDATE SQL statement")] string sql)
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    using var cmd = new NpgsqlCommand(sql, conn);
                    var rows = await cmd.ExecuteNonQueryAsync();
                    return new OperationResult(true, null, rows);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgresUpdateData failed: {Message}", ex.Message);
                return new OperationResult(false, ex.Message);
            }
        }
    }
}
