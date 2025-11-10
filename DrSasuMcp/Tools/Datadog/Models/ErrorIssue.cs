namespace DrSasuMcp.Tools.Datadog.Models
{
    /// <summary>
    /// Represents an error issue from Datadog error tracking.
    /// </summary>
    public class ErrorIssue
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Message { get; set; }
        public string? Service { get; set; }
        public string? Source { get; set; }
        public int? Count { get; set; }
        public DateTime? FirstSeen { get; set; }
        public DateTime? LastSeen { get; set; }
        public string? Status { get; set; }
        public List<string>? Tags { get; set; }
        public Dictionary<string, object>? Attributes { get; set; }
    }

    /// <summary>
    /// Represents error tracking query result.
    /// </summary>
    public class ErrorTrackingResult
    {
        public List<ErrorIssue> Issues { get; set; } = new();
        public int? TotalCount { get; set; }
        public string? Status { get; set; }
        public string? Error { get; set; }
    }
}

