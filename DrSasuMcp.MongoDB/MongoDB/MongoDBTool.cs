using DrSasuMcp.Common.Models;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace DrSasuMcp.MongoDB.MongoDB
{
    [McpServerToolType]
    public partial class MongoDBTool(IMongoConnectionFactory connectionFactory, ILogger<MongoDBTool> logger)
    {
        private readonly IMongoConnectionFactory _connectionFactory = connectionFactory;
        private readonly ILogger<MongoDBTool> _logger = logger;

        #region Read-Only Operations

        [McpServerTool(
            Title = "MongoDB: List Collections",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Lists all collections in the MongoDB database.")]
        public async Task<OperationResult> MongoListCollections()
        {
            try
            {
                var database = await _connectionFactory.GetDatabaseAsync();
                var collections = await database.ListCollectionNamesAsync();
                var collectionList = await collections.ToListAsync();

                return new OperationResult(success: true, data: collectionList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoListCollections failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
            Title = "MongoDB: Describe Collection",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Returns collection schema and metadata including indexes, document count, and sample documents.")]
        public async Task<OperationResult> MongoDescribeCollection(
            [Description("Name of collection")] string name)
        {
            try
            {
                var database = await _connectionFactory.GetDatabaseAsync();
                var collection = database.GetCollection<BsonDocument>(name);

                var result = new Dictionary<string, object>();

                // Check if collection exists by trying to get stats
                long count = 0;
                long size = 0;
                long storageSize = 0;
                long avgObjSize = 0;
                
                try
                {
                    var stats = await database.RunCommandAsync<BsonDocument>(
                        new BsonDocument("collStats", name));

                    count = stats.Contains("count") ? stats["count"].AsInt64 : 0;
                    size = stats.Contains("size") ? stats["size"].AsInt64 : 0;
                    storageSize = stats.Contains("storageSize") ? stats["storageSize"].AsInt64 : 0;
                    avgObjSize = stats.Contains("avgObjSize") ? stats["avgObjSize"].AsInt64 : 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, 
                        "collStats command failed for collection {CollectionName}. Error: {ErrorMessage}. " +
                        "This is often due to: 1) Insufficient permissions (user needs 'collStats' privilege), " +
                        "2) MongoDB Atlas/managed service restrictions, or 3) Command syntax issues. " +
                        "Falling back to CountDocumentsAsync.", 
                        name, ex.Message);
                    // Fallback to CountDocumentsAsync if collStats fails
                    try
                    {
                        count = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
                    }
                    catch (Exception countEx)
                    {
                        _logger.LogError(countEx, "Failed to get document count for collection {CollectionName}", name);
                    }
                }

                result["collection"] = new
                {
                    name,
                    count,
                    size,
                    storageSize,
                    avgObjSize
                };

                // Get indexes
                var indexes = new List<object>();
                try
                {
                    var indexList = await collection.Indexes.ListAsync();
                    var indexListAsync = await indexList.ToListAsync();

                    foreach (var index in indexListAsync)
                    {
                        var indexDoc = index;
                        Dictionary<string, object> keysDict = new Dictionary<string, object>();
                        if (indexDoc.Contains("key") && indexDoc["key"].IsBsonDocument)
                        {
                            var keysDoc = indexDoc["key"].AsBsonDocument;
                            foreach (var element in keysDoc.Elements)
                            {
                                keysDict[element.Name] = ConvertBsonValue(element.Value);
                            }
                        }
                        
                        indexes.Add(new
                        {
                            name = indexDoc["name"].AsString,
                            keys = keysDict,
                            unique = indexDoc.Contains("unique") && indexDoc["unique"].AsBoolean,
                            sparse = indexDoc.Contains("sparse") && indexDoc["sparse"].AsBoolean,
                            background = indexDoc.Contains("background") && indexDoc["background"].AsBoolean
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve indexes for collection {CollectionName}", name);
                }

                result["indexes"] = indexes;

                // Get sample documents to infer schema
                var sampleDocuments = new List<object>();
                try
                {
                    var samples = await collection.Find(FilterDefinition<BsonDocument>.Empty)
                        .Limit(5)
                        .ToListAsync();

                    foreach (var doc in samples)
                    {
                        sampleDocuments.Add(ConvertBsonToDictionary(doc));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve sample documents for collection {CollectionName}", name);
                }

                result["sampleDocuments"] = sampleDocuments;

                // Infer schema from sample documents
                if (sampleDocuments.Count > 0)
                {
                    var inferredFields = InferSchemaFromSamples(sampleDocuments);
                    result["inferredSchema"] = inferredFields;
                }

                return new OperationResult(success: true, data: result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoDescribeCollection failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
            Title = "MongoDB: Read Data",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Executes find queries against MongoDB to read documents. Supports filter, projection, sort, limit, and skip.")]
        public async Task<OperationResult> MongoReadData(
            [Description("Collection name")] string collection,
            [Description("JSON filter document (MongoDB query). Example: {\"age\": {\"$gt\": 25}}")] string? filter = null,
            [Description("JSON projection document. Example: {\"name\": 1, \"age\": 1}")] string? projection = null,
            [Description("JSON sort specification. Example: {\"age\": -1}")] string? sort = null,
            [Description("Maximum number of documents to return")] int? limit = null,
            [Description("Number of documents to skip")] int? skip = null)
        {
            try
            {
                var database = await _connectionFactory.GetDatabaseAsync();
                var coll = database.GetCollection<BsonDocument>(collection);

                FilterDefinition<BsonDocument> filterDef = FilterDefinition<BsonDocument>.Empty;
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    filterDef = new JsonFilterDefinition<BsonDocument>(filter);
                }

                var findOptions = new FindOptions<BsonDocument>();

                if (!string.IsNullOrWhiteSpace(projection))
                {
                    findOptions.Projection = new JsonProjectionDefinition<BsonDocument>(projection);
                }

                if (!string.IsNullOrWhiteSpace(sort))
                {
                    findOptions.Sort = new JsonSortDefinition<BsonDocument>(sort);
                }

                if (limit.HasValue)
                {
                    findOptions.Limit = limit.Value;
                }

                if (skip.HasValue)
                {
                    findOptions.Skip = skip.Value;
                }

                var cursor = await coll.FindAsync(filterDef, findOptions);
                var documents = await cursor.ToListAsync();

                var results = documents.Select(doc => ConvertBsonToDictionary(doc)).ToList();

                return new OperationResult(success: true, data: results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoReadData failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        #endregion

        #region Write Operations

        [McpServerTool(
            Title = "MongoDB: Insert Data",
            ReadOnly = false,
            Destructive = false),
            Description("Inserts documents into a MongoDB collection. Expects a JSON array of documents.")]
        public async Task<OperationResult> MongoInsertData(
            [Description("Collection name")] string collection,
            [Description("JSON array of documents to insert. Example: [{\"name\": \"John\", \"age\": 30}, {\"name\": \"Jane\", \"age\": 25}]")] string documents)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(collection))
                {
                    return new OperationResult(success: false, error: "Collection name cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(documents))
                {
                    return new OperationResult(success: false, error: "Documents cannot be empty.");
                }

                var database = await _connectionFactory.GetDatabaseAsync();
                var coll = database.GetCollection<BsonDocument>(collection);

                // Parse JSON array string into BsonArray
                // Wrap the array in a document to parse it, then extract the array
                BsonArray docsArray;
                try
                {
                    var wrapperDoc = BsonDocument.Parse($"{{ \"docs\": {documents} }}");
                    docsArray = wrapperDoc["docs"].AsBsonArray;
                }
                catch (Exception parseEx)
                {
                    _logger.LogError(parseEx, "Failed to parse documents JSON");
                    return new OperationResult(success: false, error: $"Invalid JSON format: {parseEx.Message}");
                }

                var bsonDocs = docsArray.Select(d => d.AsBsonDocument).ToList();

                if (bsonDocs.Count == 0)
                {
                    return new OperationResult(success: false, error: "No documents to insert.");
                }

                if (bsonDocs.Count == 1)
                {
                    await coll.InsertOneAsync(bsonDocs[0]);
                    return new OperationResult(success: true, rowsAffected: 1);
                }
                else
                {
                    await coll.InsertManyAsync(bsonDocs);
                    return new OperationResult(success: true, rowsAffected: bsonDocs.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoInsertData failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
            Title = "MongoDB: Update Data",
            ReadOnly = false,
            Destructive = true),
            Description("Updates documents in a MongoDB collection. Expects JSON filter and update documents with MongoDB update operators.")]
        public async Task<OperationResult> MongoUpdateData(
            [Description("Collection name")] string collection,
            [Description("JSON filter document to identify documents. Example: {\"name\": \"John\"}")] string filter,
            [Description("JSON update document with MongoDB update operators. Example: {\"$set\": {\"age\": 31}}")] string update,
            [Description("Insert document if no match found")] bool upsert = false,
            [Description("Update all matching documents (default: false for safety)")] bool multi = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(collection))
                {
                    return new OperationResult(success: false, error: "Collection name cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(filter))
                {
                    return new OperationResult(success: false, error: "Filter cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(update))
                {
                    return new OperationResult(success: false, error: "Update document cannot be empty.");
                }

                var database = await _connectionFactory.GetDatabaseAsync();
                var coll = database.GetCollection<BsonDocument>(collection);

                var filterDef = new JsonFilterDefinition<BsonDocument>(filter);
                var updateDef = new JsonUpdateDefinition<BsonDocument>(update);

                UpdateOptions options = new UpdateOptions { IsUpsert = upsert };

                long modifiedCount = 0;
                if (multi)
                {
                    var result = await coll.UpdateManyAsync(filterDef, updateDef, options);
                    modifiedCount = result.ModifiedCount;
                }
                else
                {
                    var result = await coll.UpdateOneAsync(filterDef, updateDef, options);
                    modifiedCount = result.ModifiedCount;
                }

                return new OperationResult(success: true, rowsAffected: (int)modifiedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoUpdateData failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
            Title = "MongoDB: Delete Data",
            ReadOnly = false,
            Destructive = true),
            Description("Deletes documents from a MongoDB collection. Expects a JSON filter document.")]
        public async Task<OperationResult> MongoDeleteData(
            [Description("Collection name")] string collection,
            [Description("JSON filter document to identify documents. Example: {\"age\": {\"$lt\": 18}}")] string filter,
            [Description("Delete all matching documents (default: false for safety)")] bool multi = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(collection))
                {
                    return new OperationResult(success: false, error: "Collection name cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(filter))
                {
                    return new OperationResult(success: false, error: "Filter cannot be empty.");
                }

                var database = await _connectionFactory.GetDatabaseAsync();
                var coll = database.GetCollection<BsonDocument>(collection);

                var filterDef = new JsonFilterDefinition<BsonDocument>(filter);

                long deletedCount = 0;
                if (multi)
                {
                    var result = await coll.DeleteManyAsync(filterDef);
                    deletedCount = result.DeletedCount;
                }
                else
                {
                    var result = await coll.DeleteOneAsync(filterDef);
                    deletedCount = result.DeletedCount;
                }

                return new OperationResult(success: true, rowsAffected: (int)deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoDeleteData failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
            Title = "MongoDB: Create Collection",
            ReadOnly = false,
            Destructive = false),
            Description("Creates a new collection in the MongoDB database. Optionally accepts collection options.")]
        public async Task<OperationResult> MongoCreateCollection(
            [Description("Collection name")] string name,
            [Description("JSON string with collection options (capped, size, max). Example: {\"capped\": true, \"size\": 1000000}")] string? options = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return new OperationResult(success: false, error: "Collection name cannot be empty.");
                }

                var database = await _connectionFactory.GetDatabaseAsync();

                CreateCollectionOptions? createOptions = null;
                if (!string.IsNullOrWhiteSpace(options))
                {
                    try
                    {
                        var optionsDoc = BsonDocument.Parse(options);
                        createOptions = new CreateCollectionOptions();

                        if (optionsDoc.Contains("capped"))
                            createOptions.Capped = optionsDoc["capped"].AsBoolean;

                        if (optionsDoc.Contains("size"))
                        {
                            // Handle both Int32 and Int64
                            var sizeValue = optionsDoc["size"];
                            createOptions.MaxSize = sizeValue.BsonType == BsonType.Int32 
                                ? sizeValue.AsInt32 
                                : sizeValue.AsInt64;
                        }

                        if (optionsDoc.Contains("max"))
                            createOptions.MaxDocuments = optionsDoc["max"].AsInt32;
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogError(parseEx, "Failed to parse collection options JSON");
                        return new OperationResult(success: false, error: $"Invalid options JSON format: {parseEx.Message}");
                    }
                }

                await database.CreateCollectionAsync(name, createOptions);
                return new OperationResult(success: true);
            }
            catch (MongoCommandException ex) when (ex.CodeName == "NamespaceExists")
            {
                _logger.LogWarning("Collection {CollectionName} already exists", name);
                return new OperationResult(success: false, error: $"Collection '{name}' already exists.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoCreateCollection failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
            Title = "MongoDB: Drop Collection",
            ReadOnly = false,
            Destructive = true),
            Description("Drops a collection from the MongoDB database. This permanently deletes the collection and all its data.")]
        public async Task<OperationResult> MongoDropCollection(
            [Description("Collection name")] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return new OperationResult(success: false, error: "Collection name cannot be empty.");
                }

                var database = await _connectionFactory.GetDatabaseAsync();
                await database.DropCollectionAsync(name);
                return new OperationResult(success: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoDropCollection failed: {Message}", ex.Message);
                return new OperationResult(success: false, error: ex.Message);
            }
        }

        #endregion

        #region Private Helper Methods

        private static Dictionary<string, object> ConvertBsonToDictionary(BsonDocument doc)
        {
            var result = new Dictionary<string, object>();
            foreach (var element in doc.Elements)
            {
                result[element.Name] = ConvertBsonValue(element.Value);
            }
            return result;
        }

        private static object ConvertBsonValue(BsonValue value)
        {
            return value.BsonType switch
            {
                BsonType.Null => null!,
                BsonType.Boolean => value.AsBoolean,
                BsonType.Int32 => value.AsInt32,
                BsonType.Int64 => value.AsInt64,
                BsonType.Double => value.AsDouble,
                BsonType.Decimal128 => Decimal128.ToDecimal(value.AsDecimal128),
                BsonType.String => value.AsString,
                BsonType.DateTime => value.ToUniversalTime(),
                BsonType.ObjectId => value.AsObjectId.ToString(),
                BsonType.Array => value.AsBsonArray.Select(v => ConvertBsonValue(v)).ToList(),
                BsonType.Document => ConvertBsonToDictionary(value.AsBsonDocument),
                _ => value.ToString()
            };
        }

        private static List<Dictionary<string, object>> InferSchemaFromSamples(List<object> samples)
        {
            var fieldTypes = new Dictionary<string, HashSet<string>>();

            foreach (var sample in samples)
            {
                if (sample is Dictionary<string, object> doc)
                {
                    foreach (var kvp in doc)
                    {
                        if (!fieldTypes.ContainsKey(kvp.Key))
                        {
                            fieldTypes[kvp.Key] = new HashSet<string>();
                        }

                        var typeName = kvp.Value?.GetType().Name ?? "Null";
                        fieldTypes[kvp.Key].Add(typeName);
                    }
                }
            }

            var inferredFields = new List<Dictionary<string, object>>();
            foreach (var field in fieldTypes)
            {
                inferredFields.Add(new Dictionary<string, object>
                {
                    ["name"] = field.Key,
                    ["types"] = field.Value.ToList(),
                    ["nullable"] = field.Value.Contains("Null")
                });
            }

            return inferredFields;
        }

        #endregion
    }
}

