namespace DrSasuMcp.Docker.Docker;

/// <summary>
/// Constants for Docker tool configuration
/// </summary>
public static class DockerToolConstants
{
    /// <summary>
    /// Environment variable for Docker host URI
    /// </summary>
    public const string EnvDockerHost = "DOCKER_HOST";

    /// <summary>
    /// Environment variable for Docker TLS verification
    /// </summary>
    public const string EnvDockerTlsVerify = "DOCKER_TLS_VERIFY";

    /// <summary>
    /// Environment variable for Docker certificate path
    /// </summary>
    public const string EnvDockerCertPath = "DOCKER_CERT_PATH";

    /// <summary>
    /// Environment variable for Docker API version
    /// </summary>
    public const string EnvDockerApiVersion = "DOCKER_API_VERSION";

    /// <summary>
    /// Environment variable for default timeout in seconds
    /// </summary>
    public const string EnvDockerDefaultTimeout = "DOCKER_DEFAULT_TIMEOUT";

    /// <summary>
    /// Default timeout in seconds for Docker operations
    /// </summary>
    public const int DefaultTimeoutSeconds = 30;

    /// <summary>
    /// Maximum timeout in seconds for Docker operations
    /// </summary>
    public const int MaxTimeoutSeconds = 300;
}
