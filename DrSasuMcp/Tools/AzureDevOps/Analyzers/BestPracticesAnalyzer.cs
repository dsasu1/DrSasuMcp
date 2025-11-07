using DrSasuMcp.Tools.AzureDevOps.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DrSasuMcp.Tools.AzureDevOps.Analyzers
{
    /// <summary>
    /// Analyzer for detecting violations of coding best practices.
    /// </summary>
    public class BestPracticesAnalyzer : ICodeAnalyzer
    {
        private readonly ILogger<BestPracticesAnalyzer> _logger;

        /// <inheritdoc/>
        public string AnalyzerName => "BestPractices";

        // Patterns for detecting best practice issues
        private static readonly Dictionary<string, Regex> BestPracticePatterns = new()
        {
            ["BP001"] = new Regex(@"catch\s*\(\s*Exception\s+\w+\s*\)\s*\{\s*\}", RegexOptions.Compiled),
            ["BP002"] = new Regex(@"catch\s*\([^)]+\)\s*\{\s*//", RegexOptions.Compiled),
            ["BP003"] = new Regex(@"\.Result\b", RegexOptions.Compiled),
            ["BP004"] = new Regex(@"\.Wait\(\)", RegexOptions.Compiled),
            ["BP005"] = new Regex(@"string\s+\w+\s*=\s*"""";(\s*\w+\s*\+=)", RegexOptions.Compiled),
            ["BP006"] = new Regex(@"==\s*null\b", RegexOptions.Compiled),
            ["BP007"] = new Regex(@"!=\s*null\b", RegexOptions.Compiled),
            ["BP008"] = new Regex(@"class\s+\w+\s*:\s*IDisposable", RegexOptions.Compiled),
            ["BP009"] = new Regex(@"new\s+HttpClient\s*\(", RegexOptions.Compiled),
            ["BP010"] = new Regex(@"Console\.WriteLine", RegexOptions.Compiled)
        };

        private static readonly Dictionary<string, string> BestPracticeMessages = new()
        {
            ["BP001"] = "Empty catch block - exceptions are silently swallowed",
            ["BP002"] = "Catch block only contains comments - possible incomplete error handling",
            ["BP003"] = "Blocking on async code with .Result - use await instead",
            ["BP004"] = "Blocking on async code with .Wait() - use await instead",
            ["BP005"] = "String concatenation in loop detected - use StringBuilder",
            ["BP006"] = "Use 'is null' instead of '== null' for better readability",
            ["BP007"] = "Use 'is not null' instead of '!= null' for better readability",
            ["BP008"] = "IDisposable implementation detected - ensure Dispose pattern is correctly implemented",
            ["BP009"] = "Creating HttpClient with 'new' - consider using IHttpClientFactory",
            ["BP010"] = "Console.WriteLine detected - use proper logging framework"
        };

        private static readonly Dictionary<string, IssueLevel> BestPracticeLevels = new()
        {
            ["BP001"] = IssueLevel.Critical,
            ["BP002"] = IssueLevel.Warning,
            ["BP003"] = IssueLevel.Warning,
            ["BP004"] = IssueLevel.Warning,
            ["BP005"] = IssueLevel.Warning,
            ["BP006"] = IssueLevel.Info,
            ["BP007"] = IssueLevel.Info,
            ["BP008"] = IssueLevel.Info,
            ["BP009"] = IssueLevel.Warning,
            ["BP010"] = IssueLevel.Info
        };

        public BestPracticesAnalyzer(ILogger<BestPracticesAnalyzer> logger)
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
                var lineNumber = i + 1;

                foreach (var pattern in BestPracticePatterns)
                {
                    if (pattern.Value.IsMatch(line))
                    {
                        comments.Add(new ReviewComment
                        {
                            FilePath = fileChange.FilePath,
                            Line = lineNumber,
                            Level = BestPracticeLevels[pattern.Key],
                            Analyzer = AnalyzerName,
                            Code = pattern.Key,
                            Message = BestPracticeMessages[pattern.Key],
                            CodeSnippet = line.Trim(),
                            Suggestion = GetBestPracticeSuggestion(pattern.Key)
                        });

                        _logger.LogDebug("Best practice issue {Code} found in {FilePath} at line {Line}",
                            pattern.Key, fileChange.FilePath, lineNumber);
                    }
                }
            }

            // Additional contextual checks
            CheckAsyncPatterns(lines, fileChange.FilePath, comments);
            CheckResourceManagement(lines, fileChange.FilePath, comments);
            CheckErrorHandling(lines, fileChange.FilePath, comments);

            return Task.FromResult(comments);
        }

        /// <inheritdoc/>
        public bool SupportsFileType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext is ".cs" or ".js" or ".ts" or ".tsx" or ".jsx" or ".java" or ".py";
        }

        private void CheckAsyncPatterns(string[] lines, string filePath, List<ReviewComment> comments)
        {
            // Check for async methods without await
            var asyncMethodPattern = new Regex(@"async\s+\w+\s+\w+", RegexOptions.Compiled);
            var awaitPattern = new Regex(@"\bawait\b", RegexOptions.Compiled);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (asyncMethodPattern.IsMatch(line))
                {
                    // Check next 20 lines for await
                    var hasAwait = false;
                    for (int j = i; j < Math.Min(i + 20, lines.Length); j++)
                    {
                        if (awaitPattern.IsMatch(lines[j]))
                        {
                            hasAwait = true;
                            break;
                        }
                    }

                    if (!hasAwait)
                    {
                        comments.Add(new ReviewComment
                        {
                            FilePath = filePath,
                            Line = i + 1,
                            Level = IssueLevel.Warning,
                            Analyzer = AnalyzerName,
                            Code = "BP011",
                            Message = "Async method detected without await keyword",
                            CodeSnippet = line.Trim(),
                            Suggestion = "Either use await or remove async keyword if not needed"
                        });
                    }
                }
            }
        }

        private void CheckResourceManagement(string[] lines, string filePath, List<ReviewComment> comments)
        {
            // Check for disposable objects not in using statements
            var disposableCreationPattern = new Regex(@"new\s+(FileStream|StreamReader|StreamWriter|SqlConnection|HttpClient)\s*\(", RegexOptions.Compiled);
            var usingPattern = new Regex(@"\busing\s*\(", RegexOptions.Compiled);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (disposableCreationPattern.IsMatch(line) && !usingPattern.IsMatch(line))
                {
                    // Check if this is part of a using statement on previous line
                    var isPreviousLineUsing = i > 0 && usingPattern.IsMatch(lines[i - 1]);

                    if (!isPreviousLineUsing)
                    {
                        comments.Add(new ReviewComment
                        {
                            FilePath = filePath,
                            Line = i + 1,
                            Level = IssueLevel.Warning,
                            Analyzer = AnalyzerName,
                            Code = "BP012",
                            Message = "Disposable object created without using statement",
                            CodeSnippet = line.Trim(),
                            Suggestion = "Wrap disposable objects in using statements or using declarations"
                        });
                    }
                }
            }
        }

        private void CheckErrorHandling(string[] lines, string filePath, List<ReviewComment> comments)
        {
            // Check for generic exception catching
            var genericCatchPattern = new Regex(@"catch\s*\(\s*Exception", RegexOptions.Compiled);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (genericCatchPattern.IsMatch(line) && !line.Contains("//"))
                {
                    comments.Add(new ReviewComment
                    {
                        FilePath = filePath,
                        Line = i + 1,
                        Level = IssueLevel.Info,
                        Analyzer = AnalyzerName,
                        Code = "BP013",
                        Message = "Catching generic Exception - consider catching specific exceptions",
                        CodeSnippet = line.Trim(),
                        Suggestion = "Catch specific exception types when possible for better error handling"
                    });
                }
            }
        }

        private static string GetBestPracticeSuggestion(string code)
        {
            return code switch
            {
                "BP001" => "Log the exception or rethrow it if you can't handle it properly",
                "BP002" => "Implement proper error handling or remove the empty catch block",
                "BP003" or "BP004" => "Use 'await' keyword instead of blocking synchronously",
                "BP005" => "Use StringBuilder for string concatenation in loops",
                "BP006" => "Use pattern matching: if (obj is null)",
                "BP007" => "Use pattern matching: if (obj is not null)",
                "BP008" => "Implement Dispose() method and call base.Dispose() if inherited",
                "BP009" => "Use IHttpClientFactory to manage HttpClient lifetime properly",
                "BP010" => "Use ILogger<T> or similar logging framework instead of Console.WriteLine",
                "BP011" => "Either await async operations or remove async modifier",
                "BP012" => "Use 'using' statement or 'using' declaration to ensure disposal",
                "BP013" => "Catch specific exceptions (e.g., ArgumentNullException, IOException)",
                _ => string.Empty
            };
        }
    }
}

