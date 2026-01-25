using Docker.DotNet;
using Docker.DotNet.Models;
using DrSasuMcp.Docker.Docker;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DrSasuMcp.Tests.Docker.Docker;

public class DockerToolTests
{
    private readonly Mock<IDockerClientFactory> _mockClientFactory;
    private readonly Mock<ILogger<DockerTool>> _mockLogger;
    private readonly DockerTool _dockerTool;

    public DockerToolTests()
    {
        _mockClientFactory = new Mock<IDockerClientFactory>();
        _mockLogger = new Mock<ILogger<DockerTool>>();
        _dockerTool = new DockerTool(_mockClientFactory.Object, _mockLogger.Object);
    }

    #region Container Operations Tests

    [Fact]
    public async Task DockerListContainers_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Docker daemon not running"));

        // Act
        var result = await _dockerTool.DockerListContainers();

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Docker daemon not running", result.Error);
    }

    [Fact]
    public async Task DockerInspectContainer_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Connection refused"));

        // Act
        var result = await _dockerTool.DockerInspectContainer("test-container");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Connection refused", result.Error);
    }

    [Fact]
    public async Task DockerStartContainer_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Container not found"));

        // Act
        var result = await _dockerTool.DockerStartContainer("nonexistent");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Container not found", result.Error);
    }

    [Fact]
    public async Task DockerStopContainer_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Container not running"));

        // Act
        var result = await _dockerTool.DockerStopContainer("test-container");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Container not running", result.Error);
    }

    [Fact]
    public async Task DockerRestartContainer_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Container not found"));

        // Act
        var result = await _dockerTool.DockerRestartContainer("test-container");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Container not found", result.Error);
    }

    [Fact]
    public async Task DockerCreateContainer_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Image not found"));

        // Act
        var result = await _dockerTool.DockerCreateContainer("nonexistent:latest");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Image not found", result.Error);
    }

    [Fact]
    public async Task DockerRemoveContainer_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Container is running"));

        // Act
        var result = await _dockerTool.DockerRemoveContainer("test-container");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Container is running", result.Error);
    }

    [Fact]
    public async Task DockerGetContainerLogs_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Container not found"));

        // Act
        var result = await _dockerTool.DockerGetContainerLogs("test-container");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Container not found", result.Error);
    }

    [Fact]
    public async Task DockerListContainerProcesses_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Container not running"));

        // Act
        var result = await _dockerTool.DockerListContainerProcesses("test-container");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Container not running", result.Error);
    }

    [Fact]
    public async Task DockerPruneContainers_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Docker daemon error"));

        // Act
        var result = await _dockerTool.DockerPruneContainers();

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Docker daemon error", result.Error);
    }

    #endregion

    #region Image Operations Tests

    [Fact]
    public async Task DockerListImages_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Docker daemon not running"));

        // Act
        var result = await _dockerTool.DockerListImages();

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Docker daemon not running", result.Error);
    }

    [Fact]
    public async Task DockerInspectImage_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Image not found"));

        // Act
        var result = await _dockerTool.DockerInspectImage("nonexistent:latest");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Image not found", result.Error);
    }

    [Fact]
    public async Task DockerPullImage_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Network error"));

        // Act
        var result = await _dockerTool.DockerPullImage("nginx:latest");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Network error", result.Error);
    }

    [Fact]
    public async Task DockerTagImage_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Image not found"));

        // Act
        var result = await _dockerTool.DockerTagImage("nonexistent", "myrepo/myimage");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Image not found", result.Error);
    }

    [Fact]
    public async Task DockerRemoveImage_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Image in use"));

        // Act
        var result = await _dockerTool.DockerRemoveImage("nginx:latest");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Image in use", result.Error);
    }

    [Fact]
    public async Task DockerGetImageHistory_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Image not found"));

        // Act
        var result = await _dockerTool.DockerGetImageHistory("nonexistent");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Image not found", result.Error);
    }

    [Fact]
    public async Task DockerPruneImages_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Docker daemon error"));

        // Act
        var result = await _dockerTool.DockerPruneImages();

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Docker daemon error", result.Error);
    }

    #endregion

    #region Network Operations Tests

    [Fact]
    public async Task DockerListNetworks_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Docker daemon not running"));

        // Act
        var result = await _dockerTool.DockerListNetworks();

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Docker daemon not running", result.Error);
    }

    [Fact]
    public async Task DockerInspectNetwork_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Network not found"));

        // Act
        var result = await _dockerTool.DockerInspectNetwork("nonexistent");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Network not found", result.Error);
    }

    [Fact]
    public async Task DockerCreateNetwork_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Network already exists"));

        // Act
        var result = await _dockerTool.DockerCreateNetwork("my-network");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Network already exists", result.Error);
    }

    [Fact]
    public async Task DockerRemoveNetwork_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Network in use"));

        // Act
        var result = await _dockerTool.DockerRemoveNetwork("my-network");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Network in use", result.Error);
    }

    [Fact]
    public async Task DockerConnectContainer_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Container not found"));

        // Act
        var result = await _dockerTool.DockerConnectContainer("my-network", "my-container");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Container not found", result.Error);
    }

    [Fact]
    public async Task DockerDisconnectContainer_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Container not connected"));

        // Act
        var result = await _dockerTool.DockerDisconnectContainer("my-network", "my-container");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Container not connected", result.Error);
    }

    [Fact]
    public async Task DockerPruneNetworks_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Docker daemon error"));

        // Act
        var result = await _dockerTool.DockerPruneNetworks();

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Docker daemon error", result.Error);
    }

    #endregion

    #region Volume Operations Tests

    [Fact]
    public async Task DockerListVolumes_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Docker daemon not running"));

        // Act
        var result = await _dockerTool.DockerListVolumes();

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Docker daemon not running", result.Error);
    }

    [Fact]
    public async Task DockerInspectVolume_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Volume not found"));

        // Act
        var result = await _dockerTool.DockerInspectVolume("nonexistent");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Volume not found", result.Error);
    }

    [Fact]
    public async Task DockerCreateVolume_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Volume already exists"));

        // Act
        var result = await _dockerTool.DockerCreateVolume("my-volume");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Volume already exists", result.Error);
    }

    [Fact]
    public async Task DockerRemoveVolume_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Volume in use"));

        // Act
        var result = await _dockerTool.DockerRemoveVolume("my-volume");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Volume in use", result.Error);
    }

    [Fact]
    public async Task DockerPruneVolumes_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _mockClientFactory
            .Setup(f => f.GetDockerClientAsync())
            .ThrowsAsync(new Exception("Docker daemon error"));

        // Act
        var result = await _dockerTool.DockerPruneVolumes();

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Docker daemon error", result.Error);
    }

    #endregion
}
