namespace DrSasuMcp.Tools.Datadog.Models
{
    /// <summary>
    /// Represents metadata for a metric.
    /// </summary>
    public class MetricMetadata
    {
        public string? Metric { get; set; }
        public string? Type { get; set; }
        public string? Unit { get; set; }
        public string? Description { get; set; }
        public List<string> Tags { get; set; } = new();
    }
}

