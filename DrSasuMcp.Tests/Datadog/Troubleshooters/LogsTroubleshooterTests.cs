using DrSasuMcp.Tools.Datadog;
using DrSasuMcp.Tools.Datadog.Models;
using DrSasuMcp.Tools.Datadog.Troubleshooters;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DrSasuMcp.Tests.Datadog.Troubleshooters
{
    public class LogsTroubleshooterTests
    {
        private readonly Mock<IDatadogService> _mockDatadogService;
        private readonly Mock<ILogger<LogsTroubleshooter>> _mockLogger;
        private readonly LogsTroubleshooter _troubleshooter;

        public LogsTroubleshooterTests()
        {
            _mockDatadogService = new Mock<IDatadogService>();
            _mockLogger = new Mock<ILogger<LogsTroubleshooter>>();
            _troubleshooter = new LogsTroubleshooter(
                _mockDatadogService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public void TroubleshooterName_ShouldReturnLogs()
        {
            // Assert
            _troubleshooter.TroubleshooterName.Should().Be("Logs");
        }

        [Theory]
        [InlineData("logs", true)]
        [InlineData("errors", true)]
        [InlineData("exceptions", true)]
        [InlineData("metrics", false)]
        [InlineData("traces", false)]
        public void SupportsIssueType_WithVariousTypes_ReturnsExpected(string issueType, bool expected)
        {
            // Act
            var result = _troubleshooter.SupportsIssueType(issueType);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public async Task AnalyzeAsync_WithContext_ShouldReturnRecommendations()
        {
            // Arrange
            var context = new TroubleshootingContext
            {
                IssueDescription = "error logs",
                ServiceName = "payment-service",
                From = DateTime.UtcNow.AddHours(-1),
                To = DateTime.UtcNow
            };

            _mockDatadogService
                .Setup(s => s.QueryLogsAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LogQueryResult
                {
                    Events = new List<LogEvent>()
                });

            // Act
            var result = await _troubleshooter.AnalyzeAsync(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<List<TroubleshootingRecommendation>>();
        }
    }
}

