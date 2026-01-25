namespace DrSasuMcp.Docker.Docker.Models;

/// <summary>
/// Container information model
/// </summary>
public class ContainerInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public Dictionary<string, string> Labels { get; set; } = new();
    public List<string> Ports { get; set; } = new();
}
