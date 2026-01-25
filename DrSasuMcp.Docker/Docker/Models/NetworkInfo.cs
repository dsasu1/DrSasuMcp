namespace DrSasuMcp.Docker.Docker.Models;

/// <summary>
/// Network information model
/// </summary>
public class NetworkInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public bool Internal { get; set; }
    public bool Attachable { get; set; }
    public bool Ingress { get; set; }
    public Dictionary<string, string> Labels { get; set; } = new();
    public Dictionary<string, object> Options { get; set; } = new();
}
