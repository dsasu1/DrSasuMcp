using Docker.DotNet;

namespace DrSasuMcp.Docker.Docker;

/// <summary>
/// Factory interface for creating Docker client instances
/// </summary>
public interface IDockerClientFactory
{
    /// <summary>
    /// Gets or creates a Docker client instance
    /// </summary>
    Task<DockerClient> GetDockerClientAsync();
}
