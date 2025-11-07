namespace DrSasuMcp.Tools.AzureDevOps
{
    /// <summary>
    /// Constants and configuration values for the Azure DevOps tool.
    /// </summary>
    public static class AzureDevOpsToolConstants
    {
        // Environment Variables
        /// <summary>
        /// Environment variable name for Azure DevOps Personal Access Token.
        /// </summary>
        public const string EnvAzureDevOpsPat = "AZURE_DEVOPS_PAT";

        /// <summary>
        /// Environment variable name for default Azure DevOps organization.
        /// </summary>
        public const string EnvAzureDevOpsOrg = "AZURE_DEVOPS_ORG";

        /// <summary>
        /// Environment variable name for maximum number of files to analyze.
        /// </summary>
        public const string EnvAzureDevOpsMaxFiles = "AZURE_DEVOPS_MAX_FILES";

        /// <summary>
        /// Environment variable name for request timeout in seconds.
        /// </summary>
        public const string EnvAzureDevOpsTimeout = "AZURE_DEVOPS_TIMEOUT";

        // Default Values
        /// <summary>
        /// Default timeout for HTTP requests in seconds.
        /// </summary>
        public const int DefaultTimeoutSeconds = 60;

        /// <summary>
        /// Default maximum number of files to analyze per PR.
        /// </summary>
        public const int DefaultMaxFiles = 100;

        /// <summary>
        /// Default maximum file size to analyze in bytes (1MB).
        /// </summary>
        public const int DefaultMaxFileSizeBytes = 1048576;

        // API Configuration
        /// <summary>
        /// Azure DevOps REST API version.
        /// </summary>
        public const string ApiVersion = "7.1";

        /// <summary>
        /// Base URL template for Azure DevOps API.
        /// Format: org, project
        /// </summary>
        public const string BaseUrlTemplate = "https://dev.azure.com/{0}/{1}/_apis";

        // API Endpoints
        /// <summary>
        /// Endpoint to get pull request details.
        /// Format: repositoryId, pullRequestId
        /// </summary>
        public const string GetPullRequestEndpoint = "/git/repositories/{0}/pullRequests/{1}";

        /// <summary>
        /// Endpoint to get pull request iterations.
        /// Format: repositoryId, pullRequestId
        /// </summary>
        public const string GetPullRequestIterationsEndpoint = "/git/repositories/{0}/pullRequests/{1}/iterations";

        /// <summary>
        /// Endpoint to get pull request changes for an iteration.
        /// Format: repositoryId, pullRequestId, iterationId
        /// </summary>
        public const string GetPullRequestChangesEndpoint = "/git/repositories/{0}/pullRequests/{1}/iterations/{2}/changes";

        /// <summary>
        /// Endpoint to get file content.
        /// Format: repositoryId
        /// </summary>
        public const string GetFileContentEndpoint = "/git/repositories/{0}/items";

        // Analysis Configuration
        /// <summary>
        /// Maximum recommended method length in lines.
        /// </summary>
        public const int MaxMethodLength = 50;

        /// <summary>
        /// Maximum recommended class length in lines.
        /// </summary>
        public const int MaxClassLength = 500;

        /// <summary>
        /// Maximum recommended cyclomatic complexity.
        /// </summary>
        public const int MaxCyclomaticComplexity = 10;

        // Issue Code Prefixes
        /// <summary>
        /// Prefix for security issue codes.
        /// </summary>
        public const string SecurityIssuePrefix = "SEC";

        /// <summary>
        /// Prefix for code quality issue codes.
        /// </summary>
        public const string QualityIssuePrefix = "QUAL";

        /// <summary>
        /// Prefix for best practice issue codes.
        /// </summary>
        public const string BestPracticeIssuePrefix = "BP";
    }
}

