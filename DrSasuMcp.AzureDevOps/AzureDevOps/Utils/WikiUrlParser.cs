using System.Text.RegularExpressions;

namespace DrSasuMcp.AzureDevOps.AzureDevOps.Utils
{
    /// <summary>
    /// Utility class for parsing Azure DevOps wiki page URLs.
    /// </summary>
    public static class WikiUrlParser
    {
        // Supports, on both dev.azure.com and the legacy {org}.visualstudio.com host:
        //   .../{project}/_wiki/wikis/{wiki}
        //   .../{project}/_wiki/wikis/{wiki}/{pageId}
        //   .../{project}/_wiki/wikis/{wiki}/{pageId}/{Page-Slug}
        //   .../{project}/_wiki/wikis/{wiki}?pagePath=%2FFoo%2FBar
        private static readonly Regex WikiUrlRegex = new(
            @"^https?://(?:dev\.azure\.com/(?<org>[^/\s?#]+)|(?<orgLegacy>[^./\s]+)\.visualstudio\.com)/(?<project>[^/\s?#]+)/_wiki/wikis/(?<wiki>[^/\s?#]+)(?:/(?<pageId>\d+)(?:/[^?#]*)?)?/?(?:\?(?<query>[^#]*))?(?:#.*)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        /// <summary>
        /// Parses an Azure DevOps wiki URL and extracts its components.
        /// </summary>
        /// <param name="url">The wiki URL to parse.</param>
        /// <returns>
        /// A tuple containing (organization, project, wiki, pageId, pagePath) if successful, or null if the URL is invalid.
        /// Exactly one of pageId and pagePath is set. A pagePath query parameter wins over a page ID,
        /// and a URL pointing at the wiki itself resolves to the root path "/".
        /// </returns>
        public static (string organization, string project, string wiki, int? pageId, string? pagePath)? ParseWikiUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var match = WikiUrlRegex.Match(url.Trim());
            if (!match.Success)
                return null;

            var organization = match.Groups["org"].Success
                ? match.Groups["org"].Value
                : match.Groups["orgLegacy"].Value;

            var project = Uri.UnescapeDataString(match.Groups["project"].Value);
            var wiki = Uri.UnescapeDataString(match.Groups["wiki"].Value);

            var pagePath = GetQueryParameter(match.Groups["query"].Value, "pagePath");
            if (!string.IsNullOrWhiteSpace(pagePath))
                return (organization, project, wiki, null, pagePath);

            if (match.Groups["pageId"].Success && int.TryParse(match.Groups["pageId"].Value, out var pageId))
                return (organization, project, wiki, pageId, null);

            return (organization, project, wiki, null, "/");
        }

        /// <summary>
        /// Checks if a URL is a valid Azure DevOps wiki URL.
        /// </summary>
        /// <param name="url">The URL to validate.</param>
        /// <returns>True if the URL is valid, false otherwise.</returns>
        public static bool IsValidWikiUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return WikiUrlRegex.IsMatch(url.Trim());
        }

        private static string? GetQueryParameter(string query, string name)
        {
            if (string.IsNullOrEmpty(query))
                return null;

            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = pair.IndexOf('=');
                var key = separator < 0 ? pair : pair.Substring(0, separator);
                if (!key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = separator < 0 ? string.Empty : pair.Substring(separator + 1);

                // Query strings may encode spaces as '+', which UnescapeDataString leaves alone
                return Uri.UnescapeDataString(value.Replace('+', ' '));
            }

            return null;
        }
    }
}
