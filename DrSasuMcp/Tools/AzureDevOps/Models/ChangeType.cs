namespace DrSasuMcp.Tools.AzureDevOps.Models
{
    /// <summary>
    /// Represents the type of change made to a file in a pull request.
    /// </summary>
    public enum ChangeType
    {
        /// <summary>
        /// File was added.
        /// </summary>
        Added,

        /// <summary>
        /// File was modified.
        /// </summary>
        Modified,

        /// <summary>
        /// File was deleted.
        /// </summary>
        Deleted,

        /// <summary>
        /// File was renamed.
        /// </summary>
        Renamed
    }
}

