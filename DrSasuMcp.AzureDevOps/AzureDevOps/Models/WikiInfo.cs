namespace DrSasuMcp.AzureDevOps.AzureDevOps.Models
{
    /// <summary>
    /// Represents an Azure DevOps wiki.
    /// </summary>
    public class WikiInfo
    {
        /// <summary>
        /// Gets or sets the wiki ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the wiki name, which can be used as the wiki identifier in page lookups.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the wiki type (projectWiki or codeWiki).
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the project the wiki belongs to.
        /// </summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the Git repository backing the wiki.
        /// </summary>
        public string RepositoryId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the repository folder published as the wiki, for code wikis.
        /// </summary>
        public string MappedPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the branches published for the wiki.
        /// </summary>
        public List<string> Versions { get; set; } = new();

        /// <summary>
        /// Gets or sets the browser URL of the wiki.
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }
}
