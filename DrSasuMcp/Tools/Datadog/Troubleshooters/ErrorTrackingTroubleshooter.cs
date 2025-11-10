using DrSasuMcp.Tools.Datadog.Models;
using Microsoft.Extensions.Logging;

namespace DrSasuMcp.Tools.Datadog.Troubleshooters
{
    /// <summary>
    /// Troubleshooter for analyzing error tracking issues.
    /// </summary>
    public class ErrorTrackingTroubleshooter : ITroubleshooter
    {
        private readonly IDatadogService _datadogService;
        private readonly ILogger<ErrorTrackingTroubleshooter> _logger;

        public string TroubleshooterName => "ErrorTracking";

        public ErrorTrackingTroubleshooter(
            IDatadogService datadogService,
            ILogger<ErrorTrackingTroubleshooter> logger)
        {
            _datadogService = datadogService;
            _logger = logger;
        }

        public bool SupportsIssueType(string issueType)
        {
            return issueType.Equals("errors", StringComparison.OrdinalIgnoreCase) ||
                   issueType.Equals("error-tracking", StringComparison.OrdinalIgnoreCase) ||
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

                var errorResult = await _datadogService.GetErrorIssuesAsync(
                    context.ServiceName,
                    from,
                    to,
                    minCount: 1,
                    cancellationToken);

                if (errorResult?.Issues != null && errorResult.Issues.Any())
                {
                    var criticalErrors = errorResult.Issues
                        .Where(e => e.Count > 10)
                        .OrderByDescending(e => e.Count)
                        .ToList();

                    if (criticalErrors.Any())
                    {
                        var topError = criticalErrors.First();
                        recommendations.Add(new TroubleshootingRecommendation
                        {
                            Title = "High-Volume Error Issue Detected",
                            Description = $"Error issue '{topError.Message}' occurred {topError.Count} times. First seen: {topError.FirstSeen}, Last seen: {topError.LastSeen}.",
                            Severity = topError.Count > 100 ? "Critical" : "Warning",
                            Category = "Error",
                            Steps = new List<string>
                            {
                                "Review the error message and stack trace",
                                "Check if this error correlates with recent deployments",
                                "Analyze the error frequency pattern",
                                "Review related logs and traces",
                                "Check for similar errors in other services"
                            },
                            Confidence = 0.95
                        });
                    }

                    // Check for new errors
                    var newErrors = errorResult.Issues
                        .Where(e => e.FirstSeen.HasValue && 
                                   e.FirstSeen.Value > from.AddMinutes(-30))
                        .ToList();

                    if (newErrors.Any())
                    {
                        recommendations.Add(new TroubleshootingRecommendation
                        {
                            Title = "New Error Issues Detected",
                            Description = $"Found {newErrors.Count} new error issue(s) that appeared recently. This may indicate a recent change or regression.",
                            Severity = newErrors.Count > 5 ? "Critical" : "Warning",
                            Category = "Error",
                            Steps = new List<string>
                            {
                                "Review recent deployments or configuration changes",
                                "Check if these errors are related to new features",
                                "Compare error patterns before and after the change",
                                "Review deployment logs and change history"
                            },
                            Confidence = 0.85
                        });
                    }

                    // Check for error trends
                    var increasingErrors = errorResult.Issues
                        .Where(e => e.FirstSeen.HasValue && e.LastSeen.HasValue &&
                                   e.LastSeen.Value > e.FirstSeen.Value.AddMinutes(30) &&
                                   e.Count > 5)
                        .ToList();

                    if (increasingErrors.Any())
                    {
                        recommendations.Add(new TroubleshootingRecommendation
                        {
                            Title = "Increasing Error Trend Detected",
                            Description = $"Found {increasingErrors.Count} error issue(s) with increasing frequency over time.",
                            Severity = "Warning",
                            Category = "Error",
                            Steps = new List<string>
                            {
                                "Monitor error rate trends closely",
                                "Check for resource exhaustion (memory, connections)",
                                "Review for cascading failures from dependencies",
                                "Consider implementing circuit breakers",
                                "Review auto-scaling configuration"
                            },
                            Confidence = 0.8
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ErrorTrackingTroubleshooter analysis");
            }

            return recommendations;
        }
    }
}

