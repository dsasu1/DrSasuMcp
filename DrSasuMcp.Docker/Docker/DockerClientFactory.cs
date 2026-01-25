using Docker.DotNet;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace DrSasuMcp.Docker.Docker;

/// <summary>
/// Factory for creating Docker client instances
/// </summary>
public class DockerClientFactory : IDockerClientFactory
{
    private readonly ILogger<DockerClientFactory> _logger;
    private DockerClient? _client;
    private readonly object _lock = new();

    /// <summary>
    /// Default Windows named pipe URI for Docker Engine
    /// </summary>
    public const string DefaultWindowsUri = "npipe://./pipe/docker_engine";

    /// <summary>
    /// Default Unix socket URI for Docker Engine
    /// </summary>
    public const string DefaultUnixUri = "unix:///var/run/docker.sock";

    /// <summary>
    /// Environment variable name for Docker host override
    /// </summary>
    public const string DockerHostEnvVar = "DOCKER_HOST";

    /// <summary>
    /// Environment variable name for default Windows URI override
    /// </summary>
    public const string DefaultWindowsUriEnvVar = "DOCKER_DEFAULT_WINDOWS_URI";

    /// <summary>
    /// Environment variable name for default Unix URI override
    /// </summary>
    public const string DefaultUnixUriEnvVar = "DOCKER_DEFAULT_UNIX_URI";

    public DockerClientFactory(ILogger<DockerClientFactory> logger)
    {
        _logger = logger;
    }

    public Task<DockerClient> GetDockerClientAsync()
    {
        if (_client != null)
        {
            return Task.FromResult(_client);
        }

        lock (_lock)
        {
            if (_client != null)
            {
                return Task.FromResult(_client);
            }

            var dockerUri = GetDockerUri();
            _logger.LogInformation("Creating Docker client for URI: {DockerUri}", dockerUri);

            var config = new DockerClientConfiguration(dockerUri);
            _client = config.CreateClient();

            return Task.FromResult(_client);
        }
    }

    private Uri GetDockerUri()
    {
        // First, check for explicit DOCKER_HOST override
        var dockerHost = Environment.GetEnvironmentVariable(DockerHostEnvVar);
        
        if (!string.IsNullOrEmpty(dockerHost))
        {
            _logger.LogInformation("Using {EnvVar} environment variable: {DockerHost}", DockerHostEnvVar, dockerHost);
            return new Uri(dockerHost);
        }

        // Default to local Docker daemon based on OS, with configurable defaults
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var windowsUri = Environment.GetEnvironmentVariable(DefaultWindowsUriEnvVar);
            var uriString = !string.IsNullOrEmpty(windowsUri) ? windowsUri : DefaultWindowsUri;
            
            _logger.LogInformation("Using Windows Docker URI: {DockerUri} (customized: {IsCustom})", 
                uriString, !string.IsNullOrEmpty(windowsUri));
            
            return new Uri(uriString);
        }
        else
        {
            var unixUri = Environment.GetEnvironmentVariable(DefaultUnixUriEnvVar);
            var uriString = !string.IsNullOrEmpty(unixUri) ? unixUri : DefaultUnixUri;
            
            _logger.LogInformation("Using Unix Docker URI: {DockerUri} (customized: {IsCustom})", 
                uriString, !string.IsNullOrEmpty(unixUri));
            
            return new Uri(uriString);
        }
    }
}
