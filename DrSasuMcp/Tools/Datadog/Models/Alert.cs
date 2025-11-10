namespace DrSasuMcp.Tools.Datadog.Models
{
    /// <summary>
    /// Represents a Datadog alert.
    /// </summary>
    public class Alert
    {
        public long MonitorId { get; set; }
        public string? MonitorName { get; set; }
        public string? Status { get; set; }
        public DateTime? TriggeredAt { get; set; }
        public string? Message { get; set; }
        public Dictionary<string, object>? Tags { get; set; }
    }
}

