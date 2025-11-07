namespace DrSasuMcp.Tools.AzureDevOps.Models
{
    /// <summary>
    /// Represents the severity level of a code review issue.
    /// </summary>
    public enum IssueLevel
    {
        /// <summary>
        /// Informational - suggestion or minor improvement.
        /// </summary>
        Info,

        /// <summary>
        /// Warning - should be addressed but not blocking.
        /// </summary>
        Warning,

        /// <summary>
        /// Critical - must be addressed before merge.
        /// </summary>
        Critical
    }
}

