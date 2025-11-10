namespace DrSasuMcp.Tools.Datadog.Models
{
    /// <summary>
    /// Represents a troubleshooting recommendation.
    /// </summary>
    public class TroubleshootingRecommendation
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Info"; // Critical, Warning, Info
        public string Category { get; set; } = string.Empty; // Performance, Error, Configuration, etc.
        public List<string> Steps { get; set; } = new();
        public Dictionary<string, string>? RelatedMetrics { get; set; }
        public Dictionary<string, string>? RelatedLogs { get; set; }
        public Dictionary<string, string>? RelatedTraces { get; set; }
        public double? Confidence { get; set; } // 0.0 to 1.0
    }

    /// <summary>
    /// Represents the context for troubleshooting analysis.
    /// </summary>
    public class TroubleshootingContext
    {
        public string IssueDescription { get; set; } = string.Empty;
        public string? ServiceName { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// Represents the result of troubleshooting analysis.
    /// </summary>
    public class TroubleshootingResult
    {
        public string IssueDescription { get; set; } = string.Empty;
        public List<TroubleshootingRecommendation> Recommendations { get; set; } = new();
        public Dictionary<string, object>? AnalysisData { get; set; }
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public int TotalRecommendations => Recommendations.Count;
        public int CriticalRecommendations => Recommendations.Count(r => r.Severity == "Critical");
        public int WarningRecommendations => Recommendations.Count(r => r.Severity == "Warning");
    }
}

