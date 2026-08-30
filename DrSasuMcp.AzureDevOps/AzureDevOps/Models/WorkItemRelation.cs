namespace DrSasuMcp.AzureDevOps.AzureDevOps.Models
{
    /// <summary>
    /// Represents a link between a work item and another artifact.
    /// </summary>
    public class WorkItemRelation
    {
        /// <summary>
        /// Gets or sets the relation type reference name (e.g., System.LinkTypes.Hierarchy-Forward).
        /// </summary>
        public string RelationType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the friendly relation name (e.g., Parent, Child, Pull Request).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL of the linked artifact.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the linked work item, or null when the link points at another artifact type
        /// such as a commit, pull request, or external hyperlink.
        /// </summary>
        public int? TargetWorkItemId { get; set; }
    }
}
