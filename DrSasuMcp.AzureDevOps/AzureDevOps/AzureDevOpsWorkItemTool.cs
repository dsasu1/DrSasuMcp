using DrSasuMcp.AzureDevOps.AzureDevOps.Models;
using DrSasuMcp.AzureDevOps.AzureDevOps.Utils;
using DrSasuMcp.Common.Models;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DrSasuMcp.AzureDevOps.AzureDevOps
{
    /// <summary>
    /// MCP tools for reading Azure DevOps work items.
    /// </summary>
    public partial class AzureDevOpsTool
    {
        private const int DefaultQueryTop = 50;

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
