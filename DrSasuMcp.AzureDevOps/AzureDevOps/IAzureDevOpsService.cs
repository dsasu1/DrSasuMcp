using DrSasuMcp.AzureDevOps.AzureDevOps.Models;

namespace DrSasuMcp.AzureDevOps.AzureDevOps
{
    /// <summary>
    /// Service interface for interacting with Azure DevOps REST API.
    /// </summary>
    public interface IAzureDevOpsService
    {
        /// <summary>
        /// Gets detailed information about a pull request.
        /// </summary>
        /// <param name="organization">The Azure DevOps organization name.</param>
        /// <param name="project">The project name.</param>
        /// <param name="repository">The repository name.</param>
        /// <param name="pullRequestId">The pull request ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Pull request information.</returns>
        Task<PullRequestInfo> GetPullRequestInfoAsync(
            string organization,
            string project,
            string repository,
            int pullRequestId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all file changes for a pull request.
        /// </summary>
        /// <param name="organization">The Azure DevOps organization name.</param>
        /// <param name="project">The project name.</param>
        /// <param name="repository">The repository name.</param>
        /// <param name="pullRequestId">The pull request ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of file changes.</returns>
        Task<List<FileChange>> GetPullRequestChangesAsync(
            string organization,
            string project,
            string repository,
            int pullRequestId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the content of a file at a specific commit.
        /// </summary>
        /// <param name="organization">The Azure DevOps organization name.</param>
        /// <param name="project">The project name.</param>
        /// <param name="repository">The repository name.</param>
        /// <param name="path">The file path.</param>
        /// <param name="commitId">The commit ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The file content as a string.</returns>
        Task<string> GetFileContentAsync(
            string organization,
            string project,
            string repository,
            string path,
            string commitId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the number of file changes in a pull request without fetching file content.
        /// </summary>
        /// <param name="organization">The Azure DevOps organization name.</param>
        /// <param name="project">The project name.</param>
        /// <param name="repository">The repository name.</param>
        /// <param name="pullRequestId">The pull request ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Count of changed files (excluding folders).</returns>
        Task<int> GetPullRequestChangesCountAsync(
            string organization,
            string project,
            string repository,
            int pullRequestId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the work item IDs linked to a pull request.
        /// </summary>
        /// <param name="organization">The Azure DevOps organization name.</param>
        /// <param name="project">The project name.</param>
        /// <param name="repository">The repository name.</param>
        /// <param name="pullRequestId">The pull request ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of linked work item IDs.</returns>
        Task<List<int>> GetPullRequestWorkItemIdsAsync(
            string organization,
            string project,
            string repository,
            int pullRequestId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets detailed information about a single work item.
        /// </summary>
        /// <param name="organization">The Azure DevOps organization name.</param>
        /// <param name="project">The project name, or null to resolve the work item at organization scope.</param>
        /// <param name="workItemId">The work item ID.</param>
        /// <param name="includeRelations">Whether to include work item links such as parent, child, and related items.</param>
        /// <param name="additionalFields">Reference names of extra fields to include in the result.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Work item information.</returns>
        Task<WorkItemInfo> GetWorkItemAsync(
            string organization,
            string? project,
            int workItemId,
            bool includeRelations = false,
            IEnumerable<string>? additionalFields = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets several work items in as few requests as possible.
        /// Work items that cannot be read are omitted from the result rather than failing the request.
        /// </summary>
        /// <param name="organization">The Azure DevOps organization name.</param>
        /// <param name="project">The project name, or null to resolve the work items at organization scope.</param>
        /// <param name="workItemIds">The work item IDs to fetch.</param>
        /// <param name="additionalFields">Reference names of extra fields to include in the results.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Work item information, ordered to match the requested IDs.</returns>
        Task<List<WorkItemInfo>> GetWorkItemsAsync(
            string organization,
            string? project,
            IEnumerable<int> workItemIds,
            IEnumerable<string>? additionalFields = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs a WIQL query and returns the matching work items.
        /// </summary>
        /// <param name="organization">The Azure DevOps organization name.</param>
        /// <param name="project">The project name to scope the query to, or null for organization scope.</param>
        /// <param name="wiql">The WIQL query text.</param>
        /// <param name="top">Maximum number of work items to return.</param>
        /// <param name="additionalFields">Reference names of extra fields to include in the results.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The query result including hydrated work items.</returns>
        Task<WorkItemQueryResult> QueryWorkItemsAsync(
            string organization,
            string? project,
            string wiql,
            int top,
            IEnumerable<string>? additionalFields = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests the connection to Azure DevOps using the configured PAT.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the connection is successful, false otherwise.</returns>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the organization configured through the AZURE_DEVOPS_ORG environment variable, if any.
        /// </summary>
        /// <returns>The default organization name, or null when it is not configured.</returns>
        string? GetDefaultOrganization();

        /// <summary>
        /// Gets the maximum number of work items a single query may return.
        /// </summary>
        /// <returns>The configured work item limit.</returns>
        int GetMaxWorkItems();
    }
}

