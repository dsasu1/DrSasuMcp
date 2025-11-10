using DrSasuMcp.Tools.Datadog;
using DrSasuMcp.Tools.Datadog.Models;
using DrSasuMcp.Tools.Datadog.Troubleshooters;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DrSasuMcp.Tests.Datadog.Troubleshooters
{
    public class ServiceMapTroubleshooterTests
    {
        private readonly Mock<IDatadogService> _mockDatadogService;
        private readonly Mock<ILogger<ServiceMapTroubleshooter>> _mockLogger;
        private readonly ServiceMapTroubleshooter _troubleshooter;

        public ServiceMapTroubleshooterTests()
        {
            _mockDatadogService = new Mock<IDatadogService>();
            _mockLogger = new Mock<ILogger<ServiceMapTroubleshooter>>();
            _troubleshooter = new ServiceMapTroubleshooter(
                _mockDatadogService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public void TroubleshooterName_ShouldReturnServiceMap()
        {
            // Assert
            _troubleshooter.TroubleshooterName.Should().Be("ServiceMap");
        }

        [Theory]
        [InlineData("service-map", true)]
        [InlineData("dependencies", true)]
        [InlineData("cascading", true)]
        [InlineData("metrics", false)]
        [InlineData("errors", false)]
        public void SupportsIssueType_WithVariousTypes_ReturnsExpected(string issueType, bool expected)
        {
            // Act
            var result = _troubleshooter.SupportsIssueType(issueType);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public async Task AnalyzeAsync_WithServiceMap_ShouldReturnRecommendations()
        {
            // Arrange
            var context = new TroubleshootingContext
            {
                IssueDescription = "service dependencies",
                ServiceName = "payment-service",
                From = DateTime.UtcNow.AddHours(-1),
                To = DateTime.UtcNow
            };

            _mockDatadogService
                .Setup(s => s.GetServiceMapAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceMap
                {
                    Nodes = new List<ServiceNode>(),
                    Edges = new List<ServiceEdge>()
                });

            // Act
            var result = await _troubleshooter.AnalyzeAsync(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<List<TroubleshootingRecommendation>>();
        }
    }
}

