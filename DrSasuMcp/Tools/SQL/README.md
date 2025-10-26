# SQL Database Tool for DrSasuMcp

A comprehensive Model Context Protocol (MCP) server tool for SQL Server database operations. This tool allows AI assistants to interact with SQL Server databases, execute queries, manage tables, and explore database schemas programmatically.

## Overview

The SQL Database Tool provides a complete suite of database operations through natural language commands. It's designed for SQL Server databases and offers both read and write capabilities with appropriate safety flags.

## Features

### 🔍 Schema Exploration
- **List Tables** - Enumerate all tables in the database
- **Describe Table** - Get detailed schema information including:
  - Column definitions (name, type, length, precision, nullable)
  - Indexes and their columns
  - Primary and foreign key constraints
  - Extended properties and descriptions
  - Table metadata (owner, type)

### 📊 Data Operations
- **Read Data** - Execute SELECT queries to retrieve data
- **Insert Data** - Add new records to tables
- **Update Data** - Modify existing records
- **Create Table** - Create new database tables
- **Drop Table** - Remove tables (destructive operation)

## Architecture

```
DrSasuMcp/Tools/SQL/
├── SQLTool.cs                # Main MCP tool with exposed methods
├── SQLToolConstant.cs        # SQL query constants
├── ISqlConnectionFactory.cs  # Connection factory interface
└── SqlConnectionFactory.cs   # Connection factory implementation
```

## MCP Exposed Methods

### Read-Only Operations

#### `ListTables`
Lists all tables in the SQL Server database with their schema names.

**Parameters:** None

**Returns:** Array of fully qualified table names (`schema.tablename`)

**Example Usage:**
```
User: "Show me all tables in the database"
AI: Calls ListTables()
Response: ["dbo.Users", "dbo.Products", "dbo.Orders", ...]
```

---

#### `DescribeTable`
Returns comprehensive schema information for a specific table.

**Parameters:**
- `name` (required) - Table name, optionally with schema (e.g., "Users" or "dbo.Users")

**Returns:** Detailed table information including:
- **table** - Metadata (id, name, schema, owner, type, description)
- **columns** - Array of column definitions with data types, constraints
- **indexes** - Non-primary/unique indexes
- **constraints** - Primary keys and unique constraints
- **foreignKeys** - Foreign key relationships

**Example Usage:**
```
User: "Describe the Users table structure"
AI: Calls DescribeTable(name: "Users")
Response: {
  table: { name: "Users", schema: "dbo", owner: "dbo" },
  columns: [
    { name: "Id", type: "int", nullable: false, ... },
    { name: "Name", type: "nvarchar", length: 100, nullable: false, ... }
  ],
  indexes: [...],
  constraints: [...],
  foreignKeys: [...]
}
```

**Schema Support:** Automatically handles both formats:
- `"Users"` - Searches all schemas
- `"dbo.Users"` - Specific schema

---

#### `ReadData`
Executes SELECT queries against the database and returns results.

**Parameters:**
- `sql` (required) - SQL SELECT query to execute

**Returns:** Array of row objects with column names as keys

**Example Usage:**
```
User: "Get all users with age over 25"
AI: Calls ReadData(sql: "SELECT * FROM Users WHERE Age > 25")
Response: [
  { Id: 1, Name: "John Doe", Age: 30, Email: "john@example.com" },
  { Id: 2, Name: "Jane Smith", Age: 28, Email: "jane@example.com" }
]
```

**Safety:** Read-only operation, marked as idempotent and non-destructive.

---

### Write Operations

#### `InsertData`
Inserts new records into database tables.

**Parameters:**
- `sql` (required) - INSERT SQL statement

**Returns:** Number of rows affected

**Example Usage:**
```
User: "Add a new user named 'Bob' with email 'bob@example.com'"
AI: Calls InsertData(sql: "INSERT INTO Users (Name, Email) VALUES ('Bob', 'bob@example.com')")
Response: { success: true, rowsAffected: 1 }
```

**Safety:** Not destructive, but modifies data.

---

#### `UpdateData`
Updates existing records in database tables.

**Parameters:**
- `sql` (required) - UPDATE SQL statement

**Returns:** Number of rows affected

**Example Usage:**
```
User: "Update user ID 5's email to 'newemail@example.com'"
AI: Calls UpdateData(sql: "UPDATE Users SET Email = 'newemail@example.com' WHERE Id = 5")
Response: { success: true, rowsAffected: 1 }
```

**Safety:** Marked as destructive (modifies existing data).

---

#### `CreateTable`
Creates new tables in the database.

**Parameters:**
- `sql` (required) - CREATE TABLE SQL statement

**Returns:** Success confirmation

**Example Usage:**
```
User: "Create a Products table with Id, Name, and Price columns"
AI: Calls CreateTable(sql: "CREATE TABLE Products (Id INT PRIMARY KEY, Name NVARCHAR(100), Price DECIMAL(10,2))")
Response: { success: true }
```

**Safety:** Not destructive (creates new resources).

---

#### `DropTable`
Removes tables from the database.

**Parameters:**
- `sql` (required) - DROP TABLE SQL statement

**Returns:** Success confirmation

**Example Usage:**
```
User: "Drop the temporary TempData table"
AI: Calls DropTable(sql: "DROP TABLE TempData")
Response: { success: true }
```

**Safety:** ⚠️ Marked as destructive - permanently deletes table and all data.

---

## Configuration

### Environment Variable

The SQL tool requires a connection string to be set via environment variable:

**Variable Name:** `SQL_CONNECTION_STRING`

#### Windows (PowerShell)
```powershell
$env:SQL_CONNECTION_STRING = "Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True"
```

#### Windows (Command Prompt)
```cmd
SET SQL_CONNECTION_STRING=Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True
```

#### Linux/Mac
```bash
export SQL_CONNECTION_STRING="Server=localhost;Database=test;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
```

### Connection String Examples

#### Windows Authentication (Local)
```
Server=.;Database=MyDatabase;Trusted_Connection=True;TrustServerCertificate=True
```

#### SQL Server Authentication
```
Server=localhost;Database=MyDatabase;User Id=myuser;Password=mypassword;TrustServerCertificate=True
```

#### Remote Server
```
Server=sql.example.com,1433;Database=MyDatabase;User Id=myuser;Password=mypassword;Encrypt=True
```

#### Azure SQL Database
```
Server=tcp:myserver.database.windows.net,1433;Database=MyDatabase;User ID=myuser@myserver;Password=mypassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

---

## Database Requirements

- **SQL Server** version 2012 or later (uses `STRING_AGG` which requires SQL Server 2017+)
- Access to system catalog views:
  - `sys.tables`
  - `sys.columns`
  - `sys.indexes`
  - `sys.key_constraints`
  - `sys.foreign_keys`
  - `INFORMATION_SCHEMA.TABLES`

---

## Operation Result Format

All operations return a standard `OperationResult` object:

### Successful Query
```json
{
  "success": true,
  "data": [
    { "Id": 1, "Name": "John Doe", "Age": 30 },
    { "Id": 2, "Name": "Jane Smith", "Age": 25 }
  ]
}
```

### Successful Write Operation
```json
{
  "success": true,
  "rowsAffected": 3
}
```

### Error Response
```json
{
  "success": false,
  "error": "Invalid column name 'NonExistentColumn'."
}
```

---

## Usage Examples

### Example 1: Database Exploration
```
User: "What tables do I have in my database?"
AI: Calls ListTables()

User: "Show me the structure of the Users table"
AI: Calls DescribeTable(name: "Users")
```

### Example 2: Data Querying
```
User: "Get all orders from last month"
AI: Calls ReadData(sql: "SELECT * FROM Orders WHERE OrderDate >= DATEADD(month, -1, GETDATE())")

User: "How many users do we have?"
AI: Calls ReadData(sql: "SELECT COUNT(*) AS UserCount FROM Users")
```

### Example 3: Data Modification
```
User: "Add a new product 'Widget' with price $19.99"
AI: Calls InsertData(sql: "INSERT INTO Products (Name, Price) VALUES ('Widget', 19.99)")

User: "Increase all product prices by 10%"
AI: Calls UpdateData(sql: "UPDATE Products SET Price = Price * 1.1")
```

### Example 4: Schema Management
```
User: "Create a table for storing customer feedback"
AI: Calls CreateTable(sql: "CREATE TABLE Feedback (Id INT IDENTITY PRIMARY KEY, CustomerId INT, Comment NVARCHAR(500), Rating INT, CreatedDate DATETIME DEFAULT GETDATE())")

User: "Drop the old TempData table"
AI: Calls DropTable(sql: "DROP TABLE TempData")
```

---

## Safety Features

### Operation Flags

Each method is marked with appropriate safety flags:

| Method | ReadOnly | Idempotent | Destructive |
|--------|----------|------------|-------------|
| ListTables | ✅ | ✅ | ❌ |
| DescribeTable | ✅ | ✅ | ❌ |
| ReadData | ✅ | ✅ | ❌ |
| InsertData | ❌ | ❌ | ❌ |
| CreateTable | ❌ | ❌ | ❌ |
| UpdateData | ❌ | ❌ | ✅ |
| DropTable | ❌ | ❌ | ✅ |

### Connection Management

- Uses ADO.NET connection pooling for optimal performance
- Connections are properly disposed after each operation
- Async/await pattern throughout for scalability

---

## Error Handling

The tool provides comprehensive error handling:

### Common Errors

**Connection Errors**
```
Error: "Connection string is not set in environment variable 'SQL_CONNECTION_STRING'"
Solution: Set the SQL_CONNECTION_STRING environment variable
```

**Table Not Found**
```
Error: "Table 'Users' not found"
Solution: Check table name and schema, use ListTables() to see available tables
```

**SQL Syntax Errors**
```
Error: "Incorrect syntax near 'FORM'"
Solution: Verify SQL statement syntax (e.g., FROM vs FORM)
```

**Permission Errors**
```
Error: "The SELECT permission was denied on the object 'Users'"
Solution: Ensure database user has appropriate permissions
```

---

## Advanced Features

### Schema-Aware Operations

The `DescribeTable` method intelligently handles schema specifications:

```csharp
// These are equivalent if Users exists in dbo schema:
DescribeTable(name: "Users")
DescribeTable(name: "dbo.Users")

// But you can target specific schemas:
DescribeTable(name: "sales.Customers")
DescribeTable(name: "inventory.Products")
```

### Extended Properties

The tool retrieves SQL Server extended properties (descriptions) for:
- Tables
- Columns
- Indexes

These appear as the `description` field in results.

### Foreign Key Relationships

Foreign keys include full relationship details:
- Source table and columns
- Referenced table and columns
- Constraint name
- Multi-column foreign keys are supported

---

## Dependencies

- `Microsoft.Data.SqlClient` - SQL Server connectivity
- `Microsoft.Extensions.Logging` - Logging framework
- `ModelContextProtocol.Server` - MCP server attributes
- `System.Data` - ADO.NET data access

---

## Integration

### Service Registration

The SQL tool is registered in `Program.cs`:

```csharp
// Register SQL Tool dependencies
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddSingleton<SQLTool>();
```

### MCP Auto-Discovery

The `[McpServerToolType]` attribute on `SQLTool` enables automatic discovery by the MCP server, making all methods available to AI assistants.

---

## Best Practices

### 1. Use Parameterized Queries (When Possible)
While the tool accepts raw SQL, consider validation in production scenarios.

### 2. Limit Result Sets
For large tables, use WHERE clauses and TOP/LIMIT:
```sql
SELECT TOP 100 * FROM LargeTable WHERE Status = 'Active'
```

### 3. Be Cautious with Destructive Operations
- Always verify before calling `DropTable` or bulk `UpdateData`
- Use transactions in application code when coordinating multiple changes

### 4. Schema Prefixing
For clarity and performance, specify schema names:
```sql
SELECT * FROM dbo.Users  -- Good
SELECT * FROM Users      -- Works but slower
```

### 5. Test Connection String
Verify connectivity before deploying:
```powershell
# Test connection (PowerShell with SqlServer module)
Test-SqlConnection -ConnectionString $env:SQL_CONNECTION_STRING
```

---

## Troubleshooting

### Issue: "Connection string is not set"
**Solution:** Set the `SQL_CONNECTION_STRING` environment variable before starting the application.

### Issue: "Login failed for user"
**Solution:** Verify credentials in connection string and ensure user exists in SQL Server.

### Issue: "A network-related error occurred"
**Solution:** 
- Check SQL Server is running
- Verify server name/address
- Check firewall settings
- Ensure SQL Server TCP/IP is enabled

### Issue: "Cannot open database"
**Solution:** Database must exist before connecting. Create it first or update connection string.

### Issue: Foreign keys not showing
**Solution:** Requires SQL Server 2017+ for `STRING_AGG` function. For older versions, modify the query in `SQLToolConstant.cs`.

---

## Extensibility

The SQL tool is designed for easy extension:

### Adding New Operations

1. Add a new method to `SQLTool.cs`
2. Decorate with `[McpServerTool]` attribute
3. Add SQL queries to `SQLToolConstant.cs` if needed

### Custom Connection Logic

Implement `ISqlConnectionFactory` for custom connection management:
- Connection pooling strategies
- Multi-database support
- Credential management

---

## Performance Considerations

- Connection pooling is handled automatically by ADO.NET
- Each operation opens a new connection (from pool) and disposes it
- Queries are executed asynchronously for better scalability
- Large result sets are streamed, not loaded entirely into memory

---

## Security Notes

⚠️ **Important Security Considerations:**

1. **SQL Injection**: The tool accepts raw SQL. In production, validate inputs or use parameterized queries.
2. **Permissions**: Grant minimal necessary permissions to the database user.
3. **Connection Strings**: Never commit connection strings with credentials to source control.
4. **TLS/Encryption**: Use encrypted connections for remote databases.
5. **Auditing**: Monitor database operations, especially destructive ones.

---

## License

Part of the DrSasuMcp project.

---

## Support

For issues or questions:
1. Check this README for common scenarios
2. Verify connection string configuration
3. Review SQL Server error logs
4. Examine MCP server logs for detailed error messages

---

**Ready to use! Set your SQL_CONNECTION_STRING environment variable and start querying!** 🚀

