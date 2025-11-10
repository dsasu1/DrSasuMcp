using DrSasuMcp.Tools.Datadog.Models;
using Microsoft.Extensions.Logging;

namespace DrSasuMcp.Tools.Datadog.Troubleshooters
{
    /// <summary>
    /// Troubleshooter for analyzing log-related issues.
    /// </summary>
    public class LogsTroubleshooter : ITroubleshooter
    {
        private readonly IDatadogService _datadogService;
        private readonly ILogger<LogsTroubleshooter> _logger;

        public string TroubleshooterName => "Logs";

        public LogsTroubleshooter(
            IDatadogService datadogService,
            ILogger<LogsTroubleshooter> logger)
        {
            _datadogService = datadogService;
            _logger = logger;
        }

        public bool SupportsIssueType(string issueType)
        {
            return issueType.Equals("logs", StringComparison.OrdinalIgnoreCase) ||
                   issueType.Equals("errors", StringComparison.OrdinalIgnoreCase) ||
                   issueType.Equals("exceptions", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<TroubleshootingRecommendation>> AnalyzeAsync(
            TroubleshootingContext context,
            CancellationToken cancellationToken = default)
        {
            var recommendations = new List<TroubleshootingRecommendation>();

            try
            {
                var from = context.From ?? DateTime.UtcNow.AddHours(-1);
                var to = context.To ?? DateTime.UtcNow;

                // Build log query
                var logQuery = new List<string>();
                if (!string.IsNullOrWhiteSpace(context.ServiceName))
                {
                    logQuery.Add($"service:{context.ServiceName}");
                }
                
                // Search for errors
                logQuery.Add("status:error");
                var query = string.Join(" ", logQuery);

                var logResult = await _datadogService.QueryLogsAsync(query, from, to, limit: 100, cancellationToken);

                if (logResult?.Events != null && logResult.Events.Any())
                {
                    var errorCount = logResult.Events.Count;
                    var uniqueErrors = logResult.Events
                        .GroupBy(e => e.Content?.Substring(0, Math.Min(100, e.Content?.Length ?? 0)))
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .ToList();

                    if (errorCount > 10)
                    {
                        recommendations.Add(new TroubleshootingRecommendation
                        {
                            Title = "High Error Log Volume Detected",
                            Description = $"Found {errorCount} error logs in the specified time range. This indicates potential issues requiring attention.",
                            Severity = errorCount > 100 ? "Critical" : "Warning",
                            Category = "Error",
                            Steps = new List<string>
                            {
                                "Review the most frequent error patterns",
                                "Check for common error messages in the logs",
                                "Analyze error timing to identify patterns",
                                "Review application code for error handling improvements",
                                "Check for configuration issues"
                            },
                            RelatedLogs = new Dictionary<string, string>
                            {
                                ["error_query"] = query,
                                ["error_count"] = errorCount.ToString()
                            },
                            Confidence = 0.85
                        });
                    }

                    // Analyze error patterns
                    if (uniqueErrors.Any())
                    {
                        var topError = uniqueErrors.First();
                        recommendations.Add(new TroubleshootingRecommendation
                        {
                            Title = "Most Frequent Error Pattern",
                            Description = $"The most common error pattern occurred {topError.Count()} times: {topError?.Key?.Substring(0, Math.Min(200, topError.Key.Length))}...",
                            Severity = topError?.Count() > 50 ? "Critical" : "Warning",
                            Category = "Error",
                            Steps = new List<string>
                            {
                                "Investigate the root cause of this specific error",
                                "Check if this error is related to recent changes",
                                "Review stack traces for this error pattern",
                                "Check service dependencies that might be causing this error"
                            },
                            Confidence = 0.9
                        });
                    }
                }

                // Check for exception patterns
                var exceptionQuery = string.Join(" ", logQuery.Where(q => !q.Contains("status:error"))) + " exception";
                var exceptionResult = await _datadogService.QueryLogsAsync(exceptionQuery, from, to, limit: 50, cancellationToken);

                if (exceptionResult?.Events != null && exceptionResult.Events.Any())
                {
                    var exceptionTypes = exceptionResult.Events
                        .SelectMany(e => ExtractExceptionType(e.Content ?? ""))
                        .GroupBy(t => t)
                        .OrderByDescending(g => g.Count())
                        .Take(3)
                        .ToList();

                    if (exceptionTypes.Any())
                    {
                        recommendations.Add(new TroubleshootingRecommendation
                        {
                            Title = "Exception Patterns Detected",
                            Description = $"Found {exceptionTypes.Count} distinct exception types in logs.",
                            Severity = "Warning",
                            Category = "Error",
                            Steps = new List<string>
                            {
                                "Review exception stack traces",
                                "Check for null reference exceptions (common cause)",
                                "Review input validation and error handling",
                                "Check for resource exhaustion (memory, connections, etc.)"
                            },
                            Confidence = 0.75
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LogsTroubleshooter analysis");
            }

            return recommendations;
        }

        private List<string> ExtractExceptionType(string content)
        {
            var exceptions = new List<string>();
            // Simple pattern matching for common exception types
            var patterns = new[] { "NullReferenceException", "ArgumentException", "TimeoutException", "HttpRequestException", "SqlException" };
            
            foreach (var pattern in patterns)
            {
                if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    exceptions.Add(pattern);
                }
            }

            return exceptions;
        }
    }
}

