using DrSasuMcp.Tools.Datadog;
using DrSasuMcp.Tools.Datadog.Models;
using DrSasuMcp.Tools.Datadog.Troubleshooters;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DrSasuMcp.Tests.Datadog.Troubleshooters
{
    public class ErrorTrackingTroubleshooterTests
    {
        private readonly Mock<IDatadogService> _mockDatadogService;
        private readonly Mock<ILogger<ErrorTrackingTroubleshooter>> _mockLogger;
        private readonly ErrorTrackingTroubleshooter _troubleshooter;

        public ErrorTrackingTroubleshooterTests()
        {
            _mockDatadogService = new Mock<IDatadogService>();
            _mockLogger = new Mock<ILogger<ErrorTrackingTroubleshooter>>();
            _troubleshooter = new ErrorTrackingTroubleshooter(
                _mockDatadogService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public void TroubleshooterName_ShouldReturnErrorTracking()
        {
            // Assert
            _troubleshooter.TroubleshooterName.Should().Be("ErrorTracking");
        }

        [Theory]
        [InlineData("errors", true)]
        [InlineData("error-tracking", true)]
        [InlineData("exceptions", true)]
        [InlineData("metrics", false)]
        [InlineData("logs", false)]
        public void SupportsIssueType_WithVariousTypes_ReturnsExpected(string issueType, bool expected)
        {
            // Act
            var result = _troubleshooter.SupportsIssueType(issueType);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public async Task AnalyzeAsync_WithErrorIssues_ShouldReturnRecommendations()
        {
            // Arrange
            var context = new TroubleshootingContext
            {
                IssueDescription = "high error rate",
                ServiceName = "payment-service",
                From = DateTime.UtcNow.AddHours(-1),
                To = DateTime.UtcNow
            };

            _mockDatadogService
                .Setup(s => s.GetErrorIssuesAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ErrorTrackingResult
                {
                    Issues = new List<ErrorIssue>()
                });

            // Act
            var result = await _troubleshooter.AnalyzeAsync(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<List<TroubleshootingRecommendation>>();
        }
    }
}

