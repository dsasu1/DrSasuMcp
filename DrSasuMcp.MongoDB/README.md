# MongoDB Database Tool for DrSasuMcp

A comprehensive Model Context Protocol (MCP) server tool for MongoDB database operations. This tool allows AI assistants to interact with MongoDB databases, execute queries, manage collections, and explore database schemas programmatically.

## Overview

The MongoDB Database Tool provides a complete suite of database operations through natural language commands. It's designed for MongoDB databases and offers both read and write capabilities with appropriate safety flags.

## Features

### 🔍 Schema Exploration
- **List Collections** - Enumerate all collections in the database
- **Describe Collection** - Get detailed schema information including:
  - Collection metadata (count, size, storage size)
  - Indexes and their configurations
  - Sample documents to infer schema
  - Inferred field types from sample data

### 📊 Data Operations
- **Read Data** - Execute find queries to retrieve documents
- **Insert Data** - Add new documents to collections
- **Update Data** - Modify existing documents
- **Delete Data** - Remove documents from collections
- **Create Collection** - Create new database collections
- **Drop Collection** - Remove collections (destructive operation)

## Architecture

```
DrSasuMcp/Tools/MongoDB/
├── MongoDBTool.cs                # Main MCP tool with exposed methods
├── IMongoConnectionFactory.cs    # Connection factory interface
└── MongoConnectionFactory.cs     # Connection factory implementation
```

## MCP Exposed Methods

### Read-Only Operations

#### `MongoListCollections`
Lists all collections in the MongoDB database.

**Parameters:** None

**Returns:** Array of collection names

**Example Usage:**
```
User: "Show me all collections in the database"
AI: Calls MongoListCollections()
Response: ["users", "products", "orders", ...]
```

---

#### `MongoDescribeCollection`
Returns comprehensive schema information for a specific collection.

**Parameters:**
- `name` (required) - Collection name

**Returns:** Detailed collection information including:
- **collection** - Metadata (name, count, size, storageSize, avgObjSize)
- **indexes** - Array of index definitions with keys and options
- **sampleDocuments** - First 5 documents from the collection
- **inferredSchema** - Field types inferred from sample documents

**Example Usage:**
```
User: "Describe the users collection structure"
AI: Calls MongoDescribeCollection(name: "users")
Response: {
  collection: { name: "users", count: 150, size: 102400, ... },
  indexes: [
    { name: "_id_", keys: { _id: 1 }, unique: true, ... },
    { name: "email_1", keys: { email: 1 }, unique: true, ... }
  ],
  sampleDocuments: [
    { _id: "...", name: "John", email: "john@example.com", age: 30 },
    ...
  ],
  inferredSchema: [
    { name: "_id", types: ["ObjectId"], nullable: false },
    { name: "name", types: ["String"], nullable: false },
    { name: "email", types: ["String"], nullable: false },
    { name: "age", types: ["Int32"], nullable: false }
  ]
}
```

---

#### `MongoReadData`
Executes find queries against the database and returns results.

**Parameters:**
- `collection` (required) - Collection name
- `filter` (optional) - JSON filter document (MongoDB query)
- `projection` (optional) - JSON projection document
- `sort` (optional) - JSON sort specification
- `limit` (optional) - Maximum number of documents to return
- `skip` (optional) - Number of documents to skip

**Returns:** Array of document objects

**Example Usage:**
```
User: "Get all users with age over 25"
AI: Calls MongoReadData(
  collection: "users",
  filter: "{\"age\": {\"$gt\": 25}}"
)
Response: [
  { _id: "...", name: "John Doe", age: 30, email: "john@example.com" },
  { _id: "...", name: "Jane Smith", age: 28, email: "jane@example.com" }
]
```

**Advanced Example:**
```
User: "Get the top 10 users by age, showing only name and email"
AI: Calls MongoReadData(
  collection: "users",
  projection: "{\"name\": 1, \"email\": 1, \"_id\": 0}",
  sort: "{\"age\": -1}",
  limit: 10
)
```

**Safety:** Read-only operation, marked as idempotent and non-destructive.

---

### Write Operations

#### `MongoInsertData`
Inserts new documents into a collection.

**Parameters:**
- `collection` (required) - Collection name
- `documents` (required) - JSON array of documents to insert

**Returns:** Number of documents inserted

**Example Usage:**
```
User: "Add a new user named 'Bob' with email 'bob@example.com' and age 35"
AI: Calls MongoInsertData(
  collection: "users",
  documents: "[{\"name\": \"Bob\", \"email\": \"bob@example.com\", \"age\": 35}]"
)
Response: { success: true, rowsAffected: 1 }
```

**Bulk Insert Example:**
```
User: "Add three new products"
AI: Calls MongoInsertData(
  collection: "products",
  documents: "[{\"name\": \"Widget\", \"price\": 19.99}, {\"name\": \"Gadget\", \"price\": 29.99}, {\"name\": \"Thing\", \"price\": 9.99}]"
)
Response: { success: true, rowsAffected: 3 }
```

**Safety:** Not destructive, but modifies data.

---

#### `MongoUpdateData`
Updates existing documents in a collection.

**Parameters:**
- `collection` (required) - Collection name
- `filter` (required) - JSON filter document to identify documents
- `update` (required) - JSON update document with MongoDB update operators
- `upsert` (optional) - Insert document if no match found (default: false)
- `multi` (optional) - Update all matching documents (default: false for safety)

**Returns:** Number of documents modified

**Example Usage:**
```
User: "Update user John's age to 31"
AI: Calls MongoUpdateData(
  collection: "users",
  filter: "{\"name\": \"John\"}",
  update: "{\"$set\": {\"age\": 31}}"
)
Response: { success: true, rowsAffected: 1 }
```

**Update Multiple Documents:**
```
User: "Increase all product prices by 10%"
AI: Calls MongoUpdateData(
  collection: "products",
  filter: "{}",
  update: "{\"$mul\": {\"price\": 1.1}}",
  multi: true
)
Response: { success: true, rowsAffected: 50 }
```

**Upsert Example:**
```
User: "Set user Bob's email to 'newemail@example.com', create if doesn't exist"
AI: Calls MongoUpdateData(
  collection: "users",
  filter: "{\"name\": \"Bob\"}",
  update: "{\"$set\": {\"email\": \"newemail@example.com\"}}",
  upsert: true
)
```

**Safety:** Marked as destructive (modifies existing data).

---

#### `MongoDeleteData`
Deletes documents from a collection.

**Parameters:**
- `collection` (required) - Collection name
- `filter` (required) - JSON filter document to identify documents
- `multi` (optional) - Delete all matching documents (default: false for safety)

**Returns:** Number of documents deleted

**Example Usage:**
```
User: "Delete user with email 'old@example.com'"
AI: Calls MongoDeleteData(
  collection: "users",
  filter: "{\"email\": \"old@example.com\"}"
)
Response: { success: true, rowsAffected: 1 }
```

**Delete Multiple Documents:**
```
User: "Delete all products with price less than 10"
AI: Calls MongoDeleteData(
  collection: "products",
  filter: "{\"price\": {\"$lt\": 10}}",
  multi: true
)
Response: { success: true, rowsAffected: 15 }
```

**Safety:** ⚠️ Marked as destructive - permanently deletes documents.

---

#### `MongoCreateCollection`
Creates a new collection in the database.

**Parameters:**
- `name` (required) - Collection name
- `options` (optional) - JSON string with collection options (capped, size, max, validator, etc.)

**Returns:** Success confirmation

**Example Usage:**
```
User: "Create a collection called 'feedback'"
AI: Calls MongoCreateCollection(name: "feedback")
Response: { success: true }
```

**Create Capped Collection:**
```
User: "Create a capped collection for logs with max size 10MB and max 1000 documents"
AI: Calls MongoCreateCollection(
  name: "logs",
  options: "{\"capped\": true, \"size\": 10485760, \"max\": 1000}"
)
Response: { success: true }
```

**Safety:** Not destructive (creates new resources).

---

#### `MongoDropCollection`
Removes a collection from the database.

**Parameters:**
- `name` (required) - Collection name

**Returns:** Success confirmation

**Example Usage:**
```
User: "Drop the temporary TempData collection"
AI: Calls MongoDropCollection(name: "TempData")
Response: { success: true }
```

**Safety:** ⚠️ Marked as destructive - permanently deletes collection and all its data.

---

## Configuration

### Environment Variable

The MongoDB tool requires a connection string to be set via environment variable:

**Variable Name:** `MONGODB_CONNECTION_STRING`

#### Windows (PowerShell)
```powershell
$env:MONGODB_CONNECTION_STRING = "mongodb://localhost:27017/test"
```

#### Windows (Command Prompt)
```cmd
SET MONGODB_CONNECTION_STRING=mongodb://localhost:27017/test
```

#### Linux/Mac
```bash
export MONGODB_CONNECTION_STRING="mongodb://localhost:27017/test"
```

### Connection String Examples

#### Local MongoDB (Default)
```
mongodb://localhost:27017/test
```

#### Local MongoDB with Authentication
```
mongodb://username:password@localhost:27017/test
```

#### MongoDB Replica Set
```
mongodb://username:password@host1:27017,host2:27017,host3:27017/test?replicaSet=rs0
```

#### MongoDB Atlas (Cloud)
```
mongodb+srv://username:password@cluster.mongodb.net/test?retryWrites=true&w=majority
```

#### MongoDB with Options
```
mongodb://localhost:27017/test?authSource=admin&ssl=true
```

### Adding to MCP Client

Once you have your connection string configured, add the MongoDB tool to your MCP client:

#### Claude Desktop Configuration

**Windows:** `%APPDATA%\Claude\claude_desktop_config.json`  
**Mac:** `~/Library/Application Support/Claude/claude_desktop_config.json`

**Development Mode (using `dotnet run`):**

```json
{
  "mcpServers": {
    "drsasumcp-mongodb": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Projects\\personal\\DrSasuMcp\\DrSasuMcp.MongoDB\\DrSasuMcp.MongoDB.csproj"],
      "env": {
        "MONGODB_CONNECTION_STRING": "mongodb://localhost:27017/test"
      }
    }
  }
}
```

**Production Mode (using published executable):**

First, publish the project:
```bash
cd DrSasuMcp.MongoDB
dotnet publish -c Release -o ./publish
```

Then configure:

**Windows:**
```json
{
  "mcpServers": {
    "drsasumcp-mongodb": {
      "command": "C:\\Projects\\personal\\DrSasuMcp\\DrSasuMcp.MongoDB\\publish\\DrSasuMcp.MongoDB.exe",
      "env": {
        "MONGODB_CONNECTION_STRING": "mongodb://localhost:27017/test"
      }
    }
  }
}
```

**Mac/Linux:**
```json
{
  "mcpServers": {
    "drsasumcp-mongodb": {
      "command": "/path/to/DrSasuMcp/DrSasuMcp.MongoDB/publish/DrSasuMcp.MongoDB",
      "env": {
        "MONGODB_CONNECTION_STRING": "mongodb://localhost:27017/test"
      }
    }
  }
}
```

> **Note:** After updating your MCP client configuration, restart the client for changes to take effect.

**Connection String Components:**
- `mongodb://` or `mongodb+srv://` - Protocol (srv for DNS seedlist)
- `username:password@` - Authentication credentials (optional)
- `host:port` - Server address and port
- `/database` - Database name (required)
- `?options` - Additional connection options

**Note:** The database name can be specified in the connection string or will default to "test" if not provided.

---

## Database Requirements

- **MongoDB** version 3.6 or later
- Network access to MongoDB server
- Appropriate database user permissions

---

## Operation Result Format

All operations return a standard `OperationResult` object:

### Successful Query
```json
{
  "success": true,
  "data": [
    { "_id": "...", "name": "John Doe", "age": 30 },
    { "_id": "...", "name": "Jane Smith", "age": 25 }
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
  "error": "Collection 'users' not found."
}
```

---

## Usage Examples

### Example 1: Database Exploration
```
User: "What collections do I have in my database?"
AI: Calls MongoListCollections()

User: "Show me the structure of the users collection"
AI: Calls MongoDescribeCollection(name: "users")
```

### Example 2: Data Querying
```
User: "Get all orders from last month"
AI: Calls MongoReadData(
  collection: "orders",
  filter: "{\"orderDate\": {\"$gte\": \"2024-01-01T00:00:00Z\"}}"
)

User: "How many users do we have?"
AI: Calls MongoReadData(
  collection: "users",
  projection: "{\"_id\": 1}",
  limit: 1
)
// Then counts the total from DescribeCollection or uses aggregation
```

### Example 3: Data Modification
```
User: "Add a new product 'Widget' with price $19.99"
AI: Calls MongoInsertData(
  collection: "products",
  documents: "[{\"name\": \"Widget\", \"price\": 19.99}]"
)

User: "Increase all product prices by 10%"
AI: Calls MongoUpdateData(
  collection: "products",
  filter: "{}",
  update: "{\"$mul\": {\"price\": 1.1}}",
  multi: true
)
```

### Example 4: Collection Management
```
User: "Create a collection for storing customer feedback"
AI: Calls MongoCreateCollection(name: "feedback")

User: "Drop the old TempData collection"
AI: Calls MongoDropCollection(name: "TempData")
```

---

## Safety Features

### Operation Flags

Each method is marked with appropriate safety flags:

| Method | ReadOnly | Idempotent | Destructive |
|--------|----------|------------|-------------|
| MongoListCollections | ✅ | ✅ | ❌ |
| MongoDescribeCollection | ✅ | ✅ | ❌ |
| MongoReadData | ✅ | ✅ | ❌ |
| MongoInsertData | ❌ | ❌ | ❌ |
| MongoCreateCollection | ❌ | ❌ | ❌ |
| MongoUpdateData | ❌ | ❌ | ✅ |
| MongoDeleteData | ❌ | ❌ | ✅ |
| MongoDropCollection | ❌ | ❌ | ✅ |

### Connection Management

- Uses MongoDB driver's built-in connection pooling
- Connections are managed automatically by the driver
- Async/await pattern throughout for scalability

### Safety Defaults

- `multi` parameter defaults to `false` for Update and Delete operations to prevent accidental bulk modifications
- Collection operations validate existence before execution where applicable

---

## Error Handling

The tool provides comprehensive error handling:

### Common Errors

**Connection Errors**
```
Error: "Connection string is not set in environment variable 'MONGODB_CONNECTION_STRING'"
Solution: Set the MONGODB_CONNECTION_STRING environment variable
```

**Collection Not Found**
```
Error: "Collection 'users' not found"
Solution: Check collection name, use MongoListCollections() to see available collections
```

**Invalid JSON**
```
Error: "Invalid JSON in filter document"
Solution: Verify JSON syntax (e.g., proper quotes, brackets, braces)
```

**Authentication Errors**
```
Error: "Authentication failed"
Solution: Verify credentials in connection string and ensure user exists in MongoDB
```

**Network Errors**
```
Error: "A network-related error occurred"
Solution: 
- Check MongoDB server is running
- Verify server address/port
- Check firewall settings
- Ensure MongoDB network binding is configured
```

---

## Advanced Features

### Schema Inference

The `MongoDescribeCollection` method automatically infers schema from sample documents:

- Analyzes field types across sample documents
- Identifies nullable fields
- Provides type information for each field

This is particularly useful since MongoDB is schema-less.

### JSON Query Support

All query operations accept JSON strings for:
- **Filters** - MongoDB query syntax (e.g., `{"age": {"$gt": 25}}`)
- **Projections** - Field selection (e.g., `{"name": 1, "age": 1, "_id": 0}`)
- **Sort** - Sort specification (e.g., `{"age": -1}` for descending)

### MongoDB Update Operators

The `MongoUpdateData` method supports all MongoDB update operators:
- `$set` - Set field values
- `$unset` - Remove fields
- `$inc` - Increment numeric values
- `$mul` - Multiply numeric values
- `$push` - Add to arrays
- `$pull` - Remove from arrays
- And many more...

**Example:**
```json
{
  "$set": {"status": "active"},
  "$inc": {"views": 1},
  "$push": {"tags": "new"}
}
```

### Capped Collections

Create time-series or log collections with size limits:

```json
{
  "capped": true,
  "size": 10485760,
  "max": 1000
}
```

---

## Dependencies

- `MongoDB.Driver` - Official MongoDB C# driver
- `Microsoft.Extensions.Logging` - Logging framework
- `ModelContextProtocol.Server` - MCP server attributes
- `System.Linq` - LINQ operations

---

## Integration

### Service Registration

The MongoDB tool is registered in `Program.cs`:

```csharp
// Register MongoDB Tool dependencies
builder.Services.AddSingleton<IMongoConnectionFactory, MongoConnectionFactory>();
builder.Services.AddSingleton<MongoDBTool>();
```

### MCP Auto-Discovery

The `[McpServerToolType]` attribute on `MongoDBTool` enables automatic discovery by the MCP server, making all methods available to AI assistants.

---

## Best Practices

### 1. Use Indexes

Create indexes on frequently queried fields:
```javascript
// Use MongoDB shell or create index tool
db.users.createIndex({ "email": 1 })
```

### 2. Limit Result Sets

For large collections, always use `limit`:
```
MongoReadData(collection: "users", limit: 100)
```

### 3. Use Projections

Only retrieve needed fields to reduce data transfer:
```
MongoReadData(
  collection: "users",
  projection: "{\"name\": 1, \"email\": 1, \"_id\": 0}"
)
```

### 4. Be Cautious with Destructive Operations

- Always verify before calling `MongoDropCollection` or bulk `MongoUpdateData`/`MongoDeleteData`
- Use `multi: false` by default for safety
- Test queries with `MongoReadData` before deleting

### 5. Validate JSON Input

Ensure filter, update, and projection JSON is valid:
- Use proper escaping for strings
- Match brackets and braces
- Use MongoDB query syntax correctly

### 6. Connection String Security

- Never commit connection strings with credentials
- Use environment variables or secure configuration
- Use MongoDB Atlas connection strings for cloud deployments

---

## Troubleshooting

### Issue: "Connection string is not set"
**Solution:** Set the `MONGODB_CONNECTION_STRING` environment variable before starting the application.

### Issue: "Authentication failed"
**Solution:** Verify credentials in connection string and ensure user exists in MongoDB with appropriate permissions.

### Issue: "A network-related error occurred"
**Solution:** 
- Check MongoDB is running: `mongosh` or `mongo` command
- Verify server name/address in connection string
- Check firewall settings
- Ensure MongoDB is listening on the specified port (default: 27017)

### Issue: "Collection not found"
**Solution:** 
- Use `MongoListCollections()` to see available collections
- Check collection name spelling
- Collections are case-sensitive

### Issue: "Invalid JSON"
**Solution:** 
- Validate JSON syntax using a JSON validator
- Ensure proper escaping of quotes in strings
- Check bracket/brace matching

### Issue: "Duplicate key error"
**Solution:** 
- Check for unique indexes on the collection
- Ensure inserted/updated documents don't violate unique constraints
- Use `upsert: true` in updates to handle existing documents

---

## MongoDB Query Examples

### Filter Examples

**Equality:**
```json
{"name": "John"}
```

**Comparison Operators:**
```json
{"age": {"$gt": 25}}
{"price": {"$gte": 10, "$lte": 100}}
```

**Logical Operators:**
```json
{"$or": [{"status": "active"}, {"status": "pending"}]}
{"$and": [{"age": {"$gt": 18}}, {"age": {"$lt": 65}}]}
```

**Array Operators:**
```json
{"tags": {"$in": ["red", "blue"]}}
{"tags": {"$all": ["red", "blue"]}}
```

**Regex:**
```json
{"email": {"$regex": "@example\\.com$", "$options": "i"}}
```

### Update Examples

**Set Fields:**
```json
{"$set": {"status": "active", "lastModified": "2024-01-01T00:00:00Z"}}
```

**Increment:**
```json
{"$inc": {"views": 1, "score": 5}}
```

**Array Operations:**
```json
{"$push": {"tags": "new"}}
{"$pull": {"tags": "old"}}
{"$addToSet": {"tags": "unique"}}
```

**Multiple Operations:**
```json
{
  "$set": {"status": "updated"},
  "$inc": {"version": 1},
  "$unset": {"temp": ""}
}
```

---

## Security Notes

⚠️ **Important Security Considerations:**

1. **Connection Strings**: Never commit connection strings with credentials to source control. Use environment variables or secure configuration management.

2. **Permissions**: Grant minimal necessary permissions to the MongoDB user:
   - Read-only user for read operations
   - Specific database access (not admin)
   - No drop database permissions unless needed

3. **TLS/SSL**: Use encrypted connections for remote databases:
   ```
   mongodb://host:27017/db?ssl=true
   ```

4. **Input Validation**: While MongoDB queries are JSON (reducing SQL injection risk), still validate:
   - JSON syntax
   - Query structure
   - User-provided filters

5. **Auditing**: Monitor database operations, especially destructive ones:
   - Enable MongoDB audit logging
   - Log all write operations
   - Track collection drops

6. **Network Security**: 
   - Use firewall rules to restrict MongoDB access
   - Use VPN or private networks for database connections
   - Consider MongoDB Atlas for managed security

---

## Performance Considerations

1. **Connection Pooling**: MongoDB driver handles connection pooling automatically. Default settings are usually sufficient.

2. **Indexes**: Create indexes on frequently queried fields:
   ```javascript
   db.collection.createIndex({ "field": 1 })
   ```

3. **Query Optimization**:
   - Use projections to limit returned fields
   - Use `limit` to restrict result sets
   - Filter early in the query pipeline

4. **Bulk Operations**: Use bulk inserts/updates when modifying multiple documents:
   ```
   MongoInsertData(collection: "users", documents: "[{...}, {...}, {...}]")
   ```

5. **Read Preferences**: For replica sets, consider read preferences (handled by connection string options).

---

## Extensibility

The MongoDB tool is designed for easy extension:

### Adding New Operations

1. Add a new method to `MongoDBTool.cs`
2. Decorate with `[McpServerTool]` attribute
3. Set appropriate safety flags (ReadOnly, Destructive, etc.)

### Custom Connection Logic

Implement `IMongoConnectionFactory` for custom connection management:
- Multi-database support
- Connection string rotation
- Custom authentication
- Connection monitoring

### Example: Adding Aggregation Support

```csharp
[McpServerTool(
    Title = "MongoDB: Aggregate",
    ReadOnly = true,
    Idempotent = true,
    Destructive = false),
    Description("Executes aggregation pipeline")]
public async Task<OperationResult> MongoAggregate(
    [Description("Collection name")] string collection,
    [Description("JSON array of pipeline stages")] string pipeline)
{
    // Implementation
}
```

---

## Comparison with SQL Tool

| Feature | SQL Tool | MongoDB Tool |
|---------|----------|--------------|
| **Query Language** | SQL | MongoDB Query (JSON) |
| **Schema** | Fixed (tables/columns) | Schema-less (inferred) |
| **Data Model** | Relational (rows) | Document (JSON) |
| **Operations** | SQL statements | JSON documents |
| **Indexes** | Automatic discovery | Explicit listing |
| **Transactions** | Built-in | Optional (not exposed) |

---

## License

Part of the DrSasuMcp project.

---

## Support

For issues or questions:
1. Check this README for common scenarios
2. Verify connection string configuration
3. Review MongoDB server logs
4. Examine MCP server logs for detailed error messages
5. Consult MongoDB documentation for query syntax

---

**Ready to use! Set your MONGODB_CONNECTION_STRING environment variable and start querying!** 🚀

