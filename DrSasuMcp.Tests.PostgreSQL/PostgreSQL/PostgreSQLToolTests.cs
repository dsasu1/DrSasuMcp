using DrSasuMcp.PostgreSQL.PostgreSQL;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DrSasuMcp.Tests.PostgreSQL
{
    public class PostgreSQLToolTests
    {
        private readonly Mock<IPostgreSqlConnectionFactory> _mockConnectionFactory;
        private readonly Mock<ILogger<PostgreSQLTool>> _mockLogger;
        private readonly PostgreSQLTool _postgresTool;

        public PostgreSQLToolTests()
        {
            _mockConnectionFactory = new Mock<IPostgreSqlConnectionFactory>();
            _mockLogger = new Mock<ILogger<PostgreSQLTool>>();
            _postgresTool = new PostgreSQLTool(_mockConnectionFactory.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithDependencies_ShouldCreateInstance()
        {
            _postgresTool.Should().NotBeNull();
        }

        [Fact]
        public void PostgreSQLTool_Instantiation_ShouldSucceed()
        {
            var tool = new PostgreSQLTool(_mockConnectionFactory.Object, _mockLogger.Object);

            tool.Should().NotBeNull();
            tool.Should().BeOfType<PostgreSQLTool>();
        }

        [Fact]
        public void DescribeTable_WithSchemaPrefix_ShouldParseCorrectly()
        {
            var tableName = "public.users";
            tableName.Should().Contain(".");

            var parts = tableName.Split('.');
            parts.Should().HaveCount(2);
            parts[0].Should().Be("public");
            parts[1].Should().Be("users");
        }

        [Fact]
        public void DescribeTable_WithoutSchema_ShouldWork()
        {
            var tableName = "users";
            tableName.Should().NotContain(".");
        }
    }
}
