using DrSasuMcp.Tools.AzureDevOps.Models;

namespace DrSasuMcp.Tools.AzureDevOps
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
        /// Tests the connection to Azure DevOps using the configured PAT.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the connection is successful, false otherwise.</returns>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    }
}

