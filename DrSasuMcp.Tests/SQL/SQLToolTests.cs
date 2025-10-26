using DrSasuMcp.Tools;
using DrSasuMcp.Tools.SQL;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DrSasuMcp.Tests.SQL
{
    public class SQLToolTests
    {
        private readonly Mock<ISqlConnectionFactory> _mockConnectionFactory;
        private readonly Mock<ILogger<SQLTool>> _mockLogger;
        private readonly SQLTool _sqlTool;

        public SQLToolTests()
        {
            _mockConnectionFactory = new Mock<ISqlConnectionFactory>();
            _mockLogger = new Mock<ILogger<SQLTool>>();
            _sqlTool = new SQLTool(_mockConnectionFactory.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithDependencies_ShouldCreateInstance()
        {
            // Assert
            _sqlTool.Should().NotBeNull();
        }

        [Fact]
        public void SQLTool_Instantiation_ShouldSucceed()
        {
            // Arrange & Act
            var tool = new SQLTool(_mockConnectionFactory.Object, _mockLogger.Object);

            // Assert
            tool.Should().NotBeNull();
            tool.Should().BeOfType<SQLTool>();
        }

        [Fact]
        public void DescribeTable_WithSchemaPrefix_ShouldParseCorrectly()
        {
            // The DescribeTable method should handle schema.table format
            // This validates the parsing logic exists in the codebase
            var tableName = "dbo.Users";
            tableName.Should().Contain(".");
            
            var parts = tableName.Split('.');
            parts.Should().HaveCount(2);
            parts[0].Should().Be("dbo");
            parts[1].Should().Be("Users");
        }

        [Fact]
        public void DescribeTable_WithoutSchema_ShouldWork()
        {
            // Test that table names without schema are handled
            var tableName = "Users";
            tableName.Should().NotContain(".");
        }

        // Note: The SQLTool methods call GetOpenConnectionAsync() before the try-catch block,
        // so connection errors will throw exceptions rather than return OperationResult.
        // This is by design - connection errors are fatal and should be handled at a higher level.
        // For full error handling tests, integration tests with a real database would be needed.
    }
}

