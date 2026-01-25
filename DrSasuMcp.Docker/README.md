# Docker & Container Management Tool for DrSasuMcp

A comprehensive Model Context Protocol (MCP) server tool for Docker container management. This tool allows AI assistants to manage Docker containers, images, networks, and volumes through natural language commands.

## Overview

The Docker Tool provides a complete suite of Docker operations through natural language commands. It supports local and remote Docker daemons and offers both read and write capabilities with appropriate safety flags.

## Features

### 🐳 Container Operations
- **List Containers** - List all containers (running, stopped, or all)
- **Inspect Container** - Get detailed container information
- **Get Container Logs** - Retrieve container logs with tail options
- **List Container Processes** - List processes running in a container
- **Start/Stop/Restart Container** - Control container lifecycle
- **Create Container** - Create new containers from images
- **Remove Container** - Delete containers (destructive)
- **Prune Containers** - Remove all stopped containers (destructive)

### 📦 Image Operations
- **List Images** - List all Docker images
- **Inspect Image** - Get detailed image information
- **Get Image History** - Show image layer history
- **Pull Image** - Download images from registries
- **Tag Image** - Tag images with new names
- **Remove Image** - Delete images (destructive)
- **Prune Images** - Remove unused images (destructive)

### 🌐 Network Operations
- **List Networks** - List all Docker networks
- **Inspect Network** - Get detailed network information
- **Create Network** - Create new networks
- **Remove Network** - Delete networks (destructive)
- **Connect Container** - Connect containers to networks
- **Disconnect Container** - Disconnect containers from networks
- **Prune Networks** - Remove unused networks (destructive)

### 💾 Volume Operations
- **List Volumes** - List all Docker volumes
- **Inspect Volume** - Get detailed volume information
- **Create Volume** - Create new volumes
- **Remove Volume** - Delete volumes (destructive)
- **Prune Volumes** - Remove unused volumes (destructive)

## Architecture

```
DrSasuMcp.Docker/
├── Program.cs                          # Server entry point
├── DrSasuMcp.Docker.csproj            # Project file
├── README.md                           # This file
└── Docker/
    ├── DockerTool.cs                   # MCP-exposed Docker operations
    ├── DockerToolConstants.cs          # Constants and defaults
    ├── IDockerClientFactory.cs         # Docker client factory interface
    ├── DockerClientFactory.cs          # Docker client factory implementation
    └── Models/
        ├── ContainerInfo.cs
        ├── ImageInfo.cs
        ├── NetworkInfo.cs
        ├── VolumeInfo.cs
        ├── ContainerStats.cs
        └── ContainerLogs.cs
```

## MCP Exposed Methods

### Container Operations

#### `DockerListContainers`
Lists all Docker containers.

**Parameters:**
- `all` (bool, default: false) - Show all containers (including stopped)
- `status` (string, optional) - Filter by status (running, exited, etc.)

**Example Usage:**
```
User: "Show me all running containers"
AI: Calls DockerListContainers(all: false)

User: "List all containers including stopped ones"
AI: Calls DockerListContainers(all: true)
```

---

#### `DockerInspectContainer`
Gets detailed information about a container.

**Parameters:**
- `containerId` (string, required) - Container ID or name

**Returns:** Detailed container info including state, config, mounts, ports, etc.

---

#### `DockerGetContainerLogs`
Retrieves logs from a container.

**Parameters:**
- `containerId` (string, required) - Container ID or name
- `tail` (int, default: 100) - Number of lines to show
- `timestamps` (bool, default: false) - Show timestamps

---

#### `DockerStartContainer` / `DockerStopContainer` / `DockerRestartContainer`
Controls container lifecycle.

**Parameters:**
- `containerId` (string, required) - Container ID or name
- `timeoutSeconds` (int, default: 10) - Timeout before killing (stop/restart only)

---

#### `DockerCreateContainer`
Creates a new container from an image.

**Parameters:**
- `image` (string, required) - Image name
- `name` (string, optional) - Container name
- `command` (string, optional) - Command to run
- `env` (string, optional) - Environment variables as JSON
- `ports` (string, optional) - Port mappings as JSON

**Example:**
```
User: "Create an nginx container with port 80 mapped to 8080"
AI: Calls DockerCreateContainer(image: "nginx:latest", name: "my-nginx", ports: "{\"80\":\"8080\"}")
```

---

#### `DockerRemoveContainer`
Removes a container (destructive).

**Parameters:**
- `containerId` (string, required) - Container ID or name
- `force` (bool, default: false) - Force removal

---

#### `DockerPruneContainers`
Removes all stopped containers (destructive).

---

### Image Operations

#### `DockerListImages`
Lists all Docker images.

**Parameters:**
- `all` (bool, default: false) - Show all images (including intermediate)

---

#### `DockerInspectImage`
Gets detailed information about an image.

**Parameters:**
- `imageId` (string, required) - Image ID or name

---

#### `DockerPullImage`
Downloads an image from a registry.

**Parameters:**
- `image` (string, required) - Image name (e.g., 'nginx:latest')

---

#### `DockerRemoveImage`
Removes an image (destructive).

**Parameters:**
- `imageId` (string, required) - Image ID or name
- `force` (bool, default: false) - Force removal

---

### Network Operations

#### `DockerListNetworks`
Lists all Docker networks.

---

#### `DockerInspectNetwork`
Gets detailed information about a network.

**Parameters:**
- `networkId` (string, required) - Network ID or name

---

#### `DockerCreateNetwork`
Creates a new network.

**Parameters:**
- `name` (string, required) - Network name
- `driver` (string, default: "bridge") - Network driver
- `isInternal` (bool, default: false) - Enable internal network
- `attachable` (bool, default: false) - Enable attachable network

---

#### `DockerConnectContainer` / `DockerDisconnectContainer`
Connects/disconnects containers to/from networks.

**Parameters:**
- `networkId` (string, required) - Network ID or name
- `containerId` (string, required) - Container ID or name

---

### Volume Operations

#### `DockerListVolumes`
Lists all Docker volumes.

---

#### `DockerInspectVolume`
Gets detailed information about a volume.

**Parameters:**
- `volumeName` (string, required) - Volume name

---

#### `DockerCreateVolume`
Creates a new volume.

**Parameters:**
- `name` (string, required) - Volume name
- `driver` (string, default: "local") - Volume driver
- `labels` (string, optional) - Labels as JSON

---

#### `DockerRemoveVolume`
Removes a volume (destructive).

**Parameters:**
- `volumeName` (string, required) - Volume name
- `force` (bool, default: false) - Force removal

---

## Configuration

### Environment Variables

#### Docker Connection

| Variable | Description | Default |
|----------|-------------|---------|
| `DOCKER_HOST` | Override Docker URI completely (highest priority) | (none) |
| `DOCKER_DEFAULT_WINDOWS_URI` | Default URI for Windows when `DOCKER_HOST` is not set | `npipe://./pipe/docker_engine` |
| `DOCKER_DEFAULT_UNIX_URI` | Default URI for Linux/Mac when `DOCKER_HOST` is not set | `unix:///var/run/docker.sock` |
| `DOCKER_TLS_VERIFY` | Enable TLS verification for remote connections | (none) |
| `DOCKER_CERT_PATH` | Path to Docker TLS certificates | (none) |

**URI Priority:**
1. `DOCKER_HOST` - If set, this is always used
2. `DOCKER_DEFAULT_WINDOWS_URI` / `DOCKER_DEFAULT_UNIX_URI` - Platform-specific defaults (if customized)
3. Built-in defaults - `npipe://./pipe/docker_engine` (Windows) or `unix:///var/run/docker.sock` (Unix)

### Configuration Examples

#### Local Docker (Default)
```json
{
  "mcpServers": {
    "drsasumcp-docker": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Projects\\personal\\DrSasuMcp\\DrSasuMcp.Docker\\DrSasuMcp.Docker.csproj"]
    }
  }
}
```

#### Custom Windows Named Pipe
```json
{
  "mcpServers": {
    "drsasumcp-docker": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Projects\\personal\\DrSasuMcp\\DrSasuMcp.Docker\\DrSasuMcp.Docker.csproj"],
      "env": {
        "DOCKER_DEFAULT_WINDOWS_URI": "npipe://./pipe/docker_engine"
      }
    }
  }
}
```

#### Remote Docker (TCP)
```json
{
  "mcpServers": {
    "drsasumcp-docker": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Projects\\personal\\DrSasuMcp\\DrSasuMcp.Docker\\DrSasuMcp.Docker.csproj"],
      "env": {
        "DOCKER_HOST": "tcp://192.168.1.100:2376"
      }
    }
  }
}
```

#### Custom Unix Socket
```json
{
  "mcpServers": {
    "drsasumcp-docker": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/DrSasuMcp.Docker.csproj"],
      "env": {
        "DOCKER_DEFAULT_UNIX_URI": "unix:///custom/path/docker.sock"
      }
    }
  }
}
```

#### Published Executable (Production)
```bash
# Publish
dotnet publish DrSasuMcp.Docker/DrSasuMcp.Docker.csproj -c Release -o ./publish/docker
```

```json
{
  "mcpServers": {
    "drsasumcp-docker": {
      "command": "C:\\Projects\\personal\\DrSasuMcp\\publish\\docker\\DrSasuMcp.Docker.exe"
    }
  }
}
```

---

## Docker Requirements

- **Docker Engine** 20.10+ (local or remote)
- **Docker API** access via local socket or TCP
- **Permissions** to execute Docker commands

---

## Operation Result Format

All operations return a standard `OperationResult` object:

### Successful Query
```json
{
  "success": true,
  "data": [
    { "id": "abc123", "name": "my-container", "status": "running" }
  ]
}
```

### Successful Write Operation
```json
{
  "success": true,
  "data": { "id": "new-container-id" }
}
```

### Error Response
```json
{
  "success": false,
  "error": "Container 'mycontainer' not found"
}
```

---

## Safety Features

### Operation Flags

| Operation | ReadOnly | Idempotent | Destructive |
|-----------|----------|------------|-------------|
| List/Inspect operations | ✅ | ✅ | ❌ |
| Start/Stop/Restart | ❌ | ❌ | ❌ |
| Create operations | ❌ | ❌ | ❌ |
| Connect/Disconnect | ❌ | ❌ | ❌ |
| Remove/Prune operations | ❌ | ❌ | ✅ |

---

## Usage Examples

### Container Management
```
User: "Show me all running containers"
AI: Calls DockerListContainers()

User: "Start the nginx container"
AI: Calls DockerStartContainer(containerId: "nginx")

User: "Stop all running containers"
AI: Lists containers, then calls DockerStopContainer for each
```

### Image Management
```
User: "Pull the latest nginx image"
AI: Calls DockerPullImage(image: "nginx:latest")

User: "List all images"
AI: Calls DockerListImages()

User: "Remove unused images"
AI: Calls DockerPruneImages()
```

### Network Management
```
User: "Create a network called my-network"
AI: Calls DockerCreateNetwork(name: "my-network")

User: "Connect my-container to my-network"
AI: Calls DockerConnectContainer(networkId: "my-network", containerId: "my-container")
```

---

## Error Handling

### Common Errors

**Container Not Found**
```
Error: "Container 'mycontainer' not found"
Solution: Verify container name/ID with DockerListContainers
```

**Image Not Found**
```
Error: "Image 'myimage' not found"
Solution: Pull the image first with DockerPullImage
```

**Docker Daemon Not Running**
```
Error: "Cannot connect to Docker daemon"
Solution: Start Docker service
```

**Permission Denied**
```
Error: "Permission denied"
Solution: Check Docker permissions
```

---

## Dependencies

- `Docker.DotNet` (^3.125.15) - Docker .NET client library
- `Microsoft.Extensions.Hosting` (^8.0.1) - Hosting framework
- `ModelContextProtocol` (^0.4.0-preview.3) - MCP server framework

---

## Security Considerations

1. **Docker Socket Access** - The tool requires access to Docker daemon
2. **Destructive Operations** - Remove/Prune operations are marked as destructive
3. **Credential Management** - Use environment variables for remote Docker credentials
4. **TLS** - Use TLS for remote Docker connections

---

## Troubleshooting

### Issue: "Cannot connect to Docker daemon"
**Solution:**
- Verify Docker is running: `docker info`
- Check DOCKER_HOST environment variable
- Verify Docker socket permissions

### Issue: "npipe URI is not valid" (Windows)
**Solution:**
- The Docker.DotNet library requires a specific URI format
- Set the `DOCKER_DEFAULT_WINDOWS_URI` environment variable in your MCP config:
```json
{
  "env": {
    "DOCKER_DEFAULT_WINDOWS_URI": "npipe://./pipe/docker_engine"
  }
}
```

### Issue: "Permission denied"
**Solution:**
- Add user to docker group: `sudo usermod -aG docker $USER`
- On Windows, run as Administrator

### Issue: "Image not found locally"
**Solution:**
- Pull the image first: `DockerPullImage(image: "nginx:latest")`

### Issue: Custom Docker socket location
**Solution:**
- Use `DOCKER_HOST` for complete override, or platform-specific variables:
  - Windows: `DOCKER_DEFAULT_WINDOWS_URI`
  - Linux/Mac: `DOCKER_DEFAULT_UNIX_URI`

---

## License

Part of the DrSasuMcp project. MIT License.

---

**Ready to use! Ensure Docker is running and start managing containers!** 🐳
