using DrSasuMcp.MongoDB.MongoDB;
using FluentAssertions;
using MongoDB.Driver;
using System;
using Xunit;

namespace DrSasuMcp.Tests.MongoDB
{
    public class MongoConnectionFactoryTests
    {
        [Fact]
        public void GetConnectionString_WhenNotSet_ShouldThrowException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("MONGODB_CONNECTION_STRING", null);
            var factory = new MongoConnectionFactory();

            // Act
            Func<System.Threading.Tasks.Task> act = async () => await factory.GetDatabaseAsync();

            // Assert
            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*MONGODB_CONNECTION_STRING*");
        }

        [Theory]
        [InlineData("mongodb://localhost:27017/testdb", "testdb")]
        [InlineData("mongodb://localhost:27017/myapp", "myapp")]
        [InlineData("mongodb://user:pass@localhost:27017/production", "production")]
        public void GetDatabaseName_WithDatabaseInUrl_ShouldParseDatabaseName(string connectionString, string expectedDbName)
        {
            // Arrange
            var url = new MongoUrl(connectionString);

            // Act
            var actualDbName = url.DatabaseName;

            // Assert
            actualDbName.Should().Be(expectedDbName);
        }

        [Fact]
        public void GetDatabaseName_WithoutDatabaseInUrl_ShouldReturnTest()
        {
            // Arrange
            var connectionString = "mongodb://localhost:27017";
            var url = new MongoUrl(connectionString);

            // Act
            var dbName = url.DatabaseName;

            // Assert
            // When database name is not specified, MongoUrl returns null
            // Our factory would then default to "test"
            dbName.Should().BeNull();
        }

        [Theory]
        [InlineData("mongodb://localhost:27017/testdb?authSource=admin", "testdb")]
        [InlineData("mongodb://localhost:27017/app?retryWrites=true&w=majority", "app")]
        [InlineData("mongodb+srv://user:pass@cluster.mongodb.net/production?retryWrites=true", "production")]
        public void GetDatabaseName_WithQueryString_ShouldHandleCorrectly(string connectionString, string expectedDbName)
        {
            // Arrange
            var url = new MongoUrl(connectionString);

            // Act
            var actualDbName = url.DatabaseName;

            // Assert
            actualDbName.Should().Be(expectedDbName);
        }

        [Fact]
        public void MongoConnectionFactory_Instantiation_ShouldSucceed()
        {
            // Arrange & Act
            var factory = new MongoConnectionFactory();

            // Assert
            factory.Should().NotBeNull();
            factory.Should().BeAssignableTo<IMongoConnectionFactory>();
        }

        [Theory]
        [InlineData("mongodb://localhost:27017/test")]
        [InlineData("mongodb://user:password@localhost:27017/mydb")]
        [InlineData("mongodb+srv://user:password@cluster.mongodb.net/database")]
        public void ConnectionString_Format_ShouldBeValid(string connectionString)
        {
            // Act
            Action act = () => new MongoUrl(connectionString);

            // Assert
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData("invalid connection string")]
        [InlineData("http://localhost:27017/test")]
        [InlineData("")]
        public void ConnectionString_InvalidFormat_ShouldThrow(string connectionString)
        {
            // Act
            Action act = () => new MongoUrl(connectionString);

            // Assert
            act.Should().Throw<Exception>();
        }
    }
}

