namespace DrSasuMcp.AzureDevOps.AzureDevOps.Models
{
    /// <summary>
    /// Represents an Azure DevOps wiki page and its markdown content.
    /// </summary>
    public class WikiPageContent
    {
        /// <summary>
        /// Gets or sets the page ID.
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the page path within the wiki, e.g. /Engineering/Onboarding.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the page content as markdown.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the content was cut short by the configured size limit.
        /// </summary>
        public bool ContentTruncated { get; set; }

        /// <summary>
        /// Gets or sets the wiki identifier the page was read from.
        /// </summary>
        public string WikiName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the project name.
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public string Organization { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path of the markdown file in the backing Git repository.
        /// </summary>
        public string GitItemPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the page has sub-pages.
        /// </summary>
        public bool IsParentPage { get; set; }

        /// <summary>
        /// Gets or sets the browser URL of the page.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the direct sub-pages, populated only when requested.
        /// </summary>
        public List<WikiPageSummary> SubPages { get; set; } = new();
    }
}
