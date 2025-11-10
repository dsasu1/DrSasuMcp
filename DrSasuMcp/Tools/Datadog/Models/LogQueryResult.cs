namespace DrSasuMcp.Tools.Datadog.Models
{
    /// <summary>
    /// Represents the result of a log query.
    /// </summary>
    public class LogQueryResult
    {
        public List<LogEvent> Events { get; set; } = new();
        public LogMeta? Meta { get; set; }
        public string? Status { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Represents a log event.
    /// </summary>
    public class LogEvent
    {
        public string? Id { get; set; }
        public string? Content { get; set; }
        public Dictionary<string, object>? Attributes { get; set; }
        public Dictionary<string, string>? Tags { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? Service { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>
    /// Represents metadata for a log query result.
    /// </summary>
    public class LogMeta
    {
        public int? Elapsed { get; set; }
        public object? Page { get; set; } // Can be int or string
        public int? PageCount { get; set; }
        public int? TotalCount { get; set; }
    }
}

