namespace DrSasuMcp.AzureDevOps.AzureDevOps.Models
{
    /// <summary>
    /// Represents metadata about a work item.
    /// </summary>
    public class WorkItemInfo
    {
        /// <summary>
        /// Gets or sets the work item ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the work item revision number.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// Gets or sets the work item type (e.g., Bug, Task, User Story).
        /// </summary>
        public string WorkItemType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the work item title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the work item state (e.g., New, Active, Closed).
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reason for the current state.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the assignee, or an empty string when unassigned.
        /// </summary>
        public string AssignedTo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the creator.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation date of the work item.
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the display name of the last person to change the work item.
        /// </summary>
        public string ChangedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date the work item was last changed.
        /// </summary>
        public DateTime? ChangedDate { get; set; }

        /// <summary>
        /// Gets or sets the area path.
        /// </summary>
        public string AreaPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the iteration path.
        /// </summary>
        public string IterationPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the project the work item belongs to.
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization the work item belongs to.
        /// </summary>
        public string Organization { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tags applied to the work item.
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Gets or sets the work item priority, if set.
        /// </summary>
        public int? Priority { get; set; }

        /// <summary>
        /// Gets or sets the severity, if set (bugs only in most processes).
        /// </summary>
        public string? Severity { get; set; }

        /// <summary>
        /// Gets or sets the description. Azure DevOps stores this as HTML for most work item types.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the acceptance criteria, if set.
        /// </summary>
        public string? AcceptanceCriteria { get; set; }

        /// <summary>
        /// Gets or sets the reproduction steps, if set (bugs only in most processes).
        /// </summary>
        public string? ReproSteps { get; set; }

        /// <summary>
        /// Gets or sets the story points estimate, if set.
        /// </summary>
        public double? StoryPoints { get; set; }

        /// <summary>
        /// Gets or sets the effort estimate, if set.
        /// </summary>
        public double? Effort { get; set; }

        /// <summary>
        /// Gets or sets the remaining work in hours, if set.
        /// </summary>
        public double? RemainingWork { get; set; }

        /// <summary>
        /// Gets or sets the completed work in hours, if set.
        /// </summary>
        public double? CompletedWork { get; set; }

        /// <summary>
        /// Gets or sets the ID of the parent work item, if any.
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// Gets or sets the browser URL of the work item.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the work item links, populated only when relations are requested.
        /// </summary>
        public List<WorkItemRelation> Relations { get; set; } = new();

        /// <summary>
        /// Gets or sets additional fields explicitly requested by the caller, keyed by field reference name.
        /// </summary>
        public Dictionary<string, string> AdditionalFields { get; set; } = new();
    }
}
