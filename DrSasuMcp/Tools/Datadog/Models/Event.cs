namespace DrSasuMcp.Tools.Datadog.Models
{
    /// <summary>
    /// Represents a Datadog event.
    /// </summary>
    public class Event
    {
        public long? Id { get; set; }
        public string? Title { get; set; }
        public string? Text { get; set; }
        public string? AlertType { get; set; }
        public DateTime? DateHappened { get; set; }
        public string? Priority { get; set; }
        public string? Source { get; set; }
        public List<string>? Tags { get; set; }
        public string? Url { get; set; }
        public bool? IsAggregate { get; set; }
    }

    /// <summary>
    /// Represents an event query result.
    /// </summary>
    public class EventQueryResult
    {
        public List<Event> Events { get; set; } = new();
        public int? TotalCount { get; set; }
        public string? Status { get; set; }
        public string? Error { get; set; }
    }
}

