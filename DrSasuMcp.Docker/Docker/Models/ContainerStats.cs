namespace DrSasuMcp.Docker.Docker.Models;

/// <summary>
/// Container statistics model
/// </summary>
public class ContainerStats
{
    public DateTime Read { get; set; }
    public long MemoryUsage { get; set; }
    public long MemoryLimit { get; set; }
    public double CpuPercent { get; set; }
    public long NetworkRxBytes { get; set; }
    public long NetworkTxBytes { get; set; }
    public long BlockReadBytes { get; set; }
    public long BlockWriteBytes { get; set; }
}
