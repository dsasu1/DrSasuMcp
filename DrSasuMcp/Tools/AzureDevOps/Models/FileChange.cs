namespace DrSasuMcp.Tools.AzureDevOps.Models
{
    /// <summary>
    /// Represents a file change in a pull request.
    /// </summary>
    public class FileChange
    {
        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of change (Added, Modified, Deleted, Renamed).
        /// </summary>
        public ChangeType ChangeType { get; set; }

        /// <summary>
        /// Gets or sets the original file content (before changes).
        /// </summary>
        public string? OriginalContent { get; set; }

        /// <summary>
        /// Gets or sets the modified file content (after changes).
        /// </summary>
        public string? ModifiedContent { get; set; }

        /// <summary>
        /// Gets or sets the original commit ID.
        /// </summary>
        public string? OriginalCommitId { get; set; }

        /// <summary>
        /// Gets or sets the modified commit ID.
        /// </summary>
        public string? ModifiedCommitId { get; set; }

        /// <summary>
        /// Gets or sets the number of lines added.
        /// </summary>
        public int Additions { get; set; }

        /// <summary>
        /// Gets or sets the number of lines deleted.
        /// </summary>
        public int Deletions { get; set; }

        /// <summary>
        /// Gets or sets whether the file is binary.
        /// </summary>
        public bool IsBinary { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        public long? FileSize { get; set; }
    }
}

