namespace DrSasuMcp.AzureDevOps.AzureDevOps.Models
{
    /// <summary>
    /// Represents the outcome of a WIQL work item query.
    /// </summary>
    public class WorkItemQueryResult
    {
        /// <summary>
        /// Gets or sets the query type reported by Azure DevOps (flat, tree, or oneHop).
        /// </summary>
        public string QueryType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the point in time the query was evaluated against.
        /// </summary>
        public DateTime? AsOf { get; set; }

        /// <summary>
        /// Gets or sets the number of work items the query matched, after the requested limit is applied.
        /// This can exceed the number of returned work items when some of them are no longer readable.
        /// </summary>
        public int MatchedCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the query matched more work items than the requested limit.
        /// </summary>
        public bool Truncated { get; set; }

        /// <summary>
        /// Gets or sets the work items returned for the query.
        /// </summary>
        public List<WorkItemInfo> WorkItems { get; set; } = new();
    }
}
