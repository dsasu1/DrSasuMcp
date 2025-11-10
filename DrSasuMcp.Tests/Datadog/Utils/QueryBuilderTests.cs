using DrSasuMcp.Tools.Datadog.Utils;
using FluentAssertions;
using Xunit;

namespace DrSasuMcp.Tests.Datadog.Utils
{
    public class QueryBuilderTests
    {
        [Fact]
        public void BuildMetricQuery_WithMetricOnly_ReturnsBasicQuery()
        {
            // Act
            var result = QueryBuilder.BuildMetricQuery("system.cpu.user");

            // Assert
            result.Should().Be("system.cpu.user");
        }

        [Fact]
        public void BuildMetricQuery_WithAggregation_ReturnsQueryWithAggregation()
        {
            // Act
            var result = QueryBuilder.BuildMetricQuery("system.cpu.user", "avg");

            // Assert
            result.Should().Be("avg:system.cpu.user");
        }

        [Fact]
        public void BuildMetricQuery_WithTags_ReturnsQueryWithTags()
        {
            // Arrange
            var tags = new Dictionary<string, string>
            {
                { "service", "payment" },
                { "env", "prod" }
            };

            // Act
            var result = QueryBuilder.BuildMetricQuery("system.cpu.user", null, tags);

            // Assert
            result.Should().Contain("system.cpu.user");
            result.Should().Contain("service:payment");
            result.Should().Contain("env:prod");
            result.Should().Contain("{");
            result.Should().Contain("}");
        }

        [Fact]
        public void BuildMetricQuery_WithAllParameters_ReturnsCompleteQuery()
        {
            // Arrange
            var tags = new Dictionary<string, string> { { "service", "payment" } };

            // Act
            var result = QueryBuilder.BuildMetricQuery("system.cpu.user", "avg", tags);

            // Assert
            result.Should().Be("avg:system.cpu.user{service:payment}");
        }

        [Fact]
        public void BuildLogQuery_WithTextOnly_ReturnsTextQuery()
        {
            // Act
            var result = QueryBuilder.BuildLogQuery("error");

            // Assert
            result.Should().Be("error");
        }

        [Fact]
        public void BuildLogQuery_WithFilters_ReturnsQueryWithFilters()
        {
            // Arrange
            var filters = new Dictionary<string, string>
            {
                { "service", "payment" },
                { "status", "error" }
            };

            // Act
            var result = QueryBuilder.BuildLogQuery(null, filters);

            // Assert
            result.Should().Contain("service:payment");
            result.Should().Contain("status:error");
        }

        [Fact]
        public void BuildLogQuery_WithTextAndFilters_ReturnsCombinedQuery()
        {
            // Arrange
            var filters = new Dictionary<string, string> { { "service", "payment" } };

            // Act
            var result = QueryBuilder.BuildLogQuery("error", filters);

            // Assert
            result.Should().Contain("error");
            result.Should().Contain("service:payment");
        }

        [Fact]
        public void BuildTraceQuery_WithServiceOnly_ReturnsServiceQuery()
        {
            // Act
            var result = QueryBuilder.BuildTraceQuery("payment-service");

            // Assert
            result.Should().Be("service:payment-service");
        }

        [Fact]
        public void BuildTraceQuery_WithServiceAndOperation_ReturnsCombinedQuery()
        {
            // Act
            var result = QueryBuilder.BuildTraceQuery("payment-service", "checkout");

            // Assert
            result.Should().Contain("service:payment-service");
            result.Should().Contain("operation:checkout");
        }

        [Fact]
        public void BuildTraceQuery_WithTags_ReturnsQueryWithTags()
        {
            // Arrange
            var tags = new Dictionary<string, string> { { "env", "prod" } };

            // Act
            var result = QueryBuilder.BuildTraceQuery("payment-service", null, tags);

            // Assert
            result.Should().Contain("service:payment-service");
            result.Should().Contain("env:prod");
        }

        [Fact]
        public void BuildTraceQuery_WithAllParameters_ReturnsCompleteQuery()
        {
            // Arrange
            var tags = new Dictionary<string, string> { { "env", "prod" } };

            // Act
            var result = QueryBuilder.BuildTraceQuery("payment-service", "checkout", tags);

            // Assert
            result.Should().Contain("service:payment-service");
            result.Should().Contain("operation:checkout");
            result.Should().Contain("env:prod");
        }
    }
}

