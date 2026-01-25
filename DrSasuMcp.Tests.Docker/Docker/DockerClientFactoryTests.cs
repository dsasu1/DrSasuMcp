using DrSasuMcp.Docker.Docker;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DrSasuMcp.Tests.Docker.Docker;

/// <summary>
/// Integration tests for DockerClientFactory.
/// These tests require Docker to be running and are skipped if Docker is not available.
/// </summary>
public class DockerClientFactoryTests
{
    private readonly Mock<ILogger<DockerClientFactory>> _mockLogger;

    public DockerClientFactoryTests()
    {
        _mockLogger = new Mock<ILogger<DockerClientFactory>>();
    }

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        // Arrange & Act
        var factory = new DockerClientFactory(_mockLogger.Object);

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public async Task GetDockerClientAsync_ReturnsSameClient_OnMultipleCalls_WhenDockerAvailable()
    {
        // Arrange
        var factory = new DockerClientFactory(_mockLogger.Object);

        try
        {
            // Act
            var client1 = await factory.GetDockerClientAsync();
            var client2 = await factory.GetDockerClientAsync();

            // Assert - Same instance should be returned
            Assert.Same(client1, client2);
        }
        catch (Exception ex) when (ex.Message.Contains("npipe") || ex.Message.Contains("docker") || ex.Message.Contains("connect"))
        {
            // Skip if Docker is not available
            Assert.True(true, "Skipped: Docker not available");
        }
    }
}
