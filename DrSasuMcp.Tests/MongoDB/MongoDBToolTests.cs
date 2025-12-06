using DrSasuMcp.Tools;
using DrSasuMcp.Tools.MongoDB;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DrSasuMcp.Tests.MongoDB
{
    public class MongoDBToolTests
    {
        private readonly Mock<IMongoConnectionFactory> _mockConnectionFactory;
        private readonly Mock<ILogger<MongoDBTool>> _mockLogger;
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly MongoDBTool _mongoTool;

        public MongoDBToolTests()
        {
            _mockConnectionFactory = new Mock<IMongoConnectionFactory>();
            _mockLogger = new Mock<ILogger<MongoDBTool>>();
            _mockDatabase = new Mock<IMongoDatabase>();

            _mockConnectionFactory
                .Setup(x => x.GetDatabaseAsync())
                .ReturnsAsync(_mockDatabase.Object);

            _mongoTool = new MongoDBTool(_mockConnectionFactory.Object, _mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithDependencies_ShouldCreateInstance()
        {
            // Assert
            _mongoTool.Should().NotBeNull();
        }

        [Fact]
        public void MongoDBTool_Instantiation_ShouldSucceed()
        {
            // Arrange & Act
            var tool = new MongoDBTool(_mockConnectionFactory.Object, _mockLogger.Object);

            // Assert
            tool.Should().NotBeNull();
            tool.Should().BeOfType<MongoDBTool>();
        }

        #endregion

        #region MongoListCollections Tests

        [Fact]
        public async Task MongoListCollections_WithCollections_ShouldReturnList()
        {
            // Arrange
            var collectionNames = new List<string> { "users", "products", "orders" };
            var mockCursor = new Mock<IAsyncCursor<string>>();
            
            mockCursor.Setup(x => x.Current).Returns(collectionNames);
            mockCursor.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockDatabase
                .Setup(x => x.ListCollectionNamesAsync(It.IsAny<ListCollectionNamesOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _mongoTool.MongoListCollections();

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().BeOfType<List<string>>();
        }

        [Fact]
        public async Task MongoListCollections_OnException_ShouldReturnError()
        {
            // Arrange
            _mockDatabase
                .Setup(x => x.ListCollectionNamesAsync(It.IsAny<ListCollectionNamesOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new MongoException("Connection failed"));

            // Act
            var result = await _mongoTool.MongoListCollections();

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Connection failed");
        }

        #endregion

        #region MongoReadData Tests

        [Fact]
        public async Task MongoReadData_WithValidFilter_ShouldReturnDocuments()
        {
            // Arrange
            var collectionName = "users";
            var filter = "{\"age\": {\"$gt\": 25}}";
            
            var mockCollection = new Mock<IMongoCollection<BsonDocument>>();
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            
            var documents = new List<BsonDocument>
            {
                new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "name", "John" }, { "age", 30 } },
                new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "name", "Jane" }, { "age", 28 } }
            };

            mockCursor.Setup(x => x.Current).Returns(documents);
            mockCursor.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            mockCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<FindOptions<BsonDocument, BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            _mockDatabase
                .Setup(x => x.GetCollection<BsonDocument>(collectionName, null))
                .Returns(mockCollection.Object);

            // Act
            var result = await _mongoTool.MongoReadData(collectionName, filter);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task MongoReadData_WithProjectionAndSort_ShouldApplyOptions()
        {
            // Arrange
            var collectionName = "users";
            var projection = "{\"name\": 1, \"age\": 1}";
            var sort = "{\"age\": -1}";
            var limit = 10;
            var skip = 5;
            
            var mockCollection = new Mock<IMongoCollection<BsonDocument>>();
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            
            mockCursor.Setup(x => x.Current).Returns(new List<BsonDocument>());
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            mockCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<FindOptions<BsonDocument, BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            _mockDatabase
                .Setup(x => x.GetCollection<BsonDocument>(collectionName, null))
                .Returns(mockCollection.Object);

            // Act
            var result = await _mongoTool.MongoReadData(collectionName, null, projection, sort, limit, skip);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }

        #endregion

        #region MongoInsertData Tests

        [Fact]
        public async Task MongoInsertData_WithEmptyCollectionName_ShouldReturnError()
        {
            // Act
            var result = await _mongoTool.MongoInsertData("", "[{\"name\": \"John\"}]");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Collection name cannot be empty");
        }

        [Fact]
        public async Task MongoInsertData_WithEmptyDocuments_ShouldReturnError()
        {
            // Act
            var result = await _mongoTool.MongoInsertData("users", "");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Documents cannot be empty");
        }

        [Fact]
        public async Task MongoInsertData_WithInvalidJson_ShouldReturnError()
        {
            // Arrange
            var mockCollection = new Mock<IMongoCollection<BsonDocument>>();
            _mockDatabase
                .Setup(x => x.GetCollection<BsonDocument>("users", null))
                .Returns(mockCollection.Object);

            // Act
            var result = await _mongoTool.MongoInsertData("users", "invalid json");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Invalid JSON format");
        }

        [Fact]
        public async Task MongoInsertData_WithSingleDocument_ShouldCallInsertOne()
        {
            // Arrange
            var mockCollection = new Mock<IMongoCollection<BsonDocument>>();
            _mockDatabase
                .Setup(x => x.GetCollection<BsonDocument>("users", null))
                .Returns(mockCollection.Object);

            mockCollection
                .Setup(x => x.InsertOneAsync(
                    It.IsAny<BsonDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _mongoTool.MongoInsertData("users", "[{\"name\": \"John\", \"age\": 30}]");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.RowsAffected.Should().Be(1);
            mockCollection.Verify(x => x.InsertOneAsync(
                It.IsAny<BsonDocument>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MongoInsertData_WithMultipleDocuments_ShouldCallInsertMany()
        {
            // Arrange
            var mockCollection = new Mock<IMongoCollection<BsonDocument>>();
            _mockDatabase
                .Setup(x => x.GetCollection<BsonDocument>("users", null))
                .Returns(mockCollection.Object);

            mockCollection
                .Setup(x => x.InsertManyAsync(
                    It.IsAny<IEnumerable<BsonDocument>>(),
                    It.IsAny<InsertManyOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _mongoTool.MongoInsertData("users", "[{\"name\": \"John\"}, {\"name\": \"Jane\"}]");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.RowsAffected.Should().Be(2);
            mockCollection.Verify(x => x.InsertManyAsync(
                It.IsAny<IEnumerable<BsonDocument>>(),
                It.IsAny<InsertManyOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region MongoUpdateData Tests

        [Fact]
        public async Task MongoUpdateData_WithEmptyCollectionName_ShouldReturnError()
        {
            // Act
            var result = await _mongoTool.MongoUpdateData("", "{\"name\": \"John\"}", "{\"$set\": {\"age\": 31}}");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Collection name cannot be empty");
        }

        [Fact]
        public async Task MongoUpdateData_WithEmptyFilter_ShouldReturnError()
        {
            // Act
            var result = await _mongoTool.MongoUpdateData("users", "", "{\"$set\": {\"age\": 31}}");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Filter cannot be empty");
        }

        [Fact]
        public async Task MongoUpdateData_WithEmptyUpdate_ShouldReturnError()
        {
            // Act
            var result = await _mongoTool.MongoUpdateData("users", "{\"name\": \"John\"}", "");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Update document cannot be empty");
        }

        [Fact]
        public async Task MongoUpdateData_WithMultiFalse_ShouldCallUpdateOne()
        {
            // Arrange
            var mockCollection = new Mock<IMongoCollection<BsonDocument>>();
            var updateResult = new UpdateResult.Acknowledged(1, 1, null);

            _mockDatabase
                .Setup(x => x.GetCollection<BsonDocument>("users", null))
                .Returns(mockCollection.Object);

            mockCollection
                .Setup(x => x.UpdateOneAsync(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<UpdateDefinition<BsonDocument>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(updateResult);

            // Act
            var result = await _mongoTool.MongoUpdateData("users", "{\"name\": \"John\"}", "{\"$set\": {\"age\": 31}}", false, false);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.RowsAffected.Should().Be(1);
            mockCollection.Verify(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<BsonDocument>>(),
                It.IsAny<UpdateDefinition<BsonDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MongoUpdateData_WithMultiTrue_ShouldCallUpdateMany()
        {
            // Arrange
            var mockCollection = new Mock<IMongoCollection<BsonDocument>>();
            var updateResult = new UpdateResult.Acknowledged(5, 5, null);

            _mockDatabase
                .Setup(x => x.GetCollection<BsonDocument>("users", null))
                .Returns(mockCollection.Object);

            mockCollection
                .Setup(x => x.UpdateManyAsync(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<UpdateDefinition<BsonDocument>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(updateResult);

            // Act
            var result = await _mongoTool.MongoUpdateData("users", "{\"status\": \"active\"}", "{\"$set\": {\"verified\": true}}", false, true);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.RowsAffected.Should().Be(5);
            mockCollection.Verify(x => x.UpdateManyAsync(
                It.IsAny<FilterDefinition<BsonDocument>>(),
                It.IsAny<UpdateDefinition<BsonDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region MongoDeleteData Tests

        [Fact]
        public async Task MongoDeleteData_WithEmptyCollectionName_ShouldReturnError()
        {
            // Act
            var result = await _mongoTool.MongoDeleteData("", "{\"age\": {\"$lt\": 18}}");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Collection name cannot be empty");
        }

        [Fact]
        public async Task MongoDeleteData_WithEmptyFilter_ShouldReturnError()
        {
            // Act
            var result = await _mongoTool.MongoDeleteData("users", "");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Filter cannot be empty");
        }

        [Fact]
        public async Task MongoDeleteData_WithMultiFalse_ShouldCallDeleteOne()
        {
            // Arrange
            var mockCollection = new Mock<IMongoCollection<BsonDocument>>();
            var deleteResult = new DeleteResult.Acknowledged(1);

            _mockDatabase
                .Setup(x => x.GetCollection<BsonDocument>("users", null))
                .Returns(mockCollection.Object);

            mockCollection
                .Setup(x => x.DeleteOneAsync(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(deleteResult);

            // Act
            var result = await _mongoTool.MongoDeleteData("users", "{\"_id\": \"123\"}", false);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.RowsAffected.Should().Be(1);
            mockCollection.Verify(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<BsonDocument>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MongoDeleteData_WithMultiTrue_ShouldCallDeleteMany()
        {
            // Arrange
            var mockCollection = new Mock<IMongoCollection<BsonDocument>>();
            var deleteResult = new DeleteResult.Acknowledged(10);

            _mockDatabase
                .Setup(x => x.GetCollection<BsonDocument>("users", null))
                .Returns(mockCollection.Object);

            mockCollection
                .Setup(x => x.DeleteManyAsync(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(deleteResult);

            // Act
            var result = await _mongoTool.MongoDeleteData("users", "{\"age\": {\"$lt\": 18}}", true);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.RowsAffected.Should().Be(10);
            mockCollection.Verify(x => x.DeleteManyAsync(
                It.IsAny<FilterDefinition<BsonDocument>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region MongoCreateCollection Tests

        [Fact]
        public async Task MongoCreateCollection_WithEmptyName_ShouldReturnError()
        {
            // Act
            var result = await _mongoTool.MongoCreateCollection("");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Collection name cannot be empty");
        }

        [Fact]
        public async Task MongoCreateCollection_WithValidName_ShouldSucceed()
        {
            // Arrange
            _mockDatabase
                .Setup(x => x.CreateCollectionAsync(
                    "test_collection",
                    It.IsAny<CreateCollectionOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _mongoTool.MongoCreateCollection("test_collection");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            _mockDatabase.Verify(x => x.CreateCollectionAsync(
                "test_collection",
                It.IsAny<CreateCollectionOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MongoCreateCollection_WithCappedOptions_ShouldParseOptions()
        {
            // Arrange
            var options = "{\"capped\": true, \"size\": 1000000, \"max\": 1000}";
            
            _mockDatabase
                .Setup(x => x.CreateCollectionAsync(
                    "logs",
                    It.IsAny<CreateCollectionOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _mongoTool.MongoCreateCollection("logs", options);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task MongoCreateCollection_WithInvalidJsonOptions_ShouldReturnError()
        {
            // Act
            var result = await _mongoTool.MongoCreateCollection("test", "invalid json");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Invalid options JSON format");
        }

        [Fact]
        public async Task MongoCreateCollection_WhenCollectionExists_ShouldReturnError()
        {
            // Arrange
            // Simulate MongoCommandException when collection already exists
            var exception = new MongoException("A collection named 'existing_collection' already exists.");
            
            _mockDatabase
                .Setup(x => x.CreateCollectionAsync(
                    "existing_collection",
                    It.IsAny<CreateCollectionOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _mongoTool.MongoCreateCollection("existing_collection");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().NotBeEmpty();
        }

        #endregion

        #region MongoDropCollection Tests

        [Fact]
        public async Task MongoDropCollection_WithEmptyName_ShouldReturnError()
        {
            // Act
            var result = await _mongoTool.MongoDropCollection("");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Collection name cannot be empty");
        }

        [Fact]
        public async Task MongoDropCollection_WithValidName_ShouldSucceed()
        {
            // Arrange
            _mockDatabase
                .Setup(x => x.DropCollectionAsync(
                    "test_collection",
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _mongoTool.MongoDropCollection("test_collection");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            _mockDatabase.Verify(x => x.DropCollectionAsync(
                "test_collection",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}

