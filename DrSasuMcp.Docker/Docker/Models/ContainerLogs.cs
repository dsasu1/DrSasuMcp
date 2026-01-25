namespace DrSasuMcp.Docker.Docker.Models;

/// <summary>
/// Container logs model
/// </summary>
public class ContainerLogs
{
    public string Logs { get; set; } = string.Empty;
    public int LineCount { get; set; }
}
