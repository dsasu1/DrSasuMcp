using DrSasuMcp.Tools.Datadog;
using DrSasuMcp.Tools.Datadog.Models;
using DrSasuMcp.Tools.Datadog.Troubleshooters;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DrSasuMcp.Tests.Datadog.Troubleshooters
{
    public class MetricsTroubleshooterTests
    {
        private readonly Mock<IDatadogService> _mockDatadogService;
        private readonly Mock<ILogger<MetricsTroubleshooter>> _mockLogger;
        private readonly MetricsTroubleshooter _troubleshooter;

        public MetricsTroubleshooterTests()
        {
            _mockDatadogService = new Mock<IDatadogService>();
            _mockLogger = new Mock<ILogger<MetricsTroubleshooter>>();
            _troubleshooter = new MetricsTroubleshooter(
                _mockDatadogService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public void TroubleshooterName_ShouldReturnMetrics()
        {
            // Assert
            _troubleshooter.TroubleshooterName.Should().Be("Metrics");
        }

        [Theory]
        [InlineData("metrics", true)]
        [InlineData("performance", true)]
        [InlineData("latency", true)]
        [InlineData("throughput", true)]
        [InlineData("errors", false)]
        [InlineData("logs", false)]
        public void SupportsIssueType_WithVariousTypes_ReturnsExpected(string issueType, bool expected)
        {
            // Act
            var result = _troubleshooter.SupportsIssueType(issueType);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void SupportsIssueType_IsCaseInsensitive()
        {
            // Act & Assert
            _troubleshooter.SupportsIssueType("METRICS").Should().BeTrue();
            _troubleshooter.SupportsIssueType("Metrics").Should().BeTrue();
            _troubleshooter.SupportsIssueType("metrics").Should().BeTrue();
        }

        [Fact]
        public async Task AnalyzeAsync_WithNullContext_ShouldReturnEmptyList()
        {
            // Arrange
            var context = new TroubleshootingContext
            {
                IssueDescription = "test"
            };

            // Act
            var result = await _troubleshooter.AnalyzeAsync(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<List<TroubleshootingRecommendation>>();
        }

        [Fact]
        public async Task AnalyzeAsync_WithServiceName_ShouldQueryMetrics()
        {
            // Arrange
            var context = new TroubleshootingContext
            {
                IssueDescription = "high cpu",
                ServiceName = "payment-service",
                From = DateTime.UtcNow.AddHours(-1),
                To = DateTime.UtcNow
            };

            _mockDatadogService
                .Setup(s => s.QueryMetricsAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MetricQueryResult
                {
                    Series = new List<MetricSeries>()
                });

            // Act
            var result = await _troubleshooter.AnalyzeAsync(context);

            // Assert
            result.Should().NotBeNull();
            _mockDatadogService.Verify(s => s.QueryMetricsAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }
    }
}

