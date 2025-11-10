namespace DrSasuMcp.Tools.Datadog.Models
{
    /// <summary>
    /// Represents a service map from Datadog.
    /// </summary>
    public class ServiceMap
    {
        public List<ServiceNode> Nodes { get; set; } = new();
        public List<ServiceEdge> Edges { get; set; } = new();
    }

    /// <summary>
    /// Represents a service node in the service map.
    /// </summary>
    public class ServiceNode
    {
        public string? Service { get; set; }
        public string? Type { get; set; }
        public Dictionary<string, object>? Attributes { get; set; }
        public ServiceHealth? Health { get; set; }
    }

    /// <summary>
    /// Represents a service edge (connection) in the service map.
    /// </summary>
    public class ServiceEdge
    {
        public string? From { get; set; }
        public string? To { get; set; }
        public string? Type { get; set; }
        public Dictionary<string, object>? Attributes { get; set; }
    }

    /// <summary>
    /// Represents service health information.
    /// </summary>
    public class ServiceHealth
    {
        public string? Status { get; set; }
        public double? ErrorRate { get; set; }
        public double? Latency { get; set; }
        public int? RequestCount { get; set; }
    }
}

