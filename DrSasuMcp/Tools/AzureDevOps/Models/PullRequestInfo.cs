namespace DrSasuMcp.Tools.AzureDevOps.Models
{
    /// <summary>
    /// Represents metadata about a pull request.
    /// </summary>
    public class PullRequestInfo
    {
        /// <summary>
        /// Gets or sets the pull request ID.
        /// </summary>
        public int PullRequestId { get; set; }

        /// <summary>
        /// Gets or sets the pull request title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the pull request description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the author's display name.
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the author's email address.
        /// </summary>
        public string AuthorEmail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the pull request status (e.g., Active, Completed, Abandoned).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation date of the pull request.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the source branch name.
        /// </summary>
        public string SourceBranch { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target branch name.
        /// </summary>
        public string TargetBranch { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string RepositoryName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the project name.
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public string Organization { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of files changed.
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Gets or sets the list of reviewer names.
        /// </summary>
        public List<string> Reviewers { get; set; } = new();
    }
}

