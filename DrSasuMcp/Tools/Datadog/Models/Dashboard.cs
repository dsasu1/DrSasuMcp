namespace DrSasuMcp.Tools.Datadog.Models
{
    /// <summary>
    /// Represents a Datadog dashboard.
    /// </summary>
    public class Dashboard
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? LayoutType { get; set; }
        public bool? IsReadOnly { get; set; }
        public List<DashboardWidget>? Widgets { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? AuthorName { get; set; }
    }

    /// <summary>
    /// Represents a dashboard widget.
    /// </summary>
    public class DashboardWidget
    {
        public int? Id { get; set; }
        public string? Type { get; set; }
        public Dictionary<string, object>? Definition { get; set; }
        public int? X { get; set; }
        public int? Y { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }
}

