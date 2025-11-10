using DrSasuMcp.Tools.Datadog;
using DrSasuMcp.Tools.Datadog.Troubleshooters;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace DrSasuMcp.Tests.Datadog
{
    public class DatadogToolTests
    {
        private readonly Mock<IDatadogService> _mockDatadogService;
        private readonly Mock<ILogger<DatadogTool>> _mockLogger;
        private readonly List<ITroubleshooter> _troubleshooters;
        private readonly DatadogTool _datadogTool;

        public DatadogToolTests()
        {
            _mockDatadogService = new Mock<IDatadogService>();
            _mockLogger = new Mock<ILogger<DatadogTool>>();
            _troubleshooters = new List<ITroubleshooter>();
            
            _datadogTool = new DatadogTool(
                _mockDatadogService.Object,
                _mockLogger.Object,
                _troubleshooters
            );
        }

        [Fact]
        public void Constructor_WithDependencies_ShouldCreateInstance()
        {
            // Assert
            _datadogTool.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithTroubleshooters_ShouldInitializeTroubleshooters()
        {
            // Arrange
            var troubleshooters = new List<ITroubleshooter>
            {
                new Mock<ITroubleshooter>().Object,
                new Mock<ITroubleshooter>().Object
            };

            // Act
            var tool = new DatadogTool(
                _mockDatadogService.Object,
                _mockLogger.Object,
                troubleshooters
            );

            // Assert
            tool.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithEmptyTroubleshooters_ShouldCreateInstance()
        {
            // Arrange
            var emptyTroubleshooters = new List<ITroubleshooter>();

            // Act
            var tool = new DatadogTool(
                _mockDatadogService.Object,
                _mockLogger.Object,
                emptyTroubleshooters
            );

            // Assert
            tool.Should().NotBeNull();
        }
    }
}

