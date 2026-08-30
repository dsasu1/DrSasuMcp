using DrSasuMcp.AzureDevOps.AzureDevOps.Analyzers;
using DrSasuMcp.AzureDevOps.AzureDevOps.Models;
using DrSasuMcp.AzureDevOps.AzureDevOps.Utils;
using DrSasuMcp.Common.Models;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;

namespace DrSasuMcp.AzureDevOps.AzureDevOps
{
    /// <summary>
    /// MCP tool for reviewing Azure DevOps Pull Requests and reading work items.
    /// </summary>
    [McpServerToolType]
    public partial class AzureDevOpsTool
    {
        private const int DefaultQueryTop = 50;

        private readonly IAzureDevOpsService _azureDevOpsService;
        private readonly IDiffService _diffService;
        private readonly IEnumerable<ICodeAnalyzer> _analyzers;
        private readonly ILogger<AzureDevOpsTool> _logger;

        public AzureDevOpsTool(
            IAzureDevOpsService azureDevOpsService,
            IDiffService diffService,
            IEnumerable<ICodeAnalyzer> analyzers,
            ILogger<AzureDevOpsTool> logger)
        {
            _azureDevOpsService = azureDevOpsService;
            _diffService = diffService;
            _analyzers = analyzers;
            _logger = logger;
        }

        /// <summary>
        /// Reviews an Azure DevOps Pull Request and provides code analysis with security, quality, and best practice insights.
        /// </summary>
        [McpServerTool(
            Title = "Azure: Review Azure DevOps Pull Request",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Review an Azure DevOps Pull Request and provide comprehensive code analysis with security, quality, and best practice insights")]
        public async Task<OperationResult> AzureReviewPullRequest(
            [Description("Full Azure DevOps PR URL")] string prUrl,
            [Description("Comma-separated analyzers: security,quality,bestpractices (default: all)")]
            string? includeAnalyzers = null,
            [Description("Minimum issue level: info,warning,critical (default: info)")]
            string minIssueLevel = "info",
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting PR review for {PrUrl}", prUrl);

                // Parse URL
                var parsed = PrUrlParser.ParsePrUrl(prUrl);
                if (parsed == null)
                {
                    return new OperationResult(
                        success: false,
                        error: "Invalid Azure DevOps PR URL format. Expected: https://dev.azure.com/{org}/{project}/_git/{repo}/pullrequest/{id}"
                    );
                }

                var (org, project, repo, prId) = parsed.Value;

                // Parse minimum issue level
                var minLevel = ParseIssueLevel(minIssueLevel);

                // Select analyzers
                var selectedAnalyzers = SelectAnalyzers(includeAnalyzers);
                if (!selectedAnalyzers.Any())
                {
                    return new OperationResult(
                        success: false,
                        error: "No valid analyzers selected. Available: security, quality, bestpractices"
                    );
                }

                // Fetch PR information
                var prInfo = await _azureDevOpsService.GetPullRequestInfoAsync(org, project, repo, prId, cancellationToken);

                // Fetch file changes
                var fileChanges = await _azureDevOpsService.GetPullRequestChangesAsync(org, project, repo, prId, cancellationToken);

                if (!fileChanges.Any())
                {
                    return new OperationResult(
                        success: true,
                        data: new ReviewSummary
                        {
                            PullRequestInfo = prInfo,
                            FilesChanged = 0,
                            OverallAssessment = "No file changes to review",
                            ReviewedAt = DateTime.UtcNow,
                            ReviewTimeMs = (int)stopwatch.ElapsedMilliseconds
                        }
                    );
                }

                // Analyze each file in parallel (content is already fetched)
                var fileReviewTasks = fileChanges.Select(async fileChange =>
                {
                    var comments = new List<ReviewComment>();

                    foreach (var analyzer in selectedAnalyzers)
                    {
                        if (analyzer.SupportsFileType(fileChange.FilePath))
                        {
                            var analyzerComments = await analyzer.AnalyzeFileChangeAsync(fileChange, cancellationToken);
                            comments.AddRange(analyzerComments);
                        }
                    }

                    // Filter by minimum issue level
                    comments = comments.Where(c => c.Level >= minLevel).ToList();

                    return new FileReview
                    {
                        FilePath = fileChange.FilePath,
                        ChangeType = fileChange.ChangeType,
                        Additions = fileChange.Additions,
                        Deletions = fileChange.Deletions,
                        Comments = comments
                    };
                });

                var fileReviews = (await Task.WhenAll(fileReviewTasks)).ToList();

                // Calculate summary statistics
                var totalAdditions = fileReviews.Sum(fr => fr.Additions);
                var totalDeletions = fileReviews.Sum(fr => fr.Deletions);
                var allComments = fileReviews.SelectMany(fr => fr.Comments).ToList();
                var criticalIssues = allComments.Count(c => c.Level == IssueLevel.Critical);
                var warnings = allComments.Count(c => c.Level == IssueLevel.Warning);
                var suggestions = allComments.Count(c => c.Level == IssueLevel.Info);

                // Determine overall assessment
                var overallAssessment = criticalIssues > 0 ? "Requires changes before merge" :
                                      warnings > 5 ? "Review recommended before merge" :
                                      warnings > 0 ? "Minor issues found" :
                                      "Looks good to merge";

                stopwatch.Stop();

                var summary = new ReviewSummary
                {
                    PullRequestInfo = prInfo,
                    FilesChanged = fileChanges.Count,
                    TotalAdditions = totalAdditions,
                    TotalDeletions = totalDeletions,
                    CriticalIssues = criticalIssues,
                    Warnings = warnings,
                    Suggestions = suggestions,
                    FileReviews = fileReviews,
                    OverallAssessment = overallAssessment,
                    ReviewedAt = DateTime.UtcNow,
                    ReviewTimeMs = (int)stopwatch.ElapsedMilliseconds
                };

                _logger.LogInformation(
                    "PR review completed in {ElapsedMs}ms. Critical: {Critical}, Warnings: {Warnings}, Suggestions: {Suggestions}",
                    stopwatch.ElapsedMilliseconds, criticalIssues, warnings, suggestions);

                return new OperationResult(success: true, data: summary);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Authentication failed");
                return new OperationResult(
                    success: false,
                    error: $"Authentication failed: {ex.Message}"
                );
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed");
                return new OperationResult(
                    success: false,
                    error: $"Failed to connect to Azure DevOps: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during PR review");
                return new OperationResult(
                    success: false,
                    error: $"Unexpected error: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Gets detailed diff for a Pull Request or specific file, showing line-by-line changes.
        /// </summary>
        [McpServerTool(
            Title = "Azure: Get Pull Request Diff",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Get detailed diff for a Pull Request or specific file, showing line-by-line changes in unified, side-by-side, or inline format")]
        public async Task<OperationResult> AzureGetPullRequestDiff(
            [Description("Full Azure DevOps PR URL")] string prUrl,
            [Description("Optional: specific file path to get diff for")] string? filePath = null,
            [Description("Diff format: unified, sidebyside, inline (default: unified)")]
            string diffFormat = "unified",
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating diff for {PrUrl}, file: {FilePath}", prUrl, filePath ?? "all");

                // Parse URL
                var parsed = PrUrlParser.ParsePrUrl(prUrl);
                if (parsed == null)
                {
                    return new OperationResult(
                        success: false,
                        error: "Invalid Azure DevOps PR URL format"
                    );
                }

                var (org, project, repo, prId) = parsed.Value;

                // Fetch file changes
                var fileChanges = await _azureDevOpsService.GetPullRequestChangesAsync(org, project, repo, prId, cancellationToken);

                _logger.LogInformation("Fetched {Count} file changes from PR", fileChanges.Count);

                // Filter by file path if specified
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    fileChanges = fileChanges.Where(fc =>
                        fc.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase) ||
                        fc.FilePath.EndsWith(filePath, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (!fileChanges.Any())
                    {
                        return new OperationResult(
                            success: false,
                            error: $"File not found in PR: {filePath}"
                        );
                    }
                }

                // Generate diffs
                var diffs = new List<DiffResultModel>();
                foreach (var fileChange in fileChanges)
                {
                    if (string.IsNullOrEmpty(fileChange.OriginalContent) && string.IsNullOrEmpty(fileChange.ModifiedContent))
                    {
                        _logger.LogWarning("Both original and modified content are empty for {FilePath}", fileChange.FilePath);
                    }

                    _logger.LogDebug("Generating {Format} diff for {FilePath} (Original: {OldLength} chars, Modified: {NewLength} chars)",
                        diffFormat, fileChange.FilePath,
                        fileChange.OriginalContent?.Length ?? 0,
                        fileChange.ModifiedContent?.Length ?? 0);

                    var diff = diffFormat.ToLowerInvariant() switch
                    {
                        "sidebyside" => _diffService.GenerateSideBySideDiff(
                            fileChange.FilePath, fileChange.OriginalContent, fileChange.ModifiedContent),
                        "inline" => _diffService.GenerateInlineDiff(
                            fileChange.FilePath, fileChange.OriginalContent, fileChange.ModifiedContent),
                        _ => _diffService.GenerateUnifiedDiff(
                            fileChange.FilePath, fileChange.OriginalContent, fileChange.ModifiedContent)
                    };

                    diffs.Add(diff);
                }

                return new OperationResult(success: true, data: diffs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating diff");
                return new OperationResult(
                    success: false,
                    error: $"Failed to generate diff: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Gets Pull Request metadata including title, author, status, and file count without performing analysis.
        /// </summary>
        [McpServerTool(
            Title = "Azure: Get Pull Request Info",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Get Pull Request metadata including title, author, status, and file count without performing analysis")]
        public async Task<OperationResult> AzureGetPullRequestInfo(
            [Description("Full Azure DevOps PR URL")] string prUrl,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching PR info for {PrUrl}", prUrl);

                // Parse URL
                var parsed = PrUrlParser.ParsePrUrl(prUrl);
                if (parsed == null)
                {
                    return new OperationResult(
                        success: false,
                        error: "Invalid Azure DevOps PR URL format"
                    );
                }

                var (org, project, repo, prId) = parsed.Value;

                // Fetch PR information and file count in parallel
                var prInfoTask = _azureDevOpsService.GetPullRequestInfoAsync(org, project, repo, prId, cancellationToken);
                var fileCountTask = _azureDevOpsService.GetPullRequestChangesCountAsync(org, project, repo, prId, cancellationToken);

                await Task.WhenAll(prInfoTask, fileCountTask);

                var prInfo = await prInfoTask;
                prInfo.TotalFiles = await fileCountTask;

                return new OperationResult(success: true, data: prInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching PR info");
                return new OperationResult(
                    success: false,
                    error: $"Failed to fetch PR info: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Tests the connection to Azure DevOps using the configured Personal Access Token.
        /// </summary>
        [McpServerTool(
            Title = "Azure: Test Azure DevOps Connection",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Test the connection to Azure DevOps using the configured Personal Access Token and verify authentication")]
        public async Task<OperationResult> AzureTestConnection(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Testing Azure DevOps connection");

                var isConnected = await _azureDevOpsService.TestConnectionAsync(cancellationToken);

                if (isConnected)
                {
                    return new OperationResult(
                        success: true,
                        data: new { connected = true, message = "Successfully connected to Azure DevOps" }
                    );
                }
                else
                {
                    return new OperationResult(
                        success: false,
                        error: "Failed to connect to Azure DevOps. Check your PAT token and network connection."
                    );
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return new OperationResult(
                    success: false,
                    error: $"Authentication failed: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed");
                return new OperationResult(
                    success: false,
                    error: $"Connection test failed: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Gets work item details by work item ID or URL.
        /// </summary>
        [McpServerTool(
            Title = "Azure: Get Work Item",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Get Azure DevOps work item details by ID or URL, including type, title, state, assignee, area and iteration paths, tags, dates, estimates, and description")]
        public async Task<OperationResult> AzureGetWorkItem(
            [Description("Work item ID (e.g. 1234) or full work item URL (e.g. https://dev.azure.com/{org}/{project}/_workitems/edit/1234)")]
            string workItem,
            [Description("Optional: project name. Required when passing a bare ID unless the work item is resolvable at organization scope")]
            string? project = null,
            [Description("Optional: organization name. Defaults to the AZURE_DEVOPS_ORG environment variable")]
            string? organization = null,
            [Description("Include work item links such as parent, child, related, and attached pull requests (default: false)")]
            bool includeRelations = false,
            [Description("Optional: comma-separated field reference names to include beyond the standard set, e.g. Custom.Team,System.BoardColumn")]
            string? additionalFields = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching work item {WorkItem}", workItem);

                var reference = WorkItemUrlParser.ParseWorkItemReference(workItem);
                if (reference == null)
                {
                    return new OperationResult(
                        success: false,
                        error: "Invalid work item reference. Provide a numeric work item ID or a URL such as " +
                               "https://dev.azure.com/{org}/{project}/_workitems/edit/1234"
                    );
                }

                var (org, resolvedProject, workItemId) = reference.Value;

                // Values from the URL win, then explicit arguments, then the configured default organization
                var effectiveOrganization = org ?? organization ?? _azureDevOpsService.GetDefaultOrganization();
                if (string.IsNullOrWhiteSpace(effectiveOrganization))
                {
                    return new OperationResult(
                        success: false,
                        error: $"Organization is required. Pass a work item URL, set the organization parameter, or set the {AzureDevOpsToolConstants.EnvAzureDevOpsOrg} environment variable."
                    );
                }

                var effectiveProject = resolvedProject ?? project;

                var result = await _azureDevOpsService.GetWorkItemAsync(
                    effectiveOrganization,
                    effectiveProject,
                    workItemId,
                    includeRelations,
                    ParseFieldList(additionalFields),
                    cancellationToken);

                return new OperationResult(success: true, data: result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Authentication failed while fetching work item");
                return new OperationResult(
                    success: false,
                    error: $"Authentication failed: {ex.Message}"
                );
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching work item");
                return new OperationResult(
                    success: false,
                    error: $"Failed to fetch work item: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching work item");
                return new OperationResult(
                    success: false,
                    error: $"Failed to fetch work item: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Queries work items using WIQL and returns the matching work item details.
        /// </summary>
        [McpServerTool(
            Title = "Azure: Query Work Items",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Query Azure DevOps work items with a WIQL query (e.g. all active bugs in a project) and return details for the matching work items")]
        public async Task<OperationResult> AzureQueryWorkItems(
            [Description("WIQL query, e.g. SELECT [System.Id] FROM WorkItems WHERE [System.WorkItemType] = 'Bug' AND [System.State] = 'Active' ORDER BY [System.ChangedDate] DESC")]
            string wiql,
            [Description("Project name to scope the query to. Required for queries that reference project-scoped values such as @project")]
            string? project = null,
            [Description("Optional: organization name. Defaults to the AZURE_DEVOPS_ORG environment variable")]
            string? organization = null,
            [Description("Maximum number of work items to return (default: 50, capped by AZURE_DEVOPS_MAX_WORK_ITEMS)")]
            int top = DefaultQueryTop,
            [Description("Optional: comma-separated field reference names to include beyond the standard set")]
            string? additionalFields = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(wiql))
                {
                    return new OperationResult(
                        success: false,
                        error: "A WIQL query is required, e.g. SELECT [System.Id] FROM WorkItems WHERE [System.State] = 'Active'"
                    );
                }

                if (!wiql.TrimStart().StartsWith("select", StringComparison.OrdinalIgnoreCase))
                {
                    return new OperationResult(
                        success: false,
                        error: "Only WIQL SELECT queries are supported. Example: SELECT [System.Id] FROM WorkItems WHERE [System.WorkItemType] = 'Bug'"
                    );
                }

                var effectiveOrganization = organization ?? _azureDevOpsService.GetDefaultOrganization();
                if (string.IsNullOrWhiteSpace(effectiveOrganization))
                {
                    return new OperationResult(
                        success: false,
                        error: $"Organization is required. Set the organization parameter or the {AzureDevOpsToolConstants.EnvAzureDevOpsOrg} environment variable."
                    );
                }

                if (top <= 0)
                {
                    top = DefaultQueryTop;
                }

                _logger.LogInformation("Querying work items in {Organization}/{Project}", effectiveOrganization, project ?? "(organization scope)");

                var result = await _azureDevOpsService.QueryWorkItemsAsync(
                    effectiveOrganization,
                    project,
                    wiql,
                    top,
                    ParseFieldList(additionalFields),
                    cancellationToken);

                return new OperationResult(success: true, rowsAffected: result.WorkItems.Count, data: result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Authentication failed while querying work items");
                return new OperationResult(
                    success: false,
                    error: $"Authentication failed: {ex.Message}"
                );
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while querying work items");
                return new OperationResult(
                    success: false,
                    error: $"Failed to query work items: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying work items");
                return new OperationResult(
                    success: false,
                    error: $"Failed to query work items: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Gets the work items linked to a pull request.
        /// </summary>
        [McpServerTool(
            Title = "Azure: Get Pull Request Work Items",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Get the work items linked to an Azure DevOps pull request, including their types, titles, states, and assignees")]
        public async Task<OperationResult> AzureGetPullRequestWorkItems(
            [Description("Full Azure DevOps PR URL")] string prUrl,
            [Description("Optional: comma-separated field reference names to include beyond the standard set")]
            string? additionalFields = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching work items linked to {PrUrl}", prUrl);

                var parsed = PrUrlParser.ParsePrUrl(prUrl);
                if (parsed == null)
                {
                    return new OperationResult(
                        success: false,
                        error: "Invalid Azure DevOps PR URL format. Expected: https://dev.azure.com/{org}/{project}/_git/{repo}/pullrequest/{id}"
                    );
                }

                var (org, project, repo, prId) = parsed.Value;

                var workItemIds = await _azureDevOpsService.GetPullRequestWorkItemIdsAsync(
                    org, project, repo, prId, cancellationToken);

                var workItems = await _azureDevOpsService.GetWorkItemsAsync(
                    org, project, workItemIds, ParseFieldList(additionalFields), cancellationToken);

                var result = new PullRequestWorkItems
                {
                    PullRequestId = prId,
                    Organization = org,
                    ProjectName = project,
                    RepositoryName = repo,
                    LinkedCount = workItemIds.Count,
                    WorkItems = workItems
                };

                return new OperationResult(success: true, rowsAffected: workItems.Count, data: result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Authentication failed while fetching pull request work items");
                return new OperationResult(
                    success: false,
                    error: $"Authentication failed: {ex.Message}"
                );
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching pull request work items");
                return new OperationResult(
                    success: false,
                    error: $"Failed to fetch pull request work items: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pull request work items");
                return new OperationResult(
                    success: false,
                    error: $"Failed to fetch pull request work items: {ex.Message}"
                );
            }
        }

        private IssueLevel ParseIssueLevel(string level)
        {
            return level.ToLowerInvariant() switch
            {
                "critical" => IssueLevel.Critical,
                "warning" => IssueLevel.Warning,
                "info" => IssueLevel.Info,
                _ => IssueLevel.Info
            };
        }

        private List<ICodeAnalyzer> SelectAnalyzers(string? includeAnalyzers)
        {
            if (string.IsNullOrWhiteSpace(includeAnalyzers))
            {
                // Return all analyzers
                return _analyzers.ToList();
            }

            var requestedAnalyzers = includeAnalyzers
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim().ToLowerInvariant())
                .ToHashSet();

            return _analyzers
                .Where(a => requestedAnalyzers.Contains(a.AnalyzerName.ToLowerInvariant()))
                .ToList();
        }

        private static List<string>? ParseFieldList(string? fields)
        {
            if (string.IsNullOrWhiteSpace(fields))
                return null;

            var parsed = fields
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return parsed.Count == 0 ? null : parsed;
        }
    }
}
