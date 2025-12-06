# MongoDB Tool - Quick Reference

## File Structure
```
DrSasuMcp/Tools/MongoDB/
├── MongoDBTool.cs                # Main MCP tool (8 methods)
├── MongoDBToolConstant.cs        # Constants (if needed)
├── IMongoConnectionFactory.cs    # Connection factory interface
├── MongoConnectionFactory.cs     # Connection factory implementation
└── README.md                     # Full documentation
```

## MCP Methods (8 Core Operations)

### Read-Only (3)
1. **MongoListCollections** - List all collections
2. **MongoDescribeCollection** - Get collection schema/metadata
3. **MongoReadData** - Execute find queries

### Write Operations (5)
4. **MongoInsertData** - Insert documents
5. **MongoUpdateData** - Update documents (destructive)
6. **MongoDeleteData** - Delete documents (destructive)
7. **MongoCreateCollection** - Create collection
8. **MongoDropCollection** - Drop collection (destructive)

## Key Differences from SQL Tool

| Aspect | SQL Tool | MongoDB Tool |
|--------|----------|--------------|
| **Query Language** | SQL strings | JSON filter/update documents |
| **Schema** | Fixed tables/columns | Schema-less (inferred from samples) |
| **Data Format** | Rows/columns | Documents (JSON) |
| **Connection** | `SqlConnection` | `IMongoDatabase` |
| **Environment Var** | `SQL_CONNECTION_STRING` | `MONGODB_CONNECTION_STRING` |

## Dependencies
- `MongoDB.Driver` NuGet package
- `Microsoft.Extensions.Logging`
- `ModelContextProtocol.Server`

## Service Registration
```csharp
builder.Services.AddSingleton<IMongoConnectionFactory, MongoConnectionFactory>();
builder.Services.AddSingleton<MongoDBTool>();
```

## Connection String Format
```
mongodb://localhost:27017/database
mongodb://user:pass@host:port/database
mongodb+srv://user:pass@cluster.mongodb.net/database
```

## Implementation Order
1. Connection factory (interface + implementation)
2. Read operations (List, Describe, Read)
3. Write operations (Insert, Update, Delete)
4. Collection management (Create, Drop)
5. Tests & Documentation

See `MONGODB_IMPLEMENTATION_PLAN.md` for full details.

