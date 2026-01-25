namespace DrSasuMcp.Docker.Docker.Models;

/// <summary>
/// Image information model
/// </summary>
public class ImageInfo
{
    public string Id { get; set; } = string.Empty;
    public List<string> RepoTags { get; set; } = new();
    public List<string> RepoDigests { get; set; } = new();
    public long Size { get; set; }
    public long VirtualSize { get; set; }
    public DateTime Created { get; set; }
    public Dictionary<string, string> Labels { get; set; } = new();
}
