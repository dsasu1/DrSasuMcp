namespace DrSasuMcp.Docker.Docker.Models;

/// <summary>
/// Volume information model
/// </summary>
public class VolumeInfo
{
    public string Name { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string Mountpoint { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, string> Labels { get; set; } = new();
    public Dictionary<string, object> Options { get; set; } = new();
    public Dictionary<string, object> Status { get; set; } = new();
}
