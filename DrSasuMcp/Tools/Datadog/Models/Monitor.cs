namespace DrSasuMcp.Tools.Datadog.Models
{
    /// <summary>
    /// Represents a Datadog monitor.
    /// </summary>
    public class Monitor
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Query { get; set; }
        public string? Message { get; set; }
        public string? Status { get; set; }
        public MonitorOptions? Options { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Modified { get; set; }
    }

    /// <summary>
    /// Represents monitor options.
    /// </summary>
    public class MonitorOptions
    {
        public bool? NotifyNoData { get; set; }
        public int? NoDataTimeframe { get; set; }
        public bool? NotifyAudit { get; set; }
        public bool? RequireFullWindow { get; set; }
        public List<string>? NotifyTags { get; set; }
    }
}

