# PostgreSQL Database Tool for DrSasuMcp

A Model Context Protocol (MCP) server tool for PostgreSQL database operations. This tool allows AI assistants to interact with PostgreSQL databases, execute queries, manage tables, and explore database schemas programmatically.

## Overview

The PostgreSQL Database Tool provides database operations through natural language commands. It mirrors the SQL Server tool surface for consistent AI usage across relational databases.

## Features

### Schema Exploration
- **PostgresListTables** - Enumerate all user tables (`schema.table`)
- **PostgresDescribeTable** - Schema details: columns, indexes, constraints, foreign keys

### Data Operations
- **PostgresReadData** - Execute SELECT queries
- **PostgresInsertData** - INSERT statements
- **PostgresUpdateData** - UPDATE statements (destructive)
- **PostgresCreateTable** - CREATE TABLE statements
- **PostgresDropTable** - DROP TABLE statements (destructive)

## Architecture

```
DrSasuMcp.PostgreSQL/
├── Program.cs
├── PostgreSQL/
│   ├── PostgreSQLTool.cs
│   ├── PostgreSQLToolQueries.cs
│   ├── PostgreSQLToolConstants.cs
│   ├── IPostgreSqlConnectionFactory.cs
│   └── PostgreSqlConnectionFactory.cs
```

## Configuration

### Environment Variable

**Variable Name:** `POSTGRES_CONNECTION_STRING`

#### Windows (PowerShell)
```powershell
$env:POSTGRES_CONNECTION_STRING = "Host=localhost;Database=test;Username=postgres;Password=yourpassword"
```

#### Linux/Mac
```bash
export POSTGRES_CONNECTION_STRING="Host=localhost;Database=test;Username=postgres;Password=yourpassword"
```

### Connection String Examples

#### Local development
```
Host=localhost;Database=mydb;Username=postgres;Password=secret
```

#### With SSL
```
Host=db.example.com;Database=mydb;Username=app;Password=secret;SSL Mode=Require
```

### MCP Client Configuration

**Development Mode:**

```json
{
  "mcpServers": {
    "drsasumcp-postgres": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Projects\\personal\\DrSasuMcp\\DrSasuMcp.PostgreSQL\\DrSasuMcp.PostgreSQL.csproj"],
      "env": {
        "POSTGRES_CONNECTION_STRING": "Host=localhost;Database=mydb;Username=postgres;Password=yourpassword"
      }
    }
  }
}
```

**Production Mode:**

```bash
dotnet publish DrSasuMcp.PostgreSQL/DrSasuMcp.PostgreSQL.csproj -c Release -o ./publish/postgres
```

```json
{
  "mcpServers": {
    "drsasumcp-postgres": {
      "command": "C:\\Projects\\personal\\DrSasuMcp\\publish\\postgres\\DrSasuMcp.PostgreSQL.exe",
      "env": {
        "POSTGRES_CONNECTION_STRING": "Host=localhost;Database=mydb;Username=postgres;Password=yourpassword"
      }
    }
  }
}
```

## Database Requirements

- **PostgreSQL** 12 or later recommended
- Read access to `information_schema` and `pg_catalog` for schema exploration
- Appropriate privileges for DML/DDL operations you intend to use

## Operation Result Format

All operations return `OperationResult` from `DrSasuMcp.Common`:

```json
{
  "success": true,
  "data": [ { "id": 1, "name": "example" } ]
}
```

```json
{
  "success": true,
  "rowsAffected": 1
}
```

```json
{
  "success": false,
  "error": "relation \"missing\" does not exist"
}
```

## Safety Features

| Method | ReadOnly | Idempotent | Destructive |
|--------|----------|------------|-------------|
| PostgresListTables | Yes | Yes | No |
| PostgresDescribeTable | Yes | Yes | No |
| PostgresReadData | Yes | Yes | No |
| PostgresInsertData | No | No | No |
| PostgresCreateTable | No | No | No |
| PostgresUpdateData | No | No | Yes |
| PostgresDropTable | No | No | Yes |

## Security

- Use parameterized metadata queries for schema operations
- Grant least-privilege database roles for AI-driven access
- Prefer read-only database users when only exploration is needed
- Use SSL for remote connections

## Extensibility

1. Add a method to `PostgreSQLTool`
2. Decorate with `[McpServerTool]` and `[Description]` attributes
3. Return `Task<OperationResult>`
4. Rebuild — tools are discovered automatically via `WithToolsFromAssembly()`

## Service Registration

```csharp
builder.Services.AddSingleton<IPostgreSqlConnectionFactory, PostgreSqlConnectionFactory>();
builder.Services.AddSingleton<PostgreSQLTool>();
builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();
```
