namespace DrSasuMcp.AzureDevOps.AzureDevOps
{
    /// <summary>
    /// Constants and configuration values for the Azure DevOps tool.
    /// </summary>
    public static class AzureDevOpsToolConstants
    {
        // Environment Variables
        /// <summary>
        /// Environment variable name for Azure DevOps Personal Access Token.
        /// </summary>
        public const string EnvAzureDevOpsPat = "AZURE_DEVOPS_PAT";

        /// <summary>
        /// Environment variable name for default Azure DevOps organization.
        /// </summary>
        public const string EnvAzureDevOpsOrg = "AZURE_DEVOPS_ORG";

        /// <summary>
        /// Environment variable name for maximum number of files to analyze.
        /// </summary>
        public const string EnvAzureDevOpsMaxFiles = "AZURE_DEVOPS_MAX_FILES";

        /// <summary>
        /// Environment variable name for request timeout in seconds.
        /// </summary>
        public const string EnvAzureDevOpsTimeout = "AZURE_DEVOPS_TIMEOUT";

        /// <summary>
        /// Environment variable name for maximum number of work items to return from a query.
        /// </summary>
        public const string EnvAzureDevOpsMaxWorkItems = "AZURE_DEVOPS_MAX_WORK_ITEMS";

        /// <summary>
        /// Environment variable name for maximum file size to analyze in bytes.
        /// </summary>
        public const string EnvAzureDevOpsMaxFileSize = "AZURE_DEVOPS_MAX_FILE_SIZE";

        // Default Values
        /// <summary>
        /// Default timeout for HTTP requests in seconds.
        /// </summary>
        public const int DefaultTimeoutSeconds = 60;

        /// <summary>
        /// Default maximum number of files to analyze per PR.
        /// </summary>
        public const int DefaultMaxFiles = 100;

        /// <summary>
        /// Default maximum file size to analyze in bytes (1MB).
        /// </summary>
        public const int DefaultMaxFileSizeBytes = 1048576;

        /// <summary>
        /// Default maximum number of work items returned by a query.
        /// </summary>
        public const int DefaultMaxWorkItems = 100;

        /// <summary>
        /// Maximum number of work items the Azure DevOps batch endpoint accepts in a single request.
        /// </summary>
        public const int WorkItemBatchSize = 200;

        // API Configuration
        /// <summary>
        /// Azure DevOps REST API version.
        /// </summary>
        public const string ApiVersion = "7.1";

        /// <summary>
        /// Base URL template for Azure DevOps API.
        /// Format: org, project
        /// </summary>
        public const string BaseUrlTemplate = "https://dev.azure.com/{0}/{1}/_apis";

        /// <summary>
        /// Base URL template for organization-scoped Azure DevOps API calls.
        /// Format: org
        /// </summary>
        public const string OrganizationBaseUrlTemplate = "https://dev.azure.com/{0}/_apis";

        /// <summary>
        /// URL template for the browser view of a work item.
        /// Format: org, project, workItemId
        /// </summary>
        public const string WorkItemWebUrlTemplate = "https://dev.azure.com/{0}/{1}/_workitems/edit/{2}";

        /// <summary>
        /// URL template for the browser view of a work item when the project is unknown.
        /// Format: org, workItemId
        /// </summary>
        public const string WorkItemWebUrlNoProjectTemplate = "https://dev.azure.com/{0}/_workitems/edit/{1}";

        // API Endpoints
        /// <summary>
        /// Endpoint to get pull request details.
        /// Format: repositoryId, pullRequestId
        /// </summary>
        public const string GetPullRequestEndpoint = "/git/repositories/{0}/pullRequests/{1}";

        /// <summary>
        /// Endpoint to get pull request iterations.
        /// Format: repositoryId, pullRequestId
        /// </summary>
        public const string GetPullRequestIterationsEndpoint = "/git/repositories/{0}/pullRequests/{1}/iterations";

        /// <summary>
        /// Endpoint to get pull request changes for an iteration.
        /// Format: repositoryId, pullRequestId, iterationId
        /// </summary>
        public const string GetPullRequestChangesEndpoint = "/git/repositories/{0}/pullRequests/{1}/iterations/{2}/changes";

        /// <summary>
        /// Endpoint to get file content.
        /// Format: repositoryId
        /// </summary>
        public const string GetFileContentEndpoint = "/git/repositories/{0}/items";

        /// <summary>
        /// Endpoint to get a single work item.
        /// Format: workItemId
        /// </summary>
        public const string GetWorkItemEndpoint = "/wit/workitems/{0}";

        /// <summary>
        /// Endpoint to get several work items in one request via the ids query parameter.
        /// </summary>
        public const string GetWorkItemsBatchEndpoint = "/wit/workitems";

        /// <summary>
        /// Endpoint to run a WIQL query.
        /// </summary>
        public const string QueryWorkItemsEndpoint = "/wit/wiql";

        /// <summary>
        /// Endpoint to get the work items linked to a pull request.
        /// Format: repositoryId, pullRequestId
        /// </summary>
        public const string GetPullRequestWorkItemsEndpoint = "/git/repositories/{0}/pullRequests/{1}/workitems";

        // Analysis Configuration
        /// <summary>
        /// Maximum recommended method length in lines.
        /// </summary>
        public const int MaxMethodLength = 50;

        /// <summary>
        /// Maximum recommended class length in lines.
        /// </summary>
        public const int MaxClassLength = 500;

        /// <summary>
        /// Maximum recommended cyclomatic complexity.
        /// </summary>
        public const int MaxCyclomaticComplexity = 10;

        // Issue Code Prefixes
        /// <summary>
        /// Prefix for security issue codes.
        /// </summary>
        public const string SecurityIssuePrefix = "SEC";

        /// <summary>
        /// Prefix for code quality issue codes.
        /// </summary>
        public const string QualityIssuePrefix = "QUAL";

        /// <summary>
        /// Prefix for best practice issue codes.
        /// </summary>
        public const string BestPracticeIssuePrefix = "BP";

        /// <summary>
        /// Reference names of the work item fields mapped by the work item tools.
        /// </summary>
        public static class WorkItemFields
        {
            public const string WorkItemType = "System.WorkItemType";
            public const string Title = "System.Title";
            public const string State = "System.State";
            public const string Reason = "System.Reason";
            public const string AssignedTo = "System.AssignedTo";
            public const string CreatedBy = "System.CreatedBy";
            public const string CreatedDate = "System.CreatedDate";
            public const string ChangedBy = "System.ChangedBy";
            public const string ChangedDate = "System.ChangedDate";
            public const string AreaPath = "System.AreaPath";
            public const string IterationPath = "System.IterationPath";
            public const string TeamProject = "System.TeamProject";
            public const string Tags = "System.Tags";
            public const string Description = "System.Description";
            public const string Parent = "System.Parent";
            public const string Priority = "Microsoft.VSTS.Common.Priority";
            public const string Severity = "Microsoft.VSTS.Common.Severity";
            public const string AcceptanceCriteria = "Microsoft.VSTS.Common.AcceptanceCriteria";
            public const string ReproSteps = "Microsoft.VSTS.TCM.ReproSteps";
            public const string StoryPoints = "Microsoft.VSTS.Scheduling.StoryPoints";
            public const string Effort = "Microsoft.VSTS.Scheduling.Effort";
            public const string RemainingWork = "Microsoft.VSTS.Scheduling.RemainingWork";
            public const string CompletedWork = "Microsoft.VSTS.Scheduling.CompletedWork";
        }
    }
}

