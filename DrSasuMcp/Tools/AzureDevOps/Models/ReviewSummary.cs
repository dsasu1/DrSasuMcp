namespace DrSasuMcp.Tools.AzureDevOps.Models
{
    /// <summary>
    /// Represents a complete pull request review summary.
    /// </summary>
    public class ReviewSummary
    {
        /// <summary>
        /// Gets or sets the pull request information.
        /// </summary>
        public PullRequestInfo PullRequestInfo { get; set; } = new();

        /// <summary>
        /// Gets or sets the number of files changed.
        /// </summary>
        public int FilesChanged { get; set; }

        /// <summary>
        /// Gets or sets the total number of lines added.
        /// </summary>
        public int TotalAdditions { get; set; }

        /// <summary>
        /// Gets or sets the total number of lines deleted.
        /// </summary>
        public int TotalDeletions { get; set; }

        /// <summary>
        /// Gets or sets the number of critical issues found.
        /// </summary>
        public int CriticalIssues { get; set; }

        /// <summary>
        /// Gets or sets the number of warnings found.
        /// </summary>
        public int Warnings { get; set; }

        /// <summary>
        /// Gets or sets the number of informational suggestions.
        /// </summary>
        public int Suggestions { get; set; }

        /// <summary>
        /// Gets or sets the list of file reviews.
        /// </summary>
        public List<FileReview> FileReviews { get; set; } = new();

        /// <summary>
        /// Gets or sets the overall assessment of the pull request.
        /// </summary>
        public string OverallAssessment { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the review was performed.
        /// </summary>
        public DateTime ReviewedAt { get; set; }

        /// <summary>
        /// Gets or sets the time taken to perform the review in milliseconds.
        /// </summary>
        public int ReviewTimeMs { get; set; }
    }

    /// <summary>
    /// Represents a review of a single file.
    /// </summary>
    public class FileReview
    {
        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of change.
        /// </summary>
        public ChangeType ChangeType { get; set; }

        /// <summary>
        /// Gets or sets the number of lines added.
        /// </summary>
        public int Additions { get; set; }

        /// <summary>
        /// Gets or sets the number of lines deleted.
        /// </summary>
        public int Deletions { get; set; }

        /// <summary>
        /// Gets or sets the list of review comments for this file.
        /// </summary>
        public List<ReviewComment> Comments { get; set; } = new();
    }
}

