namespace DrSasuMcp.AzureDevOps.AzureDevOps.Models
{
    /// <summary>
    /// Represents the work items linked to a pull request.
    /// </summary>
    public class PullRequestWorkItems
    {
        /// <summary>
        /// Gets or sets the pull request ID.
        /// </summary>
        public int PullRequestId { get; set; }

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public string Organization { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the project name.
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string RepositoryName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of work items linked to the pull request.
        /// </summary>
        public int LinkedCount { get; set; }

        /// <summary>
        /// Gets or sets the linked work items.
        /// </summary>
        public List<WorkItemInfo> WorkItems { get; set; } = new();
    }
}
