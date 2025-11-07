namespace DrSasuMcp.Tools.AzureDevOps.Models
{
    /// <summary>
    /// Represents the result of a diff operation.
    /// </summary>
    public class DiffResultModel
    {
        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the diff format (unified, sidebyside, inline).
        /// </summary>
        public string DiffFormat { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the diff content as a string.
        /// </summary>
        public string DiffContent { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the diff statistics.
        /// </summary>
        public DiffStatistics Statistics { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of diff lines.
        /// </summary>
        public List<DiffLine> Lines { get; set; } = new();
    }

    /// <summary>
    /// Represents statistics about a diff.
    /// </summary>
    public class DiffStatistics
    {
        /// <summary>
        /// Gets or sets the total number of lines.
        /// </summary>
        public int TotalLines { get; set; }

        /// <summary>
        /// Gets or sets the number of lines added.
        /// </summary>
        public int AddedLines { get; set; }

        /// <summary>
        /// Gets or sets the number of lines deleted.
        /// </summary>
        public int DeletedLines { get; set; }

        /// <summary>
        /// Gets or sets the number of lines modified.
        /// </summary>
        public int ModifiedLines { get; set; }

        /// <summary>
        /// Gets or sets the number of unchanged lines.
        /// </summary>
        public int UnchangedLines { get; set; }

        /// <summary>
        /// Gets or sets the percentage of lines changed.
        /// </summary>
        public double ChangePercentage { get; set; }
    }

    /// <summary>
    /// Represents a single line in a diff.
    /// </summary>
    public class DiffLine
    {
        /// <summary>
        /// Gets or sets the line number in the old file.
        /// </summary>
        public int? OldLineNumber { get; set; }

        /// <summary>
        /// Gets or sets the line number in the new file.
        /// </summary>
        public int? NewLineNumber { get; set; }

        /// <summary>
        /// Gets or sets the line content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of diff line (Unchanged, Added, Deleted, Modified).
        /// </summary>
        public DiffLineType Type { get; set; }
    }

    /// <summary>
    /// Represents the type of a diff line.
    /// </summary>
    public enum DiffLineType
    {
        /// <summary>
        /// Line is unchanged.
        /// </summary>
        Unchanged,

        /// <summary>
        /// Line was added.
        /// </summary>
        Added,

        /// <summary>
        /// Line was deleted.
        /// </summary>
        Deleted,

        /// <summary>
        /// Line was modified.
        /// </summary>
        Modified
    }
}

