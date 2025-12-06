# MongoDB Tool Implementation Plan

This document outlines the plan to implement a MongoDB tool for DrSasuMcp, following the same architectural patterns as the SQL tool.

## Overview

The MongoDB tool will provide a comprehensive suite of database operations for MongoDB databases through the Model Context Protocol (MCP). It will mirror the functionality and structure of the SQL tool while adapting to MongoDB's document-based architecture.

## Architecture

Following the SQL tool pattern, the MongoDB tool will consist of:

```
DrSasuMcp/Tools/MongoDB/
├── MongoDBTool.cs                # Main MCP tool with exposed methods
├── MongoDBToolConstant.cs        # MongoDB query/operation constants (if needed)
├── IMongoConnectionFactory.cs    # Connection factory interface
├── MongoConnectionFactory.cs     # Connection factory implementation
└── README.md                     # Comprehensive documentation
```

## Core Components

### 1. Connection Factory Pattern

**Interface: `IMongoConnectionFactory`**
- `Task<IMongoDatabase> GetDatabaseAsync()` - Returns a MongoDB database instance
- Similar to `ISqlConnectionFactory.GetOpenConnectionAsync()`

**Implementation: `MongoConnectionFactory`**
- Reads connection string from `MONGODB_CONNECTION_STRING` environment variable
- Parses connection string to extract database name
- Returns `IMongoDatabase` instance using MongoDB C# driver
- Handles connection pooling (MongoDB driver handles this automatically)

### 2. Main Tool Class: `MongoDBTool`

**Constructor:**
```csharp
[McpServerToolType]
public partial class MongoDBTool(IMongoConnectionFactory connectionFactory, ILogger<MongoDBTool> logger)
```

**Dependencies:**
- `IMongoConnectionFactory` - For database connections
- `ILogger<MongoDBTool>` - For logging operations

## MCP Exposed Methods

### Read-Only Operations

#### 1. `MongoListCollections`
**Purpose:** Lists all collections in the MongoDB database (equivalent to `SQLListTables`)

**Attributes:**
- `ReadOnly = true`
- `Idempotent = true`
- `Destructive = false`

**Parameters:** None

**Returns:** `OperationResult` with array of collection names

**Implementation:**
- Use `database.ListCollectionNamesAsync()` or `database.ListCollectionsAsync()`
- Return collection names as array of strings

**Example:**
```csharp
[McpServerTool(
    Title = "MongoDB: List Collections",
    ReadOnly = true,
    Idempotent = true,
    Destructive = false),
    Description("Lists all collections in the MongoDB database.")]
public async Task<OperationResult> MongoListCollections()
```

---

#### 2. `MongoDescribeCollection`
**Purpose:** Returns detailed schema information for a collection (equivalent to `SQLDescribeTable`)

**Attributes:**
- `ReadOnly = true`
- `Idempotent = true`
- `Destructive = false`

**Parameters:**
- `name` (string) - Collection name

**Returns:** `OperationResult` with collection metadata:
- Collection name
- Document count
- Indexes (name, keys, options)
- Validation rules (if any)
- Storage size
- Average document size
- Sample documents (first few) to infer schema

**Implementation:**
- Use `collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)`
- Use `collection.Indexes.ListAsync()` to get indexes
- Use `collection.Find(FilterDefinition<BsonDocument>.Empty).Limit(5).ToListAsync()` for sample documents
- Analyze sample documents to infer field types and structure

**Example:**
```csharp
[McpServerTool(
    Title = "MongoDB: Describe Collection",
    ReadOnly = true,
    Idempotent = true,
    Destructive = false),
    Description("Returns collection schema and metadata")]
public async Task<OperationResult> MongoDescribeCollection(
    [Description("Name of collection")] string name)
```

---

#### 3. `MongoReadData`
**Purpose:** Executes find queries to retrieve documents (equivalent to `SQLReadData`)

**Attributes:**
- `ReadOnly = true`
- `Idempotent = true`
- `Destructive = false`

**Parameters:**
- `collection` (string) - Collection name
- `filter` (string, optional) - JSON filter document (MongoDB query)
- `projection` (string, optional) - JSON projection document
- `sort` (string, optional) - JSON sort specification
- `limit` (int, optional) - Maximum number of documents to return
- `skip` (int, optional) - Number of documents to skip

**Returns:** `OperationResult` with array of documents

**Implementation:**
- Parse filter, projection, sort from JSON strings
- Use `collection.FindAsync(filter).Project(projection).Sort(sort).Skip(skip).Limit(limit)`
- Convert BSON documents to dictionaries/objects for serialization

**Alternative Approach (Simpler):**
- Accept a single `query` parameter as JSON string containing all options
- Parse the JSON to extract filter, projection, sort, limit, skip

**Example:**
```csharp
[McpServerTool(
    Title = "MongoDB: Read Data",
    ReadOnly = true,
    Idempotent = true,
    Destructive = false),
    Description("Executes find queries against MongoDB to read documents")]
public async Task<OperationResult> MongoReadData(
    [Description("Collection name")] string collection,
    [Description("JSON filter document (MongoDB query)")] string? filter = null,
    [Description("JSON projection document")] string? projection = null,
    [Description("JSON sort specification")] string? sort = null,
    [Description("Maximum number of documents to return")] int? limit = null,
    [Description("Number of documents to skip")] int? skip = null)
```

---

### Write Operations

#### 4. `MongoInsertData`
**Purpose:** Inserts new documents into a collection (equivalent to `SQLInsertData`)

**Attributes:**
- `ReadOnly = false`
- `Destructive = false`

**Parameters:**
- `collection` (string) - Collection name
- `documents` (string) - JSON array of documents to insert

**Returns:** `OperationResult` with number of documents inserted

**Implementation:**
- Parse JSON array of documents
- Use `collection.InsertManyAsync(documents)` or `InsertOneAsync` for single document
- Return count of inserted documents

**Example:**
```csharp
[McpServerTool(
    Title = "MongoDB: Insert Data",
    ReadOnly = false,
    Destructive = false),
    Description("Inserts documents into a MongoDB collection. Expects a JSON array of documents.")]
public async Task<OperationResult> MongoInsertData(
    [Description("Collection name")] string collection,
    [Description("JSON array of documents to insert")] string documents)
```

---

#### 5. `MongoUpdateData`
**Purpose:** Updates existing documents in a collection (equivalent to `SQLUpdateData`)

**Attributes:**
- `ReadOnly = false`
- `Destructive = true`

**Parameters:**
- `collection` (string) - Collection name
- `filter` (string) - JSON filter document to identify documents to update
- `update` (string) - JSON update document (using MongoDB update operators)
- `upsert` (bool, optional) - If true, insert document if no match found
- `multi` (bool, optional) - If true, update all matching documents (default: false for safety)

**Returns:** `OperationResult` with number of documents modified

**Implementation:**
- Parse filter and update from JSON
- Use `collection.UpdateOneAsync(filter, update, options)` or `UpdateManyAsync`
- Return count of modified documents

**Example:**
```csharp
[McpServerTool(
    Title = "MongoDB: Update Data",
    ReadOnly = false,
    Destructive = true),
    Description("Updates documents in a MongoDB collection. Expects JSON filter and update documents.")]
public async Task<OperationResult> MongoUpdateData(
    [Description("Collection name")] string collection,
    [Description("JSON filter document to identify documents")] string filter,
    [Description("JSON update document with MongoDB update operators")] string update,
    [Description("Insert document if no match found")] bool upsert = false,
    [Description("Update all matching documents")] bool multi = false)
```

---

#### 6. `MongoDeleteData`
**Purpose:** Deletes documents from a collection (new operation, no direct SQL equivalent but useful)

**Attributes:**
- `ReadOnly = false`
- `Destructive = true`

**Parameters:**
- `collection` (string) - Collection name
- `filter` (string) - JSON filter document to identify documents to delete
- `multi` (bool, optional) - If true, delete all matching documents (default: false for safety)

**Returns:** `OperationResult` with number of documents deleted

**Implementation:**
- Parse filter from JSON
- Use `collection.DeleteOneAsync(filter)` or `DeleteManyAsync`
- Return count of deleted documents

**Example:**
```csharp
[McpServerTool(
    Title = "MongoDB: Delete Data",
    ReadOnly = false,
    Destructive = true),
    Description("Deletes documents from a MongoDB collection. Expects a JSON filter document.")]
public async Task<OperationResult> MongoDeleteData(
    [Description("Collection name")] string collection,
    [Description("JSON filter document to identify documents")] string filter,
    [Description("Delete all matching documents")] bool multi = false)
```

---

#### 7. `MongoCreateCollection`
**Purpose:** Creates a new collection (equivalent to `SQLCreateTable`)

**Attributes:**
- `ReadOnly = false`
- `Destructive = false`

**Parameters:**
- `name` (string) - Collection name
- `options` (string, optional) - JSON string with collection options (capped, size, max, validator, etc.)

**Returns:** `OperationResult` with success status

**Implementation:**
- Parse options from JSON if provided
- Use `database.CreateCollectionAsync(name, options)`
- Handle case where collection already exists

**Example:**
```csharp
[McpServerTool(
    Title = "MongoDB: Create Collection",
    ReadOnly = false,
    Destructive = false),
    Description("Creates a new collection in the MongoDB database.")]
public async Task<OperationResult> MongoCreateCollection(
    [Description("Collection name")] string name,
    [Description("JSON string with collection options (capped, size, max, validator, etc.)")] string? options = null)
```

---

#### 8. `MongoDropCollection`
**Purpose:** Drops a collection (equivalent to `SQLDropTable`)

**Attributes:**
- `ReadOnly = false`
- `Destructive = true`

**Parameters:**
- `name` (string) - Collection name

**Returns:** `OperationResult` with success status

**Implementation:**
- Use `database.DropCollectionAsync(name)`
- Handle case where collection doesn't exist

**Example:**
```csharp
[McpServerTool(
    Title = "MongoDB: Drop Collection",
    ReadOnly = false,
    Destructive = true),
    Description("Drops a collection from the MongoDB database. This permanently deletes the collection and all its data.")]
public async Task<OperationResult> MongoDropCollection(
    [Description("Collection name")] string name)
```

---

## Additional Operations (Optional Enhancements)

### 9. `MongoCreateIndex`
**Purpose:** Creates indexes on collections

**Attributes:**
- `ReadOnly = false`
- `Destructive = false`

**Parameters:**
- `collection` (string) - Collection name
- `keys` (string) - JSON document specifying index keys
- `options` (string, optional) - JSON string with index options (unique, sparse, etc.)

---

### 10. `MongoListIndexes`
**Purpose:** Lists all indexes on a collection

**Attributes:**
- `ReadOnly = true`
- `Idempotent = true`
- `Destructive = false`

**Parameters:**
- `collection` (string) - Collection name

---

## Technical Implementation Details

### MongoDB C# Driver

**NuGet Package:** `MongoDB.Driver`

**Key Classes:**
- `MongoClient` - MongoDB client
- `IMongoDatabase` - Database interface
- `IMongoCollection<T>` - Collection interface
- `FilterDefinition<T>` - Query filters
- `UpdateDefinition<T>` - Update operations
- `BsonDocument` - BSON document representation

### Connection String Format

**Environment Variable:** `MONGODB_CONNECTION_STRING`

**Format Examples:**
```
mongodb://localhost:27017
mongodb://username:password@localhost:27017
mongodb://username:password@host1:27017,host2:27017/database?replicaSet=rs0
mongodb+srv://username:password@cluster.mongodb.net/database
```

**Connection String Parsing:**
- Extract database name from connection string
- If not specified, use default database or require it as parameter

### JSON Parsing

**Approach:**
- Use `BsonDocument.Parse(jsonString)` to parse JSON filter/update documents
- Use `JsonSerializer.Deserialize<T>()` for structured data
- Handle JSON parsing errors gracefully

### Error Handling

**Common Errors to Handle:**
- Connection failures
- Invalid JSON in filter/update documents
- Collection not found
- Duplicate key errors (unique indexes)
- Validation errors
- Network timeouts

**Error Response Format:**
```csharp
return new OperationResult(success: false, error: ex.Message);
```

### Logging

**Log Events:**
- Connection attempts
- Query executions
- Write operations (with appropriate log levels)
- Errors with full exception details

**Example:**
```csharp
_logger.LogError(ex, "MongoReadData failed: {Message}", ex.Message);
```

## Service Registration

Add to `Program.cs`:

```csharp
// Register MongoDB Tool dependencies
builder.Services.AddSingleton<IMongoConnectionFactory, MongoConnectionFactory>();
builder.Services.AddSingleton<MongoDBTool>();
```

## Testing Strategy

### Unit Tests

Create `MongoDBToolTests.cs` in `DrSasuMcp.Tests/MongoDB/`:

**Test Cases:**
1. Tool instantiation
2. ListCollections - success and error cases
3. DescribeCollection - with and without documents
4. ReadData - various filter combinations
5. InsertData - single and multiple documents
6. UpdateData - single and multiple updates
7. DeleteData - single and multiple deletes
8. CreateCollection - success and duplicate
9. DropCollection - success and not found

**Mocking:**
- Mock `IMongoConnectionFactory`
- Mock `IMongoDatabase` and `IMongoCollection<T>`
- Use `Moq` library (same as SQL tool tests)

## Documentation

### README.md Structure

Follow the SQL tool README structure:

1. **Overview** - What the tool does
2. **Features** - List of capabilities
3. **Architecture** - File structure
4. **MCP Exposed Methods** - Detailed method documentation
   - Parameters
   - Returns
   - Example usage
   - Safety flags
5. **Configuration** - Environment variable setup
6. **Connection String Examples** - Various scenarios
7. **Operation Result Format** - Response structure
8. **Usage Examples** - Common scenarios
9. **Safety Features** - Operation flags table
10. **Error Handling** - Common errors and solutions
11. **Advanced Features** - Schema inference, etc.
12. **Dependencies** - Required NuGet packages
13. **Integration** - Service registration
14. **Best Practices** - Usage recommendations
15. **Troubleshooting** - Common issues
16. **Security Notes** - Important considerations

## Differences from SQL Tool

### 1. Schema-less Nature
- MongoDB is schema-less, so `DescribeCollection` will infer schema from sample documents
- No fixed column definitions - fields can vary between documents

### 2. Query Language
- Uses MongoDB query syntax (JSON) instead of SQL
- Filter, projection, and update operations are JSON documents

### 3. Document-based Operations
- Operations work with documents (JSON objects) rather than rows
- Insert/Update operations accept JSON arrays/objects

### 4. Indexes
- Indexes are more prominent in MongoDB (important for performance)
- May want to expose index management operations

### 5. No Transactions by Default
- MongoDB supports transactions, but they're not as central as in SQL
- Consider adding transaction support for multi-document operations

## Implementation Steps

### Phase 1: Core Infrastructure
1. ✅ Create `MongoDB` folder in `Tools/`
2. ✅ Create `IMongoConnectionFactory.cs` interface
3. ✅ Create `MongoConnectionFactory.cs` implementation
4. ✅ Add MongoDB.Driver NuGet package

### Phase 2: Read Operations
5. ✅ Create `MongoDBTool.cs` with basic structure
6. ✅ Implement `MongoListCollections`
7. ✅ Implement `MongoDescribeCollection`
8. ✅ Implement `MongoReadData`

### Phase 3: Write Operations
9. ✅ Implement `MongoInsertData`
10. ✅ Implement `MongoUpdateData`
11. ✅ Implement `MongoDeleteData`

### Phase 4: Collection Management
12. ✅ Implement `MongoCreateCollection`
13. ✅ Implement `MongoDropCollection`

### Phase 5: Testing & Documentation
14. ✅ Create unit tests
15. ✅ Write comprehensive README.md
16. ✅ Register services in Program.cs
17. ✅ Test end-to-end with real MongoDB instance

### Phase 6: Optional Enhancements
18. ⏳ Implement `MongoCreateIndex`
19. ⏳ Implement `MongoListIndexes`
20. ⏳ Add aggregation pipeline support
21. ⏳ Add transaction support

## Dependencies

### Required NuGet Packages
- `MongoDB.Driver` - Official MongoDB C# driver
- `Microsoft.Extensions.Logging` - Logging (already in project)
- `ModelContextProtocol.Server` - MCP server attributes (already in project)

### Optional Packages
- `Newtonsoft.Json` - If needed for additional JSON handling (may already be in project)

## Configuration

### Environment Variable

**Variable Name:** `MONGODB_CONNECTION_STRING`

**Example:**
```powershell
$env:MONGODB_CONNECTION_STRING = "mongodb://localhost:27017/test"
```

**Connection String Components:**
- `mongodb://` or `mongodb+srv://` - Protocol
- `username:password@` - Authentication (optional)
- `host:port` - Server address
- `/database` - Database name
- `?options` - Additional options

## Security Considerations

1. **Connection String Security**
   - Never commit connection strings with credentials
   - Use environment variables or secure configuration

2. **Query Injection**
   - MongoDB queries are JSON, which reduces SQL injection risk
   - Still validate JSON input to prevent malformed queries
   - Consider input sanitization for user-provided filters

3. **Permissions**
   - Grant minimal necessary permissions to MongoDB user
   - Use read-only user for read operations if possible

4. **Network Security**
   - Use TLS/SSL for remote connections
   - Use MongoDB Atlas connection strings for cloud deployments

5. **Destructive Operations**
   - Mark destructive operations appropriately
   - Consider adding confirmation for drop operations

## Performance Considerations

1. **Connection Pooling**
   - MongoDB driver handles connection pooling automatically
   - Default pool size is usually sufficient

2. **Query Optimization**
   - Encourage use of indexes in filters
   - Limit result sets with `limit` parameter
   - Use projections to reduce data transfer

3. **Bulk Operations**
   - Support bulk inserts/updates for efficiency
   - Consider adding bulk operation methods

## Future Enhancements

1. **Aggregation Pipeline Support**
   - Add method to execute aggregation pipelines
   - Useful for complex data transformations

2. **Change Streams**
   - Support for real-time change monitoring
   - Advanced feature for event-driven scenarios

3. **GridFS Support**
   - File storage operations
   - Useful for large file handling

4. **Multi-Database Support**
   - Allow switching between databases
   - Add database parameter to operations

5. **Schema Validation**
   - Support for JSON Schema validation
   - Help enforce document structure

## Success Criteria

✅ All read operations work correctly
✅ All write operations work correctly
✅ Error handling is comprehensive
✅ Documentation is complete and clear
✅ Unit tests cover all methods
✅ Integration with MCP server works
✅ Follows same patterns as SQL tool
✅ README provides clear usage examples

---

**Ready to implement!** This plan provides a comprehensive roadmap for building a MongoDB tool that mirrors the SQL tool's functionality while adapting to MongoDB's document-based architecture.

