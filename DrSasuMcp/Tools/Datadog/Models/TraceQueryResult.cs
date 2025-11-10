namespace DrSasuMcp.Tools.Datadog.Models
{
    /// <summary>
    /// Represents the result of a trace query.
    /// </summary>
    public class TraceQueryResult
    {
        public List<Trace> Traces { get; set; } = new();
        public string? Status { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Represents a distributed trace.
    /// </summary>
    public class Trace
    {
        public string? TraceId { get; set; }
        public List<Span> Spans { get; set; } = new();
        public DateTime? StartTime { get; set; }
        public long? Duration { get; set; }
        public string? Service { get; set; }
    }

    /// <summary>
    /// Represents a span in a trace.
    /// </summary>
    public class Span
    {
        public string? SpanId { get; set; }
        public string? TraceId { get; set; }
        public string? ParentId { get; set; }
        public string? Service { get; set; }
        public string? Name { get; set; }
        public string? Resource { get; set; }
        public DateTime? Start { get; set; }
        public long? Duration { get; set; }
        public string? Type { get; set; }
        public Dictionary<string, object>? Meta { get; set; }
        public Dictionary<string, double>? Metrics { get; set; }
    }
}

