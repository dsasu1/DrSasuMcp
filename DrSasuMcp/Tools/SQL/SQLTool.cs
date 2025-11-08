using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.SQL
{
    [McpServerToolType]
    public  partial class SQLTool(ISqlConnectionFactory connectionFactory, ILogger<SQLTool> logger)
    {
        private readonly ISqlConnectionFactory _connectionFactory = connectionFactory;
        private readonly ILogger<SQLTool> _logger = logger;

        #region Public Methods

        [McpServerTool(
       Title = "SQL: Describe Table",
       ReadOnly = true,
       Idempotent = true,
       Destructive = false),
       Description("Returns table schema")]
        public async Task<OperationResult> SQLDescribeTable(
       [Description("Name of table")] string name)
        {
            string? schema = null;
            if (name.Contains('.'))
            {
                // If the table name contains a schema, split it into schema and table name
                var parts = name.Split('.');
                if (parts.Length > 1)
                {
                    name = parts[1]; // Use only the table name part
                    schema = parts[0]; // Use the first part as schema  
                }
            }
           
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    var result = new Dictionary<string, object>();
                    // Table info
                    using (var cmd = new SqlCommand(TableInfoQuery, conn))
                    {
                        var _ = cmd.Parameters.AddWithValue("@TableName", name);
                        _ = cmd.Parameters.AddWithValue("@TableSchema", schema == null ? DBNull.Value : schema);
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
                    // Columns
                    using (var cmd = new SqlCommand(ColumnsQuery, conn))
                    {
                        var _ = cmd.Parameters.AddWithValue("@TableName", name);
                        _ = cmd.Parameters.AddWithValue("@TableSchema", schema == null ? DBNull.Value : schema);
                        using var reader = await cmd.ExecuteReaderAsync();
                        var columns = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            columns.Add(new
                            {
                                name = reader["name"],
                                type = reader["type"],
                                length = reader["length"],
                                precision = reader["precision"],
                                scale = reader["scale"],
                                nullable = (bool)reader["nullable"],
                                description = reader["description"] is DBNull ? null : reader["description"]
                            });
                        }
                        result["columns"] = columns;
                    }
                    // Indexes
                    using (var cmd = new SqlCommand(IndexesQuery, conn))
                    {
                        var _ = cmd.Parameters.AddWithValue("@TableName", name);
                        _ = cmd.Parameters.AddWithValue("@TableSchema", schema == null ? DBNull.Value : schema);
                        using var reader = await cmd.ExecuteReaderAsync();
                        var indexes = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            indexes.Add(new
                            {
                                name = reader["name"],
                                type = reader["type"],
                                description = reader["description"] is DBNull ? null : reader["description"],
                                keys = reader["keys"]
                            });
                        }
                        result["indexes"] = indexes;
                    }
                    // Constraints
                    using (var cmd = new SqlCommand(ConstraintsQuery, conn))
                    {
                        var _ = cmd.Parameters.AddWithValue("@TableName", name);
                        _ = cmd.Parameters.AddWithValue("@TableSchema", schema == null ? DBNull.Value : schema);
                        using var reader = await cmd.ExecuteReaderAsync();
                        var constraints = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            constraints.Add(new
                            {
                                name = reader["name"],
                                type = reader["type"],
                                keys = reader["keys"]
                            });
                        }
                        result["constraints"] = constraints;
                    }

                    // Foreign Keys
                    using (var cmd = new SqlCommand(ForeignKeyInformation, conn))
                    {
                        var _ = cmd.Parameters.AddWithValue("@TableName", name);
                        _ = cmd.Parameters.AddWithValue("@TableSchema", schema == null ? DBNull.Value : schema);
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
                _logger.LogError(ex, "DescribeTable failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
        Title = "SQL: Read Data",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Executes SQL queries against SQL Database to read data")]
        public async Task<OperationResult> SQLReadData(
        [Description("SQL query to execute")] string sql)
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    using var cmd = new SqlCommand(sql, conn);
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
                _logger.LogError(ex, "ReadData failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
        Title = "SQL: List Tables",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Lists all tables in the SQL Database.")]
        public async Task<OperationResult> SQLListTables()
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    using var cmd = new SqlCommand(ListTablesQuery, conn);
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
                _logger.LogError(ex, "ListTables failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
     Title = "SQL: Insert Data",
     ReadOnly = false,
     Destructive = false),
     Description("Updates data in a table in the SQL Database. Expects a valid INSERT SQL statement as input. ")]
        public async Task<OperationResult> SQLInsertData(
     [Description("INSERT SQL statement")] string sql)
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
                    var rows = await cmd.ExecuteNonQueryAsync();
                    return new OperationResult(success: true, rowsAffected: rows);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InsertData failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
       Title = "Drop Table",
       ReadOnly = false,
       Destructive = true),
       Description("Drops a table in the SQL Database. Expects a valid DROP TABLE SQL statement as input.")]
        public async Task<OperationResult> SQLDropTable(
       [Description("DROP TABLE SQL statement")] string sql)
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
                    _ = await cmd.ExecuteNonQueryAsync();
                    return new OperationResult(success: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DropTable failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
        Title = "SQL: Create Table",
        ReadOnly = false,
        Destructive = false),
        Description("Creates a new table in the SQL Database. Expects a valid CREATE TABLE SQL statement as input.")]
        public async Task<OperationResult> SQLCreateTable(
        [Description("CREATE TABLE SQL statement")] string sql)
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
                    _ = await cmd.ExecuteNonQueryAsync();
                    return new OperationResult(success: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateTable failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
        Title = "SQL: Update Data",
        ReadOnly = false,
        Destructive = true),
        Description("Updates data in a table in the SQL Database. Expects a valid UPDATE SQL statement as input.")]
        public async Task<OperationResult> SQLUpdateData(
        [Description("UPDATE SQL statement")] string sql)
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
                    var rows = await cmd.ExecuteNonQueryAsync();
                    return new OperationResult(true, null, rows);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateData failed: {Message}", ex.Message);
                return new OperationResult(false, ex.Message);
            }
        }

        #endregion


        #region Private Methods
        // Helper to convert DataTable to a serializable list
        private static List<Dictionary<string, object>> DataTableToList(DataTable table)
        {
            var result = new List<Dictionary<string, object>>();
            foreach (DataRow row in table.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }
                result.Add(dict);
            }
            return result;
        }
        #endregion

    }
}
