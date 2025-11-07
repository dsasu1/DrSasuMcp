using DrSasuMcp.Tools.AzureDevOps.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DrSasuMcp.Tools.AzureDevOps.Analyzers
{
    /// <summary>
    /// Analyzer for detecting security issues in code changes.
    /// </summary>
    public class SecurityAnalyzer : ICodeAnalyzer
    {
        private readonly ILogger<SecurityAnalyzer> _logger;

        /// <inheritdoc/>
        public string AnalyzerName => "Security";

        private static readonly Dictionary<string, Regex> SecurityPatterns = new()
        {
            ["SEC001"] = new Regex(@"password\s*=\s*[""'][^""']+[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["SEC002"] = new Regex(@"api[_-]?key\s*=\s*[""'][^""']+[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["SEC003"] = new Regex(@"(secret|token)\s*=\s*[""'][^""']+[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["SEC004"] = new Regex(@"SELECT\s+.*\s+FROM\s+.*\s*\+\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["SEC005"] = new Regex(@"(new\s+MD5CryptoServiceProvider|new\s+SHA1Managed|MD5\.Create|SHA1\.Create)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["SEC006"] = new Regex(@"\.InnerHtml\s*=\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["SEC007"] = new Regex(@"eval\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["SEC008"] = new Regex(@"Process\.Start\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["SEC009"] = new Regex(@"File\.Delete\s*\(\s*.*\s*\+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["SEC010"] = new Regex(@"(accessToken|bearer|auth).*[""'][A-Za-z0-9+/=]{20,}[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };

        private static readonly Dictionary<string, string> SecurityMessages = new()
        {
            ["SEC001"] = "Potential hardcoded password detected",
            ["SEC002"] = "Potential hardcoded API key detected",
            ["SEC003"] = "Potential hardcoded secret or token detected",
            ["SEC004"] = "Potential SQL injection vulnerability - use parameterized queries",
            ["SEC005"] = "Weak cryptographic algorithm (MD5/SHA1) - use SHA256 or stronger",
            ["SEC006"] = "Potential XSS vulnerability - validate and encode user input",
            ["SEC007"] = "Use of eval() is dangerous - avoid dynamic code execution",
            ["SEC008"] = "Process execution detected - validate and sanitize inputs",
            ["SEC009"] = "File deletion with concatenated path - validate to prevent path traversal",
            ["SEC010"] = "Potential hardcoded authentication token detected"
        };

        private static readonly Dictionary<string, IssueLevel> SecurityLevels = new()
        {
            ["SEC001"] = IssueLevel.Critical,
            ["SEC002"] = IssueLevel.Critical,
            ["SEC003"] = IssueLevel.Critical,
            ["SEC004"] = IssueLevel.Critical,
            ["SEC005"] = IssueLevel.Warning,
            ["SEC006"] = IssueLevel.Critical,
            ["SEC007"] = IssueLevel.Critical,
            ["SEC008"] = IssueLevel.Warning,
            ["SEC009"] = IssueLevel.Warning,
            ["SEC010"] = IssueLevel.Critical
        };

        public SecurityAnalyzer(ILogger<SecurityAnalyzer> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public Task<List<ReviewComment>> AnalyzeFileChangeAsync(
            FileChange fileChange,
            CancellationToken cancellationToken = default)
        {
            var comments = new List<ReviewComment>();

            // Only analyze added/modified content
            if (fileChange.ChangeType == ChangeType.Deleted)
                return Task.FromResult(comments);

            if (!SupportsFileType(fileChange.FilePath))
                return Task.FromResult(comments);

            var content = fileChange.ModifiedContent ?? string.Empty;
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                foreach (var pattern in SecurityPatterns)
                {
                    if (pattern.Value.IsMatch(line))
                    {
                        comments.Add(new ReviewComment
                        {
                            FilePath = fileChange.FilePath,
                            Line = i + 1,
                            Level = SecurityLevels[pattern.Key],
                            Analyzer = AnalyzerName,
                            Code = pattern.Key,
                            Message = SecurityMessages[pattern.Key],
                            CodeSnippet = line.Trim(),
                            Suggestion = GetSecuritySuggestion(pattern.Key)
                        });

                        _logger.LogDebug("Security issue {Code} found in {FilePath} at line {Line}",
                            pattern.Key, fileChange.FilePath, i + 1);
                    }
                }
            }

            return Task.FromResult(comments);
        }

        /// <inheritdoc/>
        public bool SupportsFileType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext is ".cs" or ".js" or ".ts" or ".tsx" or ".jsx" or ".java" or ".py" or ".go" or ".php" or ".rb";
        }

        private static string GetSecuritySuggestion(string code)
        {
            return code switch
            {
                "SEC001" or "SEC002" or "SEC003" or "SEC010" =>
                    "Use environment variables, Azure Key Vault, or secure configuration management",
                "SEC004" =>
                    "Use parameterized queries, prepared statements, or an ORM framework",
                "SEC005" =>
                    "Use SHA256, SHA384, SHA512, or modern algorithms like BLAKE2",
                "SEC006" =>
                    "Use TextContent instead of InnerHtml, or properly encode/sanitize user input",
                "SEC007" =>
                    "Use JSON.parse() for data or refactor to avoid dynamic code execution",
                "SEC008" =>
                    "Validate and sanitize all inputs; use allowlists for commands",
                "SEC009" =>
                    "Validate file paths and use Path.Combine() with proper validation",
                _ => string.Empty
            };
        }
    }
}

