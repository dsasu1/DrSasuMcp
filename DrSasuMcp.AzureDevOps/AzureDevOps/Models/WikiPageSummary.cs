namespace DrSasuMcp.AzureDevOps.AzureDevOps.Models
{
    /// <summary>
    /// Represents a wiki page listed without its content, such as a sub-page.
    /// </summary>
    public class WikiPageSummary
    {
        /// <summary>
        /// Gets or sets the page ID.
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the page path within the wiki.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the page has sub-pages of its own.
        /// </summary>
        public bool IsParentPage { get; set; }

        /// <summary>
        /// Gets or sets the browser URL of the page.
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }
}
