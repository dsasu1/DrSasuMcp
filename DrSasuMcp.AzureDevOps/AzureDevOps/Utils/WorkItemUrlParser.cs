using System.Text.RegularExpressions;

namespace DrSasuMcp.AzureDevOps.AzureDevOps.Utils
{
    /// <summary>
    /// Utility class for parsing Azure DevOps work item references.
    /// </summary>
    public static class WorkItemUrlParser
    {
        // Supports, on both dev.azure.com and the legacy {org}.visualstudio.com host, with an optional project segment:
        //   .../_workitems/edit/{id}
        //   .../_workitems?id={id}
        private static readonly Regex WorkItemUrlRegex = new(
            @"^https?://(?:dev\.azure\.com/(?<org>[^/\s?#]+)|(?<orgLegacy>[^./\s]+)\.visualstudio\.com)(?:/(?<project>[^/\s?#]+))?/_workitems(?:/edit/(?<id>\d+)|/?\?(?:[^#]*&)?id=(?<queryId>\d+))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        // Matches the REST API URL form used by work item relations: .../_apis/wit/workItems/{id}
        private static readonly Regex WorkItemApiUrlRegex = new(
            @"/_apis/wit/workItems/(?<id>\d+)/?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private static readonly Regex WorkItemIdRegex = new(
            @"^#?(?<id>\d+)$",
            RegexOptions.Compiled
        );

        /// <summary>
        /// Parses an Azure DevOps work item URL and extracts its components.
        /// </summary>
        /// <param name="url">The work item URL to parse.</param>
        /// <returns>
        /// A tuple containing (organization, project, workItemId) if successful, or null if the URL is invalid.
        /// The project is null for organization-scoped URLs that omit the project segment.
        /// </returns>
        public static (string organization, string? project, int workItemId)? ParseWorkItemUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var match = WorkItemUrlRegex.Match(url.Trim());
            if (!match.Success)
                return null;

            var organization = match.Groups["org"].Success
                ? match.Groups["org"].Value
                : match.Groups["orgLegacy"].Value;

            var project = match.Groups["project"].Success ? match.Groups["project"].Value : null;

            var idGroup = match.Groups["id"].Success ? match.Groups["id"] : match.Groups["queryId"];
            if (!int.TryParse(idGroup.Value, out var workItemId))
                return null;

            return (organization, project, workItemId);
        }

        /// <summary>
        /// Checks if a URL is a valid Azure DevOps work item URL.
        /// </summary>
        /// <param name="url">The URL to validate.</param>
        /// <returns>True if the URL is valid, false otherwise.</returns>
        public static bool IsValidWorkItemUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return WorkItemUrlRegex.IsMatch(url.Trim());
        }

        /// <summary>
        /// Parses a work item reference that is either a bare ID (such as "1234" or "#1234") or a full work item URL.
        /// </summary>
        /// <param name="reference">The reference to parse.</param>
        /// <returns>
        /// A tuple containing (organization, project, workItemId) if successful, or null if the reference is invalid.
        /// Organization and project are null when a bare ID is supplied, and must then come from the caller's context.
        /// </returns>
        public static (string? organization, string? project, int workItemId)? ParseWorkItemReference(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                return null;

            var trimmed = reference.Trim();

            var idMatch = WorkItemIdRegex.Match(trimmed);
            if (idMatch.Success && int.TryParse(idMatch.Groups["id"].Value, out var id) && id > 0)
                return (null, null, id);

            var parsedUrl = ParseWorkItemUrl(trimmed);
            if (parsedUrl == null)
                return null;

            return (parsedUrl.Value.organization, parsedUrl.Value.project, parsedUrl.Value.workItemId);
        }

        /// <summary>
        /// Extracts the work item ID from an Azure DevOps REST API work item URL, as found in work item relations.
        /// </summary>
        /// <param name="url">The API URL to parse.</param>
        /// <returns>The work item ID, or null when the URL does not point at a work item.</returns>
        public static int? ParseWorkItemIdFromApiUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var match = WorkItemApiUrlRegex.Match(url.Trim());
            if (!match.Success)
                return null;

            return int.TryParse(match.Groups["id"].Value, out var id) ? id : null;
        }
    }
}
