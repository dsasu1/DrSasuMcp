using DrSasuMcp.Tools;
using DrSasuMcp.Tools.AzureDevOps.Analyzers;
using DrSasuMcp.Tools.AzureDevOps.Models;
using DrSasuMcp.Tools.AzureDevOps.Utils;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;

namespace DrSasuMcp.Tools.AzureDevOps
{
    /// <summary>
    /// MCP tool for reviewing Azure DevOps Pull Requests.
    /// </summary>
    [McpServerToolType]
    public partial class AzureDevOpsTool
    {
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
        /// <param name="prUrl">Full Azure DevOps PR URL (e.g., https://dev.azure.com/org/project/_git/repo/pullrequest/123)</param>
        /// <param name="includeAnalyzers">Comma-separated list of analyzers to run: security,quality,bestpractices (default: all)</param>
        /// <param name="minIssueLevel">Minimum issue level to report: info, warning, critical (default: info)</param>
        /// <returns>Comprehensive review summary with all findings</returns>
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
            string minIssueLevel = "info")
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
                var prInfo = await _azureDevOpsService.GetPullRequestInfoAsync(org, project, repo, prId);

                // Fetch file changes
                var fileChanges = await _azureDevOpsService.GetPullRequestChangesAsync(org, project, repo, prId);

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

                // Analyze each file
                var fileReviews = new List<FileReview>();
                int totalAdditions = 0;
                int totalDeletions = 0;

                foreach (var fileChange in fileChanges)
                {
                    totalAdditions += fileChange.Additions;
                    totalDeletions += fileChange.Deletions;

                    var comments = new List<ReviewComment>();

                    // Run selected analyzers
                    foreach (var analyzer in selectedAnalyzers)
                    {
                        if (analyzer.SupportsFileType(fileChange.FilePath))
                        {
                            var analyzerComments = await analyzer.AnalyzeFileChangeAsync(fileChange);
                            comments.AddRange(analyzerComments);
                        }
                    }

                    // Filter by minimum issue level
                    comments = comments.Where(c => c.Level >= minLevel).ToList();

                    fileReviews.Add(new FileReview
                    {
                        FilePath = fileChange.FilePath,
                        ChangeType = fileChange.ChangeType,
                        Additions = fileChange.Additions,
                        Deletions = fileChange.Deletions,
                        Comments = comments
                    });
                }

                // Calculate summary statistics
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
        /// <param name="prUrl">Full Azure DevOps PR URL</param>
        /// <param name="filePath">Optional: specific file path to get diff for (e.g., src/Program.cs)</param>
        /// <param name="diffFormat">Diff format: unified, sidebyside, inline (default: unified)</param>
        /// <returns>Diff results for the requested file(s)</returns>
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
            string diffFormat = "unified")
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
                var fileChanges = await _azureDevOpsService.GetPullRequestChangesAsync(org, project, repo, prId);

                _logger.LogInformation("Fetched {Count} file changes from PR", fileChanges.Count());

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
                    // Log if content is missing
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
        /// <param name="prUrl">Full Azure DevOps PR URL</param>
        /// <returns>Pull request metadata</returns>
        [McpServerTool(
            Title = "Azure: Get Pull Request Info",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Get Pull Request metadata including title, author, status, and file count without performing analysis")]
        public async Task<OperationResult> AzureGetPullRequestInfo(
            [Description("Full Azure DevOps PR URL")] string prUrl)
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

                // Fetch PR information
                var prInfo = await _azureDevOpsService.GetPullRequestInfoAsync(org, project, repo, prId);

                // Get file count
                var fileChanges = await _azureDevOpsService.GetPullRequestChangesAsync(org, project, repo, prId);
                prInfo.TotalFiles = fileChanges.Count;

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
        /// <returns>Connection test result</returns>
        [McpServerTool(
            Title = "Azure: Test Azure DevOps Connection",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Test the connection to Azure DevOps using the configured Personal Access Token and verify authentication")]
        public async Task<OperationResult> AzureTestConnection()
        {
            try
            {
                _logger.LogInformation("Testing Azure DevOps connection");

                var isConnected = await _azureDevOpsService.TestConnectionAsync();

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
    }
}

