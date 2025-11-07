using DrSasuMcp.Tools.AzureDevOps.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DrSasuMcp.Tools.AzureDevOps.Analyzers
{
    /// <summary>
    /// Analyzer for detecting code quality issues.
    /// </summary>
    public class CodeQualityAnalyzer : ICodeAnalyzer
    {
        private readonly ILogger<CodeQualityAnalyzer> _logger;

        /// <inheritdoc/>
        public string AnalyzerName => "CodeQuality";

        // Patterns for detecting quality issues
        private static readonly Regex MagicNumberPattern = new(@"\b\d{2,}\b(?!\s*;)", RegexOptions.Compiled);
        private static readonly Regex TodoCommentPattern = new(@"(TODO|FIXME|HACK|XXX):", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex LongLinePattern = new(@"^.{120,}", RegexOptions.Compiled);
        private static readonly Regex DuplicateCodePattern = new(@"^(\s*)(.+)$", RegexOptions.Compiled);

        public CodeQualityAnalyzer(ILogger<CodeQualityAnalyzer> logger)
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

            // Check file length
            if (lines.Length > AzureDevOpsToolConstants.MaxClassLength)
            {
                comments.Add(new ReviewComment
                {
                    FilePath = fileChange.FilePath,
                    Line = null,
                    Level = IssueLevel.Warning,
                    Analyzer = AnalyzerName,
                    Code = "QUAL001",
                    Message = $"File is too long ({lines.Length} lines). Consider splitting into smaller files.",
                    Suggestion = "Split large classes into smaller, more focused classes following Single Responsibility Principle"
                });
            }

            // Analyze each line
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNumber = i + 1;

                // Check for TODO comments
                if (TodoCommentPattern.IsMatch(line))
                {
                    comments.Add(new ReviewComment
                    {
                        FilePath = fileChange.FilePath,
                        Line = lineNumber,
                        Level = IssueLevel.Info,
                        Analyzer = AnalyzerName,
                        Code = "QUAL002",
                        Message = "TODO/FIXME comment found",
                        CodeSnippet = line.Trim(),
                        Suggestion = "Complete or track this task in your project management system"
                    });
                }

                // Check for long lines
                if (LongLinePattern.IsMatch(line))
                {
                    comments.Add(new ReviewComment
                    {
                        FilePath = fileChange.FilePath,
                        Line = lineNumber,
                        Level = IssueLevel.Info,
                        Analyzer = AnalyzerName,
                        Code = "QUAL003",
                        Message = $"Line is too long ({line.Length} characters)",
                        Suggestion = "Keep lines under 120 characters for better readability"
                    });
                }

                // Check for magic numbers (in code, not comments)
                if (!line.TrimStart().StartsWith("//") && !line.TrimStart().StartsWith("*"))
                {
                    var magicMatches = MagicNumberPattern.Matches(line);
                    if (magicMatches.Count > 0)
                    {
                        // Filter out common non-magic numbers
                        var suspiciousNumbers = magicMatches
                            .Where(m => !IsCommonNumber(m.Value))
                            .ToList();

                        if (suspiciousNumbers.Any())
                        {
                            comments.Add(new ReviewComment
                            {
                                FilePath = fileChange.FilePath,
                                Line = lineNumber,
                                Level = IssueLevel.Info,
                                Analyzer = AnalyzerName,
                                Code = "QUAL004",
                                Message = "Potential magic number detected",
                                CodeSnippet = line.Trim(),
                                Suggestion = "Consider extracting magic numbers into named constants"
                            });
                        }
                    }
                }
            }

            // Check for methods that are too long
            AnalyzeMethodLength(lines, fileChange.FilePath, comments);

            // Check for code complexity
            AnalyzeComplexity(lines, fileChange.FilePath, comments);

            // Check naming conventions
            AnalyzeNamingConventions(lines, fileChange.FilePath, comments);

            return Task.FromResult(comments);
        }

        /// <inheritdoc/>
        public bool SupportsFileType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext is ".cs" or ".js" or ".ts" or ".tsx" or ".jsx" or ".java" or ".py" or ".go";
        }

        private void AnalyzeMethodLength(string[] lines, string filePath, List<ReviewComment> comments)
        {
            var methodPattern = new Regex(@"(public|private|protected|internal|static)\s+\w+\s+\w+\s*\(", RegexOptions.Compiled);
            int? methodStartLine = null;
            int braceDepth = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (methodPattern.IsMatch(line))
                {
                    methodStartLine = i + 1;
                    braceDepth = 0;
                }

                if (methodStartLine.HasValue)
                {
                    braceDepth += line.Count(c => c == '{');
                    braceDepth -= line.Count(c => c == '}');

                    if (braceDepth == 0 && line.Contains('}'))
                    {
                        var methodLength = i + 1 - methodStartLine.Value;
                        if (methodLength > AzureDevOpsToolConstants.MaxMethodLength)
                        {
                            comments.Add(new ReviewComment
                            {
                                FilePath = filePath,
                                Line = methodStartLine.Value,
                                Level = IssueLevel.Warning,
                                Analyzer = AnalyzerName,
                                Code = "QUAL005",
                                Message = $"Method is too long ({methodLength} lines)",
                                Suggestion = "Consider breaking down large methods into smaller, more focused methods"
                            });
                        }
                        methodStartLine = null;
                    }
                }
            }
        }

        private void AnalyzeComplexity(string[] lines, string filePath, List<ReviewComment> comments)
        {
            // Simple cyclomatic complexity check based on control flow keywords
            var complexityKeywords = new[] { "if", "else", "for", "foreach", "while", "case", "catch", "&&", "||" };

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var complexity = complexityKeywords.Sum(keyword => 
                    Regex.Matches(line, $@"\b{Regex.Escape(keyword)}\b").Count);

                if (complexity >= 5)
                {
                    comments.Add(new ReviewComment
                    {
                        FilePath = filePath,
                        Line = i + 1,
                        Level = IssueLevel.Warning,
                        Analyzer = AnalyzerName,
                        Code = "QUAL006",
                        Message = $"High cyclomatic complexity detected (complexity: {complexity})",
                        CodeSnippet = line.Trim(),
                        Suggestion = "Simplify complex conditions using early returns or extracting methods"
                    });
                }
            }
        }

        private void AnalyzeNamingConventions(string[] lines, string filePath, List<ReviewComment> comments)
        {
            // Check for C# naming conventions
            if (Path.GetExtension(filePath).ToLowerInvariant() == ".cs")
            {
                var privateFieldPattern = new Regex(@"private\s+\w+\s+([A-Z]\w+)\s*[;=]", RegexOptions.Compiled);
                var publicFieldPattern = new Regex(@"public\s+\w+\s+([a-z]\w+)\s*[;=]", RegexOptions.Compiled);

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    // Private fields should start with lowercase or underscore
                    var privateMatch = privateFieldPattern.Match(line);
                    if (privateMatch.Success && !line.Contains("const"))
                    {
                        comments.Add(new ReviewComment
                        {
                            FilePath = filePath,
                            Line = i + 1,
                            Level = IssueLevel.Info,
                            Analyzer = AnalyzerName,
                            Code = "QUAL007",
                            Message = "Private field should use camelCase or start with underscore",
                            CodeSnippet = line.Trim(),
                            Suggestion = $"Consider renaming to _{privateMatch.Groups[1].Value.ToLowerInvariant()}"
                        });
                    }

                    // Public properties should be PascalCase
                    var publicMatch = publicFieldPattern.Match(line);
                    if (publicMatch.Success && !line.Contains("const"))
                    {
                        comments.Add(new ReviewComment
                        {
                            FilePath = filePath,
                            Line = i + 1,
                            Level = IssueLevel.Info,
                            Analyzer = AnalyzerName,
                            Code = "QUAL008",
                            Message = "Public member should use PascalCase",
                            CodeSnippet = line.Trim(),
                            Suggestion = $"Consider renaming to {char.ToUpper(publicMatch.Groups[1].Value[0])}{publicMatch.Groups[1].Value.Substring(1)}"
                        });
                    }
                }
            }
        }

        private static bool IsCommonNumber(string number)
        {
            // Common numbers that are not magic: 0, 1, 2, 10, 100, etc.
            var commonNumbers = new[] { "0", "1", "2", "10", "100", "1000" };
            return commonNumbers.Contains(number);
        }
    }
}

