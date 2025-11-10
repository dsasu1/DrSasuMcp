using DrSasuMcp.Tools.Datadog;
using DrSasuMcp.Tools.Datadog.Models;
using DrSasuMcp.Tools.Datadog.Troubleshooters;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DrSasuMcp.Tests.Datadog.Troubleshooters
{
    public class TracesTroubleshooterTests
    {
        private readonly Mock<IDatadogService> _mockDatadogService;
        private readonly Mock<ILogger<TracesTroubleshooter>> _mockLogger;
        private readonly TracesTroubleshooter _troubleshooter;

        public TracesTroubleshooterTests()
        {
            _mockDatadogService = new Mock<IDatadogService>();
            _mockLogger = new Mock<ILogger<TracesTroubleshooter>>();
            _troubleshooter = new TracesTroubleshooter(
                _mockDatadogService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public void TroubleshooterName_ShouldReturnTraces()
        {
            // Assert
            _troubleshooter.TroubleshooterName.Should().Be("Traces");
        }

        [Theory]
        [InlineData("traces", true)]
        [InlineData("apm", true)]
        [InlineData("latency", true)]
        [InlineData("performance", true)]
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
        public async Task AnalyzeAsync_WithTraces_ShouldReturnRecommendations()
        {
            // Arrange
            var context = new TroubleshootingContext
            {
                IssueDescription = "slow traces",
                ServiceName = "payment-service",
                From = DateTime.UtcNow.AddHours(-1),
                To = DateTime.UtcNow
            };

            _mockDatadogService
                .Setup(s => s.QueryTracesAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TraceQueryResult
                {
                    Traces = new List<Trace>()
                });

            // Act
            var result = await _troubleshooter.AnalyzeAsync(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<List<TroubleshootingRecommendation>>();
        }
    }
}

