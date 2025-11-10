namespace DrSasuMcp.Tools.Datadog.Models
{
    /// <summary>
    /// Represents the result of a metric query.
    /// </summary>
    public class MetricQueryResult
    {
        public List<MetricSeries> Series { get; set; } = new();
        public string? Status { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Represents a metric series.
    /// </summary>
    public class MetricSeries
    {
        public string? Metric { get; set; }
        public Dictionary<string, string>? Tags { get; set; }
        public List<MetricPoint> Points { get; set; } = new();
        public object? Unit { get; set; } // Can be string or object
        public string? DisplayName { get; set; }
    }

    /// <summary>
    /// Represents a metric data point.
    /// </summary>
    public class MetricPoint
    {
        public long Timestamp { get; set; }
        public double? Value { get; set; }
    }
}

