using Docker.DotNet;
using Docker.DotNet.Models;
using DrSasuMcp.Common.Models;
using DrSasuMcp.Docker.Docker.Models;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DrSasuMcp.Docker.Docker;

[McpServerToolType]
public partial class DockerTool
{
    private readonly IDockerClientFactory _dockerClientFactory;
    private readonly ILogger<DockerTool> _logger;

    public DockerTool(IDockerClientFactory dockerClientFactory, ILogger<DockerTool> logger)
    {
        _dockerClientFactory = dockerClientFactory;
        _logger = logger;
    }

    #region Container Operations - Read

    [McpServerTool(
        Title = "Docker: List Containers",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Lists all Docker containers")]
    public async Task<OperationResult> DockerListContainers(
        [Description("Show all containers (including stopped). Default: false")] bool all = false,
        [Description("Filter containers by status (running, exited, etc.)")] string? status = null)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var parameters = new ContainersListParameters
            {
                All = all
            };

            if (!string.IsNullOrEmpty(status))
            {
                parameters.Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["status"] = new Dictionary<string, bool> { [status] = true }
                };
            }

            var containers = await client.Containers.ListContainersAsync(parameters);

            var result = containers.Select(c => new
            {
                id = c.ID,
                name = c.Names?.FirstOrDefault()?.TrimStart('/') ?? string.Empty,
                image = c.Image,
                status = c.Status,
                state = c.State,
                created = c.Created,
                labels = c.Labels,
                ports = c.Ports?.Select(p => $"{p.PrivatePort}/{p.Type} -> {p.PublicPort}").ToList()
            }).ToList();

            return new OperationResult(success: true, data: result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListContainers failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Inspect Container",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Gets detailed information about a container")]
    public async Task<OperationResult> DockerInspectContainer(
        [Description("Container ID or name")] string containerId)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var container = await client.Containers.InspectContainerAsync(containerId);

            var result = new
            {
                id = container.ID,
                name = container.Name?.TrimStart('/'),
                image = container.Config?.Image,
                state = container.State?.Status,
                created = container.Created,
                startedAt = container.State?.StartedAt,
                finishedAt = container.State?.FinishedAt,
                restartCount = container.RestartCount,
                exitCode = container.State?.ExitCode,
                error = container.State?.Error,
                running = container.State?.Running ?? false,
                paused = container.State?.Paused ?? false,
                restarting = container.State?.Restarting ?? false,
                labels = container.Config?.Labels,
                env = container.Config?.Env,
                ports = container.NetworkSettings?.Ports,
                mounts = container.Mounts?.Select(m => new
                {
                    type = m.Type,
                    source = m.Source,
                    destination = m.Destination,
                    mode = m.Mode
                })
            };

            return new OperationResult(success: true, data: result);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Container '{containerId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InspectContainer failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Get Container Logs",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Retrieves logs from a container")]
    public async Task<OperationResult> DockerGetContainerLogs(
        [Description("Container ID or name")] string containerId,
        [Description("Number of lines to show from the end of logs")] int tail = 100,
        [Description("Show timestamps")] bool timestamps = false)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var parameters = new ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true,
                Tail = tail > 0 ? tail.ToString() : "all",
                Timestamps = timestamps
            };

            using var logs = await client.Containers.GetContainerLogsAsync(containerId, false, parameters, CancellationToken.None);

            var logBuilder = new StringBuilder();
            var buffer = new byte[4096];
            while (true)
            {
                var readResult = await logs.ReadOutputAsync(buffer, 0, buffer.Length, CancellationToken.None);
                if (readResult.EOF)
                    break;
                logBuilder.Append(Encoding.UTF8.GetString(buffer, 0, readResult.Count));
            }

            var logContent = logBuilder.ToString();
            var result = new
            {
                logs = logContent,
                lineCount = logContent.Split('\n').Length
            };

            return new OperationResult(success: true, data: result);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Container '{containerId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetContainerLogs failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: List Container Processes",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Lists processes running inside a container")]
    public async Task<OperationResult> DockerListContainerProcesses(
        [Description("Container ID or name")] string containerId)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var processes = await client.Containers.ListProcessesAsync(containerId, new ContainerListProcessesParameters());

            var result = new
            {
                titles = processes.Titles,
                processes = processes.Processes
            };

            return new OperationResult(success: true, data: result);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Container '{containerId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListContainerProcesses failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    #endregion

    #region Container Operations - Write

    [McpServerTool(
        Title = "Docker: Start Container",
        ReadOnly = false,
        Idempotent = false,
        Destructive = false),
        Description("Starts a stopped container")]
    public async Task<OperationResult> DockerStartContainer(
        [Description("Container ID or name")] string containerId)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters());

            return new OperationResult(success: true);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Container '{containerId}' not found");
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotModified)
        {
            return new OperationResult(success: false, error: $"Container '{containerId}' is already running");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StartContainer failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Stop Container",
        ReadOnly = false,
        Idempotent = false,
        Destructive = false),
        Description("Stops a running container")]
    public async Task<OperationResult> DockerStopContainer(
        [Description("Container ID or name")] string containerId,
        [Description("Timeout in seconds before killing the container")] int timeoutSeconds = 10)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            await client.Containers.StopContainerAsync(containerId, new ContainerStopParameters
            {
                WaitBeforeKillSeconds = (uint)timeoutSeconds
            });

            return new OperationResult(success: true);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Container '{containerId}' not found");
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotModified)
        {
            return new OperationResult(success: false, error: $"Container '{containerId}' is already stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StopContainer failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Restart Container",
        ReadOnly = false,
        Idempotent = false,
        Destructive = false),
        Description("Restarts a container")]
    public async Task<OperationResult> DockerRestartContainer(
        [Description("Container ID or name")] string containerId,
        [Description("Timeout in seconds before killing the container")] int timeoutSeconds = 10)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            await client.Containers.RestartContainerAsync(containerId, new ContainerRestartParameters
            {
                WaitBeforeKillSeconds = (uint)timeoutSeconds
            });

            return new OperationResult(success: true);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Container '{containerId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RestartContainer failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Create Container",
        ReadOnly = false,
        Idempotent = false,
        Destructive = false),
        Description("Creates a new container from an image")]
    public async Task<OperationResult> DockerCreateContainer(
        [Description("Image name to use for the container")] string image,
        [Description("Container name")] string? name = null,
        [Description("Command to run in the container")] string? command = null,
        [Description("Environment variables in KEY=value format, e.g. [\"POSTGRES_PASSWORD=secret\", \"POSTGRES_DB=mydb\"]")] List<string>? environment = null,
        [Description("Port mappings as containerPort:hostPort, e.g. {\"5432\":\"5432\", \"80\":\"8080\"}")] Dictionary<string, string>? ports = null,
        [Description("Volume binds, e.g. [\"myvolume:/data\", \"/host/path:/container/path\"]")] List<string>? volumes = null,
        [Description("Restart policy: no, always, unless-stopped, on-failure")] string? restartPolicy = null)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();

            var createParams = new CreateContainerParameters
            {
                Image = image,
                Name = name,
                Cmd = !string.IsNullOrEmpty(command) ? command.Split(' ') : null
            };

            if (environment is { Count: > 0 })
            {
                createParams.Env = environment;
            }

            var hostConfig = new HostConfig();
            var hasHostConfig = false;

            if (ports is { Count: > 0 })
            {
                hasHostConfig = true;
                createParams.ExposedPorts = new Dictionary<string, EmptyStruct>();
                hostConfig.PortBindings = new Dictionary<string, IList<PortBinding>>();

                foreach (var kvp in ports)
                {
                    var containerPort = kvp.Key.Contains('/') ? kvp.Key : $"{kvp.Key}/tcp";
                    createParams.ExposedPorts[containerPort] = default;
                    hostConfig.PortBindings[containerPort] = new List<PortBinding>
                    {
                        new PortBinding { HostPort = kvp.Value }
                    };
                }
            }

            if (volumes is { Count: > 0 })
            {
                hasHostConfig = true;
                hostConfig.Binds = volumes;
            }

            if (!string.IsNullOrEmpty(restartPolicy))
            {
                hasHostConfig = true;
                hostConfig.RestartPolicy = new RestartPolicy
                {
                    Name = restartPolicy switch
                    {
                        "always" => RestartPolicyKind.Always,
                        "unless-stopped" => RestartPolicyKind.UnlessStopped,
                        "on-failure" => RestartPolicyKind.OnFailure,
                        _ => RestartPolicyKind.No
                    }
                };
            }

            if (hasHostConfig)
            {
                createParams.HostConfig = hostConfig;
            }

            var response = await client.Containers.CreateContainerAsync(createParams);

            return new OperationResult(success: true, data: new { id = response.ID, warnings = response.Warnings });
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Image '{image}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateContainer failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Remove Container",
        ReadOnly = false,
        Idempotent = false,
        Destructive = true),
        Description("Removes a container")]
    public async Task<OperationResult> DockerRemoveContainer(
        [Description("Container ID or name")] string containerId,
        [Description("Force removal even if container is running")] bool force = false)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            await client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters
            {
                Force = force
            });

            return new OperationResult(success: true);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Container '{containerId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveContainer failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Prune Containers",
        ReadOnly = false,
        Idempotent = false,
        Destructive = true),
        Description("Removes all stopped containers")]
    public async Task<OperationResult> DockerPruneContainers()
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var result = await client.Containers.PruneContainersAsync();

            return new OperationResult(success: true, data: new
            {
                spaceReclaimed = result.SpaceReclaimed,
                containersDeleted = result.ContainersDeleted
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PruneContainers failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    #endregion

    #region Image Operations - Read

    [McpServerTool(
        Title = "Docker: List Images",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Lists all Docker images")]
    public async Task<OperationResult> DockerListImages(
        [Description("Show all images (including intermediate). Default: false")] bool all = false)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var images = await client.Images.ListImagesAsync(new ImagesListParameters
            {
                All = all
            });

            var result = images.Select(img => new
            {
                id = img.ID,
                repoTags = img.RepoTags,
                repoDigests = img.RepoDigests,
                size = img.Size,
                virtualSize = img.VirtualSize,
                created = img.Created,
                labels = img.Labels
            }).ToList();

            return new OperationResult(success: true, data: result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListImages failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Inspect Image",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Gets detailed information about an image")]
    public async Task<OperationResult> DockerInspectImage(
        [Description("Image ID or name")] string imageId)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var image = await client.Images.InspectImageAsync(imageId);

            var result = new
            {
                id = image.ID,
                repoTags = image.RepoTags,
                repoDigests = image.RepoDigests,
                parent = image.Parent,
                created = image.Created,
                size = image.Size,
                virtualSize = image.VirtualSize,
                architecture = image.Architecture,
                os = image.Os
            };

            return new OperationResult(success: true, data: result);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Image '{imageId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InspectImage failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Get Image History",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Shows the history of an image")]
    public async Task<OperationResult> DockerGetImageHistory(
        [Description("Image ID or name")] string imageId)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var history = await client.Images.GetImageHistoryAsync(imageId);

            var result = history.Select(h => new
            {
                id = h.ID,
                created = h.Created,
                createdBy = h.CreatedBy,
                size = h.Size,
                comment = h.Comment,
                tags = h.Tags
            }).ToList();

            return new OperationResult(success: true, data: result);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Image '{imageId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetImageHistory failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    #endregion

    #region Image Operations - Write

    [McpServerTool(
        Title = "Docker: Pull Image",
        ReadOnly = false,
        Idempotent = false,
        Destructive = false),
        Description("Downloads an image from a registry")]
    public async Task<OperationResult> DockerPullImage(
        [Description("Image name (e.g., 'nginx:latest' or 'docker.io/library/nginx:latest')")] string image)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var progress = new Progress<JSONMessage>(message =>
            {
                _logger.LogDebug("Pull progress: {Status} {Progress}", message.Status, message.ProgressMessage);
            });

            await client.Images.CreateImageAsync(new ImagesCreateParameters
            {
                FromImage = image
            }, null, progress);

            return new OperationResult(success: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PullImage failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Tag Image",
        ReadOnly = false,
        Idempotent = false,
        Destructive = false),
        Description("Tags an image")]
    public async Task<OperationResult> DockerTagImage(
        [Description("Image ID or name")] string imageId,
        [Description("Repository name (e.g., 'myrepo/myimage')")] string repository,
        [Description("Tag name (e.g., 'v1.0')")] string? tag = null)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var tagName = !string.IsNullOrEmpty(tag) ? $"{repository}:{tag}" : repository;
            
            await client.Images.TagImageAsync(imageId, new ImageTagParameters
            {
                RepositoryName = repository,
                Tag = tag
            });

            return new OperationResult(success: true, data: new { tag = tagName });
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Image '{imageId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TagImage failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Remove Image",
        ReadOnly = false,
        Idempotent = false,
        Destructive = true),
        Description("Removes an image")]
    public async Task<OperationResult> DockerRemoveImage(
        [Description("Image ID or name")] string imageId,
        [Description("Force removal even if image is being used")] bool force = false)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            await client.Images.DeleteImageAsync(imageId, new ImageDeleteParameters
            {
                Force = force
            });

            return new OperationResult(success: true);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Image '{imageId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveImage failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Prune Images",
        ReadOnly = false,
        Idempotent = false,
        Destructive = true),
        Description("Removes unused images")]
    public async Task<OperationResult> DockerPruneImages()
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var result = await client.Images.PruneImagesAsync();

            return new OperationResult(success: true, data: new
            {
                spaceReclaimed = result.SpaceReclaimed,
                imagesDeleted = result.ImagesDeleted
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PruneImages failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    #endregion

    #region Network Operations - Read

    [McpServerTool(
        Title = "Docker: List Networks",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Lists all Docker networks")]
    public async Task<OperationResult> DockerListNetworks()
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var networks = await client.Networks.ListNetworksAsync();

            var result = networks.Select(n => new
            {
                id = n.ID,
                name = n.Name,
                driver = n.Driver,
                scope = n.Scope,
                @internal = n.Internal,
                attachable = n.Attachable,
                ingress = n.Ingress,
                labels = n.Labels
            }).ToList();

            return new OperationResult(success: true, data: result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListNetworks failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Inspect Network",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Gets detailed information about a network")]
    public async Task<OperationResult> DockerInspectNetwork(
        [Description("Network ID or name")] string networkId)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var network = await client.Networks.InspectNetworkAsync(networkId);

            var result = new
            {
                id = network.ID,
                name = network.Name,
                driver = network.Driver,
                scope = network.Scope,
                isInternal = network.Internal,
                attachable = network.Attachable,
                ingress = network.Ingress,
                labels = network.Labels,
                containers = network.Containers?.Select(c => new
                {
                    id = c.Key,
                    name = c.Value.Name,
                    macAddress = c.Value.MacAddress,
                    ipv4Address = c.Value.IPv4Address,
                    ipv6Address = c.Value.IPv6Address
                })
            };

            return new OperationResult(success: true, data: result);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Network '{networkId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InspectNetwork failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    #endregion

    #region Network Operations - Write

    [McpServerTool(
        Title = "Docker: Create Network",
        ReadOnly = false,
        Idempotent = false,
        Destructive = false),
        Description("Creates a new network")]
    public async Task<OperationResult> DockerCreateNetwork(
        [Description("Network name")] string name,
        [Description("Network driver (bridge, host, overlay, etc.)")] string driver = "bridge",
        [Description("Enable internal network")] bool isInternal = false,
        [Description("Enable attachable network")] bool attachable = false)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var response = await client.Networks.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = name,
                Driver = driver,
                Internal = isInternal,
                Attachable = attachable
            });

            return new OperationResult(success: true, data: new { id = response.ID });
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return new OperationResult(success: false, error: $"Network '{name}' already exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateNetwork failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Remove Network",
        ReadOnly = false,
        Idempotent = false,
        Destructive = true),
        Description("Removes a network")]
    public async Task<OperationResult> DockerRemoveNetwork(
        [Description("Network ID or name")] string networkId)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            await client.Networks.DeleteNetworkAsync(networkId);

            return new OperationResult(success: true);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Network '{networkId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveNetwork failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Connect Container to Network",
        ReadOnly = false,
        Idempotent = false,
        Destructive = false),
        Description("Connects a container to a network")]
    public async Task<OperationResult> DockerConnectContainer(
        [Description("Network ID or name")] string networkId,
        [Description("Container ID or name")] string containerId)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            await client.Networks.ConnectNetworkAsync(networkId, new NetworkConnectParameters
            {
                Container = containerId
            });

            return new OperationResult(success: true);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Network '{networkId}' or container '{containerId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConnectContainer failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Disconnect Container from Network",
        ReadOnly = false,
        Idempotent = false,
        Destructive = false),
        Description("Disconnects a container from a network")]
    public async Task<OperationResult> DockerDisconnectContainer(
        [Description("Network ID or name")] string networkId,
        [Description("Container ID or name")] string containerId,
        [Description("Force disconnection")] bool force = false)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            await client.Networks.DisconnectNetworkAsync(networkId, new NetworkDisconnectParameters
            {
                Container = containerId,
                Force = force
            });

            return new OperationResult(success: true);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Network '{networkId}' or container '{containerId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DisconnectContainer failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Prune Networks",
        ReadOnly = false,
        Idempotent = false,
        Destructive = true),
        Description("Removes unused networks")]
    public async Task<OperationResult> DockerPruneNetworks()
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var result = await client.Networks.PruneNetworksAsync();

            return new OperationResult(success: true, data: new
            {
                networksDeleted = result.NetworksDeleted
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PruneNetworks failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    #endregion

    #region Volume Operations - Read

    [McpServerTool(
        Title = "Docker: List Volumes",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Lists all Docker volumes")]
    public async Task<OperationResult> DockerListVolumes()
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var volumes = await client.Volumes.ListAsync();

            var result = volumes.Volumes.Select(v => new
            {
                name = v.Name,
                driver = v.Driver,
                mountpoint = v.Mountpoint,
                createdAt = v.CreatedAt,
                labels = v.Labels
            }).ToList();

            return new OperationResult(success: true, data: result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListVolumes failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Inspect Volume",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Gets detailed information about a volume")]
    public async Task<OperationResult> DockerInspectVolume(
        [Description("Volume name")] string volumeName)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var volume = await client.Volumes.InspectAsync(volumeName);

            var result = new
            {
                name = volume.Name,
                driver = volume.Driver,
                mountpoint = volume.Mountpoint,
                createdAt = volume.CreatedAt,
                labels = volume.Labels
            };

            return new OperationResult(success: true, data: result);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Volume '{volumeName}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InspectVolume failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    #endregion

    #region Volume Operations - Write

    [McpServerTool(
        Title = "Docker: Create Volume",
        ReadOnly = false,
        Idempotent = false,
        Destructive = false),
        Description("Creates a new volume")]
    public async Task<OperationResult> DockerCreateVolume(
        [Description("Volume name")] string name,
        [Description("Volume driver")] string driver = "local",
        [Description("Labels as JSON object")] string? labels = null)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();

            var parameters = new VolumesCreateParameters
            {
                Name = name,
                Driver = driver
            };

            if (!string.IsNullOrEmpty(labels))
            {
                parameters.Labels = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(labels);
            }

            var volume = await client.Volumes.CreateAsync(parameters);

            return new OperationResult(success: true, data: new
            {
                name = volume.Name,
                driver = volume.Driver,
                mountpoint = volume.Mountpoint
            });
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return new OperationResult(success: false, error: $"Volume '{name}' already exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateVolume failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Remove Volume",
        ReadOnly = false,
        Idempotent = false,
        Destructive = true),
        Description("Removes a volume")]
    public async Task<OperationResult> DockerRemoveVolume(
        [Description("Volume name")] string volumeName,
        [Description("Force removal even if volume is in use")] bool force = false)
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            await client.Volumes.RemoveAsync(volumeName, force);

            return new OperationResult(success: true);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new OperationResult(success: false, error: $"Volume '{volumeName}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveVolume failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    [McpServerTool(
        Title = "Docker: Prune Volumes",
        ReadOnly = false,
        Idempotent = false,
        Destructive = true),
        Description("Removes unused volumes")]
    public async Task<OperationResult> DockerPruneVolumes()
    {
        try
        {
            var client = await _dockerClientFactory.GetDockerClientAsync();
            var result = await client.Volumes.PruneAsync();

            return new OperationResult(success: true, data: new
            {
                spaceReclaimed = result.SpaceReclaimed,
                volumesDeleted = result.VolumesDeleted
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PruneVolumes failed: {Message}", ex.Message);
            return new OperationResult(success: false, error: ex.Message);
        }
    }

    #endregion
}
