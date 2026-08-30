using DrSasuMcp.AzureDevOps.AzureDevOps.Models;
using DrSasuMcp.AzureDevOps.AzureDevOps.Utils;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DrSasuMcp.AzureDevOps.AzureDevOps
{
    /// <summary>
    /// Implementation of Azure DevOps REST API client.
    /// </summary>
    public class AzureDevOpsService : IAzureDevOpsService
    {
        private readonly HttpClient _httpClient;
        private readonly IDiffService _diffService;
        private readonly ILogger<AzureDevOpsService> _logger;
        private readonly string? _personalAccessToken;
        private readonly string? _defaultOrganization;
        private readonly int _maxFiles;
        private readonly int _maxFileSizeBytes;
        private readonly int _maxWorkItems;

        // Reuse a single options instance instead of allocating one per call
        private static readonly JsonSerializerOptions JsonOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public AzureDevOpsService(
            IHttpClientFactory httpClientFactory,
            IDiffService diffService,
            ILogger<AzureDevOpsService> logger)
        {
            _logger = logger;
            _diffService = diffService;
            _httpClient = httpClientFactory.CreateClient(nameof(AzureDevOpsService));

            // Get configuration from environment variables
            _personalAccessToken = Environment.GetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsPat);
            _defaultOrganization = Environment.GetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsOrg);
            _maxFiles = GetIntFromEnv(AzureDevOpsToolConstants.EnvAzureDevOpsMaxFiles, AzureDevOpsToolConstants.DefaultMaxFiles);
            _maxFileSizeBytes = GetIntFromEnv(AzureDevOpsToolConstants.EnvAzureDevOpsMaxFileSize, AzureDevOpsToolConstants.DefaultMaxFileSizeBytes);
            // A configured limit below one would leave every query unable to return results
            _maxWorkItems = Math.Max(1, GetIntFromEnv(AzureDevOpsToolConstants.EnvAzureDevOpsMaxWorkItems, AzureDevOpsToolConstants.DefaultMaxWorkItems));

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
            var prResponse = JsonSerializer.Deserialize<AzurePullRequestResponse>(content, JsonOptions);

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

            var (sourceCommitId, targetCommitId, rawChanges) =
                await FetchIterationChangesAsync(organization, project, repository, pullRequestId, cancellationToken);

            var fileChanges = new List<FileChange>();

            // Filter folders first, then apply the file limit so we always get up to _maxFiles actual files
            foreach (var change in rawChanges.Where(c => c.item?.isFolder != true).Take(_maxFiles))
            {
                var changeType = MapChangeType(change.changeType);
                var filePath = change.item?.path ?? string.Empty;

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

                    // Use DiffService for accurate addition/deletion counts
                    CalculateLineChanges(fileChange);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch content for file {FilePath}. Error: {Error}",
                        filePath, ex.Message);
                    fileChange.OriginalContent = null;
                    fileChange.ModifiedContent = null;
                }

                fileChanges.Add(fileChange);
            }

            _logger.LogInformation("Retrieved {Count} file changes", fileChanges.Count);
            return fileChanges;
        }

        /// <inheritdoc/>
        public async Task<int> GetPullRequestChangesCountAsync(
            string organization,
            string project,
            string repository,
            int pullRequestId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching change count for PR {PullRequestId}", pullRequestId);

            ValidateAuthentication();

            var (_, _, rawChanges) =
                await FetchIterationChangesAsync(organization, project, repository, pullRequestId, cancellationToken);

            // Count only non-folder entries; no _maxFiles limit since we want the true total
            return rawChanges.Count(c => c.item?.isFolder != true && !string.IsNullOrWhiteSpace(c.item?.path));
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

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            // Guard against very large files
            if (Encoding.UTF8.GetByteCount(responseContent) > _maxFileSizeBytes)
            {
                _logger.LogWarning("File {Path} exceeds max size limit ({MaxBytes} bytes), skipping content", path, _maxFileSizeBytes);
                return string.Empty;
            }

            // Parse JSON response to extract content field
            try
            {
                using var jsonDoc = JsonDocument.Parse(responseContent);
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
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON response for {Path}. Response: {Response}", path, responseContent.Substring(0, Math.Min(200, responseContent.Length)));
                return string.Empty;
            }
        }

        /// <inheritdoc/>
        public async Task<List<int>> GetPullRequestWorkItemIdsAsync(
            string organization,
            string project,
            string repository,
            int pullRequestId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching work items linked to PR {PullRequestId}", pullRequestId);

            ValidateAuthentication();

            var baseUrl = string.Format(AzureDevOpsToolConstants.BaseUrlTemplate, organization, project);
            var endpoint = string.Format(AzureDevOpsToolConstants.GetPullRequestWorkItemsEndpoint, repository, pullRequestId);
            var url = $"{baseUrl}{endpoint}?api-version={AzureDevOpsToolConstants.ApiVersion}";

            var content = await GetStringAsync(url, $"work items linked to pull request {pullRequestId}", cancellationToken);
            var response = JsonSerializer.Deserialize<AzureResourceRefsResponse>(content, JsonOptions);

            var ids = new List<int>();
            foreach (var reference in response?.value ?? new List<AzureResourceRef>())
            {
                // The pull request work items endpoint returns IDs as strings
                if (int.TryParse(reference.id, out var id) && !ids.Contains(id))
                    ids.Add(id);
            }

            _logger.LogInformation("PR {PullRequestId} has {Count} linked work items", pullRequestId, ids.Count);
            return ids;
        }

        /// <inheritdoc/>
        public async Task<WorkItemInfo> GetWorkItemAsync(
            string organization,
            string? project,
            int workItemId,
            bool includeRelations = false,
            IEnumerable<string>? additionalFields = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching work item {WorkItemId} from {Organization}/{Project}",
                workItemId, organization, string.IsNullOrWhiteSpace(project) ? "(organization scope)" : project);

            ValidateAuthentication();

            var baseUrl = BuildBaseUrl(organization, project);
            var endpoint = string.Format(AzureDevOpsToolConstants.GetWorkItemEndpoint, workItemId);
            var expand = includeRelations ? "all" : "fields";
            var url = $"{baseUrl}{endpoint}?$expand={expand}&api-version={AzureDevOpsToolConstants.ApiVersion}";

            var content = await GetStringAsync(url, $"work item {workItemId}", cancellationToken);
            var workItem = JsonSerializer.Deserialize<AzureWorkItemResponse>(content, JsonOptions);

            if (workItem == null)
                throw new InvalidOperationException("Failed to deserialize work item response");

            return MapWorkItem(workItem, organization, project, additionalFields);
        }

        /// <inheritdoc/>
        public async Task<List<WorkItemInfo>> GetWorkItemsAsync(
            string organization,
            string? project,
            IEnumerable<int> workItemIds,
            IEnumerable<string>? additionalFields = null,
            CancellationToken cancellationToken = default)
        {
            var ids = workItemIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0)
                return new List<WorkItemInfo>();

            ValidateAuthentication();

            var baseUrl = BuildBaseUrl(organization, project);
            var workItemsById = new Dictionary<int, WorkItemInfo>();

            for (var offset = 0; offset < ids.Count; offset += AzureDevOpsToolConstants.WorkItemBatchSize)
            {
                var batchIds = ids.Skip(offset).Take(AzureDevOpsToolConstants.WorkItemBatchSize).ToList();

                // errorPolicy=omit keeps the batch alive when individual work items are deleted or inaccessible
                var url = $"{baseUrl}{AzureDevOpsToolConstants.GetWorkItemsBatchEndpoint}" +
                          $"?ids={string.Join(',', batchIds)}&errorPolicy=omit&api-version={AzureDevOpsToolConstants.ApiVersion}";

                var content = await GetStringAsync(url, $"a batch of {batchIds.Count} work items", cancellationToken);
                var batch = JsonSerializer.Deserialize<AzureWorkItemsBatchResponse>(content, JsonOptions);

                foreach (var workItem in batch?.value ?? new List<AzureWorkItemResponse?>())
                {
                    if (workItem == null)
                        continue;

                    workItemsById[workItem.id] = MapWorkItem(workItem, organization, project, additionalFields);
                }
            }

            var missing = ids.Count - workItemsById.Count;
            if (missing > 0)
                _logger.LogWarning("{Count} of {Total} work items could not be read and were omitted", missing, ids.Count);

            // Preserve the requested order
            return ids.Where(workItemsById.ContainsKey).Select(id => workItemsById[id]).ToList();
        }

        /// <inheritdoc/>
        public async Task<WorkItemQueryResult> QueryWorkItemsAsync(
            string organization,
            string? project,
            string wiql,
            int top,
            IEnumerable<string>? additionalFields = null,
            CancellationToken cancellationToken = default)
        {
            ValidateAuthentication();

            var limit = Math.Clamp(top, 1, _maxWorkItems);

            _logger.LogInformation("Running WIQL query against {Organization}/{Project} with a limit of {Limit}",
                organization, string.IsNullOrWhiteSpace(project) ? "(organization scope)" : project, limit);

            var baseUrl = BuildBaseUrl(organization, project);

            // Ask for one extra result so truncation can be reported accurately
            var url = $"{baseUrl}{AzureDevOpsToolConstants.QueryWorkItemsEndpoint}" +
                      $"?$top={limit + 1}&api-version={AzureDevOpsToolConstants.ApiVersion}";

            var requestBody = JsonSerializer.Serialize(new { query = wiql });
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, "the WIQL query", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var queryResponse = JsonSerializer.Deserialize<AzureWiqlResponse>(content, JsonOptions);

            var matchedIds = ExtractWorkItemIds(queryResponse);
            var truncated = matchedIds.Count > limit;
            var selectedIds = matchedIds.Take(limit).ToList();

            var workItems = await GetWorkItemsAsync(organization, project, selectedIds, additionalFields, cancellationToken);

            _logger.LogInformation("WIQL query returned {Count} work items (truncated: {Truncated})",
                workItems.Count, truncated);

            return new WorkItemQueryResult
            {
                QueryType = queryResponse?.queryType ?? string.Empty,
                AsOf = queryResponse?.asOf,
                MatchedCount = selectedIds.Count,
                Truncated = truncated,
                WorkItems = workItems
            };
        }

        /// <inheritdoc/>
        public string? GetDefaultOrganization() =>
            string.IsNullOrWhiteSpace(_defaultOrganization) ? null : _defaultOrganization;

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

        /// <summary>
        /// Shared helper: fetches the latest iteration and its raw change entries for a PR.
        /// </summary>
        private async Task<(string sourceCommitId, string targetCommitId, List<AzureChange> changes)>
            FetchIterationChangesAsync(
                string organization,
                string project,
                string repository,
                int pullRequestId,
                CancellationToken cancellationToken)
        {
            var baseUrl = string.Format(AzureDevOpsToolConstants.BaseUrlTemplate, organization, project);

            // Get iterations
            var iterationsEndpoint = string.Format(AzureDevOpsToolConstants.GetPullRequestIterationsEndpoint, repository, pullRequestId);
            var iterationsUrl = $"{baseUrl}{iterationsEndpoint}?api-version={AzureDevOpsToolConstants.ApiVersion}";

            var iterationsResponse = await _httpClient.GetAsync(iterationsUrl, cancellationToken);
            iterationsResponse.EnsureSuccessStatusCode();

            var iterationsContent = await iterationsResponse.Content.ReadAsStringAsync(cancellationToken);
            var iterations = JsonSerializer.Deserialize<AzureIterationsResponse>(iterationsContent, JsonOptions);

            if (iterations == null || iterations.value.Count == 0)
                throw new InvalidOperationException("No iterations found for pull request");

            var latestIteration = iterations.value.OrderByDescending(i => i.id).First();
            var sourceCommitId = latestIteration.sourceRefCommit?.commitId ?? string.Empty;
            var targetCommitId = latestIteration.targetRefCommit?.commitId ?? string.Empty;

            // Get changes for the latest iteration
            var changesEndpoint = string.Format(AzureDevOpsToolConstants.GetPullRequestChangesEndpoint,
                repository, pullRequestId, latestIteration.id);
            var changesUrl = $"{baseUrl}{changesEndpoint}?api-version={AzureDevOpsToolConstants.ApiVersion}";

            var changesResponse = await _httpClient.GetAsync(changesUrl, cancellationToken);
            changesResponse.EnsureSuccessStatusCode();

            var changesContent = await changesResponse.Content.ReadAsStringAsync(cancellationToken);
            var changes = JsonSerializer.Deserialize<AzureChangesResponse>(changesContent, JsonOptions);

            return (sourceCommitId, targetCommitId, changes?.changeEntries ?? new List<AzureChange>());
        }

        private static string BuildBaseUrl(string organization, string? project)
        {
            return string.IsNullOrWhiteSpace(project)
                ? string.Format(AzureDevOpsToolConstants.OrganizationBaseUrlTemplate, organization)
                : string.Format(AzureDevOpsToolConstants.BaseUrlTemplate, organization, project);
        }

        /// <summary>
        /// Issues a GET request and returns the response body, translating failures into descriptive exceptions.
        /// </summary>
        private async Task<string> GetStringAsync(string url, string description, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Requesting {Description} from {Url}", description, url);

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            await EnsureSuccessAsync(response, description, cancellationToken);

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        private async Task EnsureSuccessAsync(HttpResponseMessage response, string description, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
                return;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var detail = ExtractErrorMessage(body);

            _logger.LogError("Request for {Description} failed with {Status}: {Body}",
                description, response.StatusCode, body);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException(
                    $"Azure DevOps denied access to {description} ({(int)response.StatusCode} {response.StatusCode}). " +
                    $"Verify the {AzureDevOpsToolConstants.EnvAzureDevOpsPat} token is valid and has Work Items (Read) scope. {detail}".TrimEnd());
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new HttpRequestException(
                    $"Azure DevOps could not find {description}. Check the ID, organization, and project. {detail}".TrimEnd());
            }

            throw new HttpRequestException(
                $"Azure DevOps request for {description} failed: {(int)response.StatusCode} {response.StatusCode}. {detail}".TrimEnd());
        }

        /// <summary>
        /// Pulls the human readable message out of an Azure DevOps error payload, falling back to a truncated body.
        /// </summary>
        private static string ExtractErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.ValueKind == JsonValueKind.Object &&
                    document.RootElement.TryGetProperty("message", out var message) &&
                    message.ValueKind == JsonValueKind.String)
                {
                    return message.GetString() ?? string.Empty;
                }
            }
            catch (JsonException)
            {
                // Azure DevOps returns HTML for some failures, such as an expired token
            }

            return body.Substring(0, Math.Min(300, body.Length));
        }

        /// <summary>
        /// Collects work item IDs from a WIQL response, which reports flat results and hierarchical results differently.
        /// </summary>
        private static List<int> ExtractWorkItemIds(AzureWiqlResponse? response)
        {
            var ids = new List<int>();

            if (response == null)
                return ids;

            foreach (var workItem in response.workItems ?? new List<AzureWorkItemRef>())
            {
                if (workItem.id > 0 && !ids.Contains(workItem.id))
                    ids.Add(workItem.id);
            }

            foreach (var relation in response.workItemRelations ?? new List<AzureWiqlRelation>())
            {
                foreach (var candidate in new[] { relation.source, relation.target })
                {
                    if (candidate != null && candidate.id > 0 && !ids.Contains(candidate.id))
                        ids.Add(candidate.id);
                }
            }

            return ids;
        }

        private static WorkItemInfo MapWorkItem(
            AzureWorkItemResponse response,
            string organization,
            string? project,
            IEnumerable<string>? additionalFields)
        {
            var fields = BuildFieldLookup(response.fields);

            var teamProject = GetFieldString(fields, AzureDevOpsToolConstants.WorkItemFields.TeamProject);
            var resolvedProject = !string.IsNullOrWhiteSpace(teamProject) ? teamProject : project ?? string.Empty;

            var workItem = new WorkItemInfo
            {
                Id = response.id,
                Revision = response.rev,
                WorkItemType = GetFieldString(fields, AzureDevOpsToolConstants.WorkItemFields.WorkItemType),
                Title = GetFieldString(fields, AzureDevOpsToolConstants.WorkItemFields.Title),
                State = GetFieldString(fields, AzureDevOpsToolConstants.WorkItemFields.State),
                Reason = GetFieldString(fields, AzureDevOpsToolConstants.WorkItemFields.Reason),
                AssignedTo = GetIdentityDisplayName(fields, AzureDevOpsToolConstants.WorkItemFields.AssignedTo),
                CreatedBy = GetIdentityDisplayName(fields, AzureDevOpsToolConstants.WorkItemFields.CreatedBy),
                CreatedDate = GetFieldDateTime(fields, AzureDevOpsToolConstants.WorkItemFields.CreatedDate),
                ChangedBy = GetIdentityDisplayName(fields, AzureDevOpsToolConstants.WorkItemFields.ChangedBy),
                ChangedDate = GetFieldDateTime(fields, AzureDevOpsToolConstants.WorkItemFields.ChangedDate),
                AreaPath = GetFieldString(fields, AzureDevOpsToolConstants.WorkItemFields.AreaPath),
                IterationPath = GetFieldString(fields, AzureDevOpsToolConstants.WorkItemFields.IterationPath),
                ProjectName = resolvedProject,
                Organization = organization,
                Tags = ParseTags(GetFieldString(fields, AzureDevOpsToolConstants.WorkItemFields.Tags)),
                Priority = GetFieldInt(fields, AzureDevOpsToolConstants.WorkItemFields.Priority),
                Severity = GetOptionalFieldString(fields, AzureDevOpsToolConstants.WorkItemFields.Severity),
                Description = GetFieldString(fields, AzureDevOpsToolConstants.WorkItemFields.Description),
                AcceptanceCriteria = GetOptionalFieldString(fields, AzureDevOpsToolConstants.WorkItemFields.AcceptanceCriteria),
                ReproSteps = GetOptionalFieldString(fields, AzureDevOpsToolConstants.WorkItemFields.ReproSteps),
                StoryPoints = GetFieldDouble(fields, AzureDevOpsToolConstants.WorkItemFields.StoryPoints),
                Effort = GetFieldDouble(fields, AzureDevOpsToolConstants.WorkItemFields.Effort),
                RemainingWork = GetFieldDouble(fields, AzureDevOpsToolConstants.WorkItemFields.RemainingWork),
                CompletedWork = GetFieldDouble(fields, AzureDevOpsToolConstants.WorkItemFields.CompletedWork),
                ParentId = GetFieldInt(fields, AzureDevOpsToolConstants.WorkItemFields.Parent),
                Url = ResolveWorkItemWebUrl(response, organization, resolvedProject)
            };

            foreach (var relation in response.relations ?? new List<AzureWorkItemRelationResponse>())
            {
                workItem.Relations.Add(MapWorkItemRelation(relation));
            }

            foreach (var fieldName in additionalFields ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(fieldName))
                    continue;

                var name = fieldName.Trim();
                if (fields.TryGetValue(name, out var value))
                    workItem.AdditionalFields[name] = FormatFieldValue(value);
            }

            return workItem;
        }

        private static WorkItemRelation MapWorkItemRelation(AzureWorkItemRelationResponse relation)
        {
            var name = string.Empty;
            if (relation.attributes != null &&
                relation.attributes.TryGetValue("name", out var nameElement) &&
                nameElement.ValueKind == JsonValueKind.String)
            {
                name = nameElement.GetString() ?? string.Empty;
            }

            return new WorkItemRelation
            {
                RelationType = relation.rel ?? string.Empty,
                Name = name,
                Url = relation.url ?? string.Empty,
                TargetWorkItemId = WorkItemUrlParser.ParseWorkItemIdFromApiUrl(relation.url)
            };
        }

        /// <summary>
        /// Copies the raw field payload into a case-insensitive lookup, since field reference name casing
        /// varies between Azure DevOps API versions.
        /// </summary>
        private static Dictionary<string, JsonElement> BuildFieldLookup(Dictionary<string, JsonElement>? fields)
        {
            var lookup = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

            foreach (var field in fields ?? new Dictionary<string, JsonElement>())
            {
                lookup[field.Key] = field.Value;
            }

            return lookup;
        }

        private static string ResolveWorkItemWebUrl(AzureWorkItemResponse response, string organization, string project)
        {
            if (response.links != null &&
                response.links.TryGetValue("html", out var htmlLink) &&
                !string.IsNullOrWhiteSpace(htmlLink?.href))
            {
                return htmlLink!.href!;
            }

            return string.IsNullOrWhiteSpace(project)
                ? string.Format(AzureDevOpsToolConstants.WorkItemWebUrlNoProjectTemplate, organization, response.id)
                : string.Format(AzureDevOpsToolConstants.WorkItemWebUrlTemplate, organization, project, response.id);
        }

        private static List<string> ParseTags(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
                return new List<string>();

            return tags
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        private static string GetFieldString(Dictionary<string, JsonElement> fields, string fieldName)
        {
            return GetOptionalFieldString(fields, fieldName) ?? string.Empty;
        }

        private static string? GetOptionalFieldString(Dictionary<string, JsonElement> fields, string fieldName)
        {
            if (!fields.TryGetValue(fieldName, out var element))
                return null;

            var value = FormatFieldValue(element);
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static string GetIdentityDisplayName(Dictionary<string, JsonElement> fields, string fieldName)
        {
            if (!fields.TryGetValue(fieldName, out var element))
                return string.Empty;

            return element.ValueKind switch
            {
                // Identity fields are plain strings in older API versions and objects from 5.0 onwards
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Object => GetIdentityFromObject(element),
                _ => string.Empty
            };
        }

        private static string GetIdentityFromObject(JsonElement element)
        {
            if (element.TryGetProperty("displayName", out var displayName) && displayName.ValueKind == JsonValueKind.String)
                return displayName.GetString() ?? string.Empty;

            if (element.TryGetProperty("uniqueName", out var uniqueName) && uniqueName.ValueKind == JsonValueKind.String)
                return uniqueName.GetString() ?? string.Empty;

            return string.Empty;
        }

        private static int? GetFieldInt(Dictionary<string, JsonElement> fields, string fieldName)
        {
            if (!fields.TryGetValue(fieldName, out var element))
                return null;

            return element.ValueKind switch
            {
                JsonValueKind.Number when element.TryGetInt32(out var number) => number,
                JsonValueKind.String when int.TryParse(element.GetString(), out var parsed) => parsed,
                _ => null
            };
        }

        private static double? GetFieldDouble(Dictionary<string, JsonElement> fields, string fieldName)
        {
            if (!fields.TryGetValue(fieldName, out var element))
                return null;

            return element.ValueKind switch
            {
                JsonValueKind.Number when element.TryGetDouble(out var number) => number,
                JsonValueKind.String when double.TryParse(element.GetString(), out var parsed) => parsed,
                _ => null
            };
        }

        private static DateTime? GetFieldDateTime(Dictionary<string, JsonElement> fields, string fieldName)
        {
            if (!fields.TryGetValue(fieldName, out var element) || element.ValueKind != JsonValueKind.String)
                return null;

            return element.TryGetDateTime(out var value) ? value : null;
        }

        private static string FormatFieldValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
                JsonValueKind.Object => GetIdentityFromObject(element) is { Length: > 0 } identity
                    ? identity
                    : element.GetRawText(),
                _ => element.GetRawText()
            };
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

        private void CalculateLineChanges(FileChange fileChange)
        {
            if (fileChange.ChangeType == ChangeType.Added)
            {
                var stats = _diffService.CalculateStatistics(null, fileChange.ModifiedContent);
                fileChange.Additions = stats.AddedLines + stats.UnchangedLines;
                fileChange.Deletions = 0;
            }
            else if (fileChange.ChangeType == ChangeType.Deleted)
            {
                var stats = _diffService.CalculateStatistics(fileChange.OriginalContent, null);
                fileChange.Additions = 0;
                fileChange.Deletions = stats.DeletedLines + stats.UnchangedLines;
            }
            else
            {
                var stats = _diffService.CalculateStatistics(fileChange.OriginalContent, fileChange.ModifiedContent);
                fileChange.Additions = stats.AddedLines + stats.ModifiedLines;
                fileChange.Deletions = stats.DeletedLines + stats.ModifiedLines;
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

        private class AzureResourceRefsResponse
        {
            public List<AzureResourceRef> value { get; set; } = new();
        }

        private class AzureResourceRef
        {
            public string id { get; set; } = string.Empty;
            public string? url { get; set; }
        }

        private class AzureWorkItemResponse
        {
            public int id { get; set; }
            public int rev { get; set; }
            public Dictionary<string, JsonElement>? fields { get; set; }
            public List<AzureWorkItemRelationResponse>? relations { get; set; }
            public string? url { get; set; }

            [JsonPropertyName("_links")]
            public Dictionary<string, AzureReferenceLink?>? links { get; set; }
        }

        private class AzureReferenceLink
        {
            public string? href { get; set; }
        }

        private class AzureWorkItemRelationResponse
        {
            public string? rel { get; set; }
            public string? url { get; set; }
            public Dictionary<string, JsonElement>? attributes { get; set; }
        }

        private class AzureWorkItemsBatchResponse
        {
            public int count { get; set; }

            // Entries are null for work items omitted by errorPolicy=omit
            public List<AzureWorkItemResponse?> value { get; set; } = new();
        }

        private class AzureWiqlResponse
        {
            public string? queryType { get; set; }
            public DateTime? asOf { get; set; }
            public List<AzureWorkItemRef>? workItems { get; set; }
            public List<AzureWiqlRelation>? workItemRelations { get; set; }
        }

        private class AzureWorkItemRef
        {
            public int id { get; set; }
            public string? url { get; set; }
        }

        private class AzureWiqlRelation
        {
            public string? rel { get; set; }
            public AzureWorkItemRef? source { get; set; }
            public AzureWorkItemRef? target { get; set; }
        }

        #endregion
    }
}
