namespace DrSasuMcp.Tools.AzureDevOps.Models
{
    /// <summary>
    /// Represents a code review comment.
    /// </summary>
    public class ReviewComment
    {
        /// <summary>
        /// Gets or sets the file path where the comment applies.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the line number in the file (null for file-level comments).
        /// </summary>
        public int? Line { get; set; }

        /// <summary>
        /// Gets or sets the severity level of the issue.
        /// </summary>
        public IssueLevel Level { get; set; }

        /// <summary>
        /// Gets or sets the name of the analyzer that generated this comment.
        /// </summary>
        public string Analyzer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the comment message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the issue code (e.g., SEC001, QUAL002).
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional suggestion for fixing the issue.
        /// </summary>
        public string? Suggestion { get; set; }

        /// <summary>
        /// Gets or sets an optional code snippet showing the issue.
        /// </summary>
        public string? CodeSnippet { get; set; }
    }
}

