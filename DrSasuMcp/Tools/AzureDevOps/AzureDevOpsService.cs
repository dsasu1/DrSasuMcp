using DrSasuMcp.Tools.AzureDevOps.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DrSasuMcp.Tools.AzureDevOps
{
    /// <summary>
    /// Implementation of Azure DevOps REST API client.
    /// </summary>
    public class AzureDevOpsService : IAzureDevOpsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AzureDevOpsService> _logger;
        private readonly string? _personalAccessToken;
        private readonly int _maxFiles;

        public AzureDevOpsService(ILogger<AzureDevOpsService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();

            // Get configuration from environment variables
            _personalAccessToken = Environment.GetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsPat);
            _maxFiles = GetIntFromEnv(AzureDevOpsToolConstants.EnvAzureDevOpsMaxFiles, AzureDevOpsToolConstants.DefaultMaxFiles);

            var timeoutSeconds = GetIntFromEnv(AzureDevOpsToolConstants.EnvAzureDevOpsTimeout, AzureDevOpsToolConstants.DefaultTimeoutSeconds);
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            // Set up authentication if PAT is available
            if (!string.IsNullOrWhiteSpace(_personalAccessToken))
            {
                var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_personalAccessToken}"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            }

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <inheritdoc/>
        public async Task<PullRequestInfo> GetPullRequestInfoAsync(
            string organization,
            string project,
            string repository,
            int pullRequestId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching PR {PullRequestId} from {Organization}/{Project}/{Repository}",
                pullRequestId, organization, project, repository);

            ValidateAuthentication();

            var baseUrl = string.Format(AzureDevOpsToolConstants.BaseUrlTemplate, organization, project);
            var endpoint = string.Format(AzureDevOpsToolConstants.GetPullRequestEndpoint, repository, pullRequestId);
            var url = $"{baseUrl}{endpoint}?api-version={AzureDevOpsToolConstants.ApiVersion}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var prResponse = JsonSerializer.Deserialize<AzurePullRequestResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (prResponse == null)
                throw new InvalidOperationException("Failed to deserialize pull request response");

            return new PullRequestInfo
            {
                PullRequestId = prResponse.pullRequestId,
                Title = prResponse.title,
                Description = prResponse.description ?? string.Empty,
                Author = prResponse.createdBy?.displayName ?? "Unknown",
                AuthorEmail = prResponse.createdBy?.uniqueName ?? string.Empty,
                Status = prResponse.status,
                CreatedDate = prResponse.creationDate,
                SourceBranch = prResponse.sourceRefName ?? string.Empty,
                TargetBranch = prResponse.targetRefName ?? string.Empty,
                RepositoryName = repository,
                ProjectName = project,
                Organization = organization,
                Reviewers = prResponse.reviewers?.Select(r => r.displayName).ToList() ?? new List<string>()
            };
        }

        /// <inheritdoc/>
        public async Task<List<FileChange>> GetPullRequestChangesAsync(
            string organization,
            string project,
            string repository,
            int pullRequestId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching changes for PR {PullRequestId}", pullRequestId);

            ValidateAuthentication();

            // First, get the latest iteration
            var baseUrl = string.Format(AzureDevOpsToolConstants.BaseUrlTemplate, organization, project);
            var iterationsEndpoint = string.Format(AzureDevOpsToolConstants.GetPullRequestIterationsEndpoint, repository, pullRequestId);
            var iterationsUrl = $"{baseUrl}{iterationsEndpoint}?api-version={AzureDevOpsToolConstants.ApiVersion}";

            var iterationsResponse = await _httpClient.GetAsync(iterationsUrl, cancellationToken);
            iterationsResponse.EnsureSuccessStatusCode();

            var iterationsContent = await iterationsResponse.Content.ReadAsStringAsync(cancellationToken);
            var iterations = JsonSerializer.Deserialize<AzureIterationsResponse>(iterationsContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (iterations == null || iterations.value.Count == 0)
                throw new InvalidOperationException("No iterations found for pull request");

            // Get the latest iteration
            var latestIteration = iterations.value.OrderByDescending(i => i.id).First();
            var sourceCommitId = latestIteration.sourceRefCommit?.commitId ?? string.Empty;
            var targetCommitId = latestIteration.targetRefCommit?.commitId ?? string.Empty;

            // Get changes for the iteration
            var changesEndpoint = string.Format(AzureDevOpsToolConstants.GetPullRequestChangesEndpoint,
                repository, pullRequestId, latestIteration.id);
            var changesUrl = $"{baseUrl}{changesEndpoint}?api-version={AzureDevOpsToolConstants.ApiVersion}";

            var changesResponse = await _httpClient.GetAsync(changesUrl, cancellationToken);
            changesResponse.EnsureSuccessStatusCode();

            var changesContent = await changesResponse.Content.ReadAsStringAsync(cancellationToken);
            var changes = JsonSerializer.Deserialize<AzureChangesResponse>(changesContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (changes == null)
                return new List<FileChange>();

            var fileChanges = new List<FileChange>();

            foreach (var change in changes.changeEntries.Take(_maxFiles))
            {
                // Skip folders
                if (change.item?.isFolder == true)
                    continue;

                var changeType = MapChangeType(change.changeType);
                var filePath = change.item?.path ?? string.Empty;

                // Skip if path is empty
                if (string.IsNullOrWhiteSpace(filePath))
                    continue;

                var fileChange = new FileChange
                {
                    FilePath = filePath,
                    ChangeType = changeType,
                    ModifiedCommitId = sourceCommitId,
                    OriginalCommitId = targetCommitId
                };

                // Fetch file contents
                try
                {
                    if (changeType != ChangeType.Added)
                    {
                        fileChange.OriginalContent = await GetFileContentAsync(
                            organization, project, repository, filePath, targetCommitId, cancellationToken);
                        _logger.LogDebug("Fetched original content for {FilePath}: {Length} chars", 
                            filePath, fileChange.OriginalContent?.Length ?? 0);
                    }

                    if (changeType != ChangeType.Deleted)
                    {
                        fileChange.ModifiedContent = await GetFileContentAsync(
                            organization, project, repository, filePath, sourceCommitId, cancellationToken);
                        _logger.LogDebug("Fetched modified content for {FilePath}: {Length} chars", 
                            filePath, fileChange.ModifiedContent?.Length ?? 0);
                    }

                    // Calculate additions/deletions
                    CalculateLineChanges(fileChange);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch content for file {FilePath}. Error: {Error}", 
                        filePath, ex.Message);
                    // Continue with other files but mark content as unavailable
                    fileChange.OriginalContent = null;
                    fileChange.ModifiedContent = null;
                }

                fileChanges.Add(fileChange);
            }

            _logger.LogInformation("Retrieved {Count} file changes", fileChanges.Count);
            return fileChanges;
        }

        /// <inheritdoc/>
        public async Task<string> GetFileContentAsync(
            string organization,
            string project,
            string repository,
            string path,
            string commitId,
            CancellationToken cancellationToken = default)
        {
            ValidateAuthentication();

            var baseUrl = string.Format(AzureDevOpsToolConstants.BaseUrlTemplate, organization, project);
            var endpoint = string.Format(AzureDevOpsToolConstants.GetFileContentEndpoint, repository);
            var url = $"{baseUrl}{endpoint}?path={Uri.EscapeDataString(path)}&versionDescriptor.versionType=commit&versionDescriptor.version={commitId}&includeContent=true&api-version={AzureDevOpsToolConstants.ApiVersion}";

            _logger.LogDebug("Fetching file content from: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File not found: {Path} at commit {CommitId}", path, commitId);
                return string.Empty;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to fetch file content. Status: {Status}, Response: {Response}", 
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to fetch file content: {response.StatusCode} - {errorContent}");
            }

            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // Parse JSON response to extract content field
            try
            {
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                if (jsonDoc.RootElement.TryGetProperty("content", out var contentElement))
                {
                    var content = contentElement.GetString() ?? string.Empty;
                    _logger.LogDebug("Retrieved {Length} characters for {Path}", content.Length, path);
                    return content;
                }
                else
                {
                    _logger.LogWarning("No 'content' field in response for {Path}. Response: {Response}", path, responseContent.Substring(0, Math.Min(200, responseContent.Length)));
                    return string.Empty;
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON response for {Path}. Response: {Response}", path, responseContent.Substring(0, Math.Min(200, responseContent.Length)));
                return string.Empty;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                // Try to access a simple endpoint
                var url = "https://dev.azure.com/_apis/projects?api-version=7.1&$top=1";
                var response = await _httpClient.GetAsync(url, cancellationToken);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed");
                return false;
            }
        }

        private void ValidateAuthentication()
        {
            if (string.IsNullOrWhiteSpace(_personalAccessToken))
            {
                throw new UnauthorizedAccessException(
                    $"Azure DevOps Personal Access Token not found. Set the {AzureDevOpsToolConstants.EnvAzureDevOpsPat} environment variable.");
            }
        }

        private static ChangeType MapChangeType(string changeType)
        {
            return changeType.ToLowerInvariant() switch
            {
                "add" => ChangeType.Added,
                "edit" => ChangeType.Modified,
                "delete" => ChangeType.Deleted,
                "rename" => ChangeType.Renamed,
                _ => ChangeType.Modified
            };
        }

        private static void CalculateLineChanges(FileChange fileChange)
        {
            if (fileChange.ChangeType == ChangeType.Added)
            {
                fileChange.Additions = fileChange.ModifiedContent?.Split('\n').Length ?? 0;
                fileChange.Deletions = 0;
            }
            else if (fileChange.ChangeType == ChangeType.Deleted)
            {
                fileChange.Additions = 0;
                fileChange.Deletions = fileChange.OriginalContent?.Split('\n').Length ?? 0;
            }
            else
            {
                var oldLines = fileChange.OriginalContent?.Split('\n') ?? Array.Empty<string>();
                var newLines = fileChange.ModifiedContent?.Split('\n') ?? Array.Empty<string>();

                // Simple line counting (will be refined by DiffService)
                var oldCount = oldLines.Length;
                var newCount = newLines.Length;

                if (newCount > oldCount)
                {
                    fileChange.Additions = newCount - oldCount;
                    fileChange.Deletions = 0;
                }
                else
                {
                    fileChange.Additions = 0;
                    fileChange.Deletions = oldCount - newCount;
                }
            }
        }

        private static int GetIntFromEnv(string name, int defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        #region Internal API Response Models

        private class AzurePullRequestResponse
        {
            public int pullRequestId { get; set; }
            public string title { get; set; } = string.Empty;
            public string? description { get; set; }
            public string status { get; set; } = string.Empty;
            public DateTime creationDate { get; set; }
            public AzureIdentity? createdBy { get; set; }
            public string? sourceRefName { get; set; }
            public string? targetRefName { get; set; }
            public List<AzureIdentity>? reviewers { get; set; }
        }

        private class AzureIdentity
        {
            public string displayName { get; set; } = string.Empty;
            public string uniqueName { get; set; } = string.Empty;
        }

        private class AzureIterationsResponse
        {
            public List<AzureIteration> value { get; set; } = new();
        }

        private class AzureIteration
        {
            public int id { get; set; }
            public AzureCommit? sourceRefCommit { get; set; }
            public AzureCommit? targetRefCommit { get; set; }
        }

        private class AzureCommit
        {
            public string commitId { get; set; } = string.Empty;
        }

        private class AzureChangesResponse
        {
            public List<AzureChange> changeEntries { get; set; } = new();
        }

        private class AzureChange
        {
            public string changeType { get; set; } = string.Empty;
            public AzureItem? item { get; set; }
        }

        private class AzureItem
        {
            public string path { get; set; } = string.Empty;
            public bool isFolder { get; set; }
        }

        #endregion
    }
}

