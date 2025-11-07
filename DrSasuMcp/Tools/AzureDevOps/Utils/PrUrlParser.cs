using System.Text.RegularExpressions;

namespace DrSasuMcp.Tools.AzureDevOps.Utils
{
    /// <summary>
    /// Utility class for parsing Azure DevOps pull request URLs.
    /// </summary>
    public static class PrUrlParser
    {
        // Supports: https://dev.azure.com/{org}/{project}/_git/{repo}/pullrequest/{id}
        private static readonly Regex PrUrlRegex = new(
            @"https://dev\.azure\.com/(?<org>[^/]+)/(?<project>[^/]+)/_git/(?<repo>[^/]+)/pullrequest/(?<id>\d+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        /// <summary>
        /// Parses an Azure DevOps pull request URL and extracts its components.
        /// </summary>
        /// <param name="url">The pull request URL to parse.</param>
        /// <returns>
        /// A tuple containing (organization, project, repository, pullRequestId) if successful,
        /// or null if the URL is invalid.
        /// </returns>
        public static (string organization, string project, string repository, int pullRequestId)? ParsePrUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var match = PrUrlRegex.Match(url);
            if (!match.Success)
                return null;

            var organization = match.Groups["org"].Value;
            var project = match.Groups["project"].Value;
            var repository = match.Groups["repo"].Value;
            var pullRequestId = int.Parse(match.Groups["id"].Value);

            return (organization, project, repository, pullRequestId);
        }

        /// <summary>
        /// Checks if a URL is a valid Azure DevOps pull request URL.
        /// </summary>
        /// <param name="url">The URL to validate.</param>
        /// <returns>True if the URL is valid, false otherwise.</returns>
        public static bool IsValidPrUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return PrUrlRegex.IsMatch(url);
        }
    }
}

