using DrSasuMcp.Tools.Datadog.Models;
using Microsoft.Extensions.Logging;

namespace DrSasuMcp.Tools.Datadog.Troubleshooters
{
    /// <summary>
    /// Troubleshooter for analyzing metric-related issues.
    /// </summary>
    public class MetricsTroubleshooter : ITroubleshooter
    {
        private readonly IDatadogService _datadogService;
        private readonly ILogger<MetricsTroubleshooter> _logger;

        public string TroubleshooterName => "Metrics";

        public MetricsTroubleshooter(
            IDatadogService datadogService,
            ILogger<MetricsTroubleshooter> logger)
        {
            _datadogService = datadogService;
            _logger = logger;
        }

        public bool SupportsIssueType(string issueType)
        {
            return issueType.Equals("metrics", StringComparison.OrdinalIgnoreCase) ||
                   issueType.Equals("performance", StringComparison.OrdinalIgnoreCase) ||
                   issueType.Equals("latency", StringComparison.OrdinalIgnoreCase) ||
                   issueType.Equals("throughput", StringComparison.OrdinalIgnoreCase);
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

                // Query common performance metrics
                var serviceFilter = !string.IsNullOrWhiteSpace(context.ServiceName)
                    ? $"service:{context.ServiceName}"
                    : "*";

                // Check CPU usage
                var cpuQuery = $"avg:system.cpu.user{{{serviceFilter}}}";
                var cpuResult = await _datadogService.QueryMetricsAsync(cpuQuery, from, to, cancellationToken);
                
                if (cpuResult?.Series != null && cpuResult.Series.Any())
                {
                    var avgCpu = cpuResult.Series
                        .SelectMany(s => s.Points)
                        .Where(p => p.Value.HasValue)
                        .Select(p => p.Value!.Value)
                        .DefaultIfEmpty(0)
                        .Average();

                    if (avgCpu > 80)
                    {
                        recommendations.Add(new TroubleshootingRecommendation
                        {
                            Title = "High CPU Usage Detected",
                            Description = $"Average CPU usage is {avgCpu:F1}%, which is above the recommended threshold of 80%.",
                            Severity = avgCpu > 90 ? "Critical" : "Warning",
                            Category = "Performance",
                            Steps = new List<string>
                            {
                                "Check for CPU-intensive processes",
                                "Review application code for inefficient algorithms",
                                "Consider scaling horizontally or vertically",
                                "Check for memory leaks that might cause garbage collection overhead"
                            },
                            RelatedMetrics = new Dictionary<string, string>
                            {
                                ["cpu_usage"] = cpuQuery
                            },
                            Confidence = 0.8
                        });
                    }
                }

                // Check memory usage
                var memoryQuery = $"avg:system.mem.used{{{serviceFilter}}}";
                var memoryResult = await _datadogService.QueryMetricsAsync(memoryQuery, from, to, cancellationToken);
                
                if (memoryResult?.Series != null && memoryResult.Series.Any())
                {
                    var avgMemory = memoryResult.Series
                        .SelectMany(s => s.Points)
                        .Where(p => p.Value.HasValue)
                        .Select(p => p.Value!.Value)
                        .DefaultIfEmpty(0)
                        .Average();

                    // This is a simplified check - in reality, you'd compare against total memory
                    if (avgMemory > 80)
                    {
                        recommendations.Add(new TroubleshootingRecommendation
                        {
                            Title = "High Memory Usage Detected",
                            Description = $"Average memory usage is {avgMemory:F1}%, which may indicate memory pressure.",
                            Severity = "Warning",
                            Category = "Performance",
                            Steps = new List<string>
                            {
                                "Check for memory leaks in application code",
                                "Review memory-intensive operations",
                                "Consider increasing available memory",
                                "Analyze garbage collection patterns"
                            },
                            RelatedMetrics = new Dictionary<string, string>
                            {
                                ["memory_usage"] = memoryQuery
                            },
                            Confidence = 0.7
                        });
                    }
                }

                // Check error rate if service name is provided
                if (!string.IsNullOrWhiteSpace(context.ServiceName))
                {
                    var errorRateQuery = $"sum:traces.errors{{{serviceFilter}}}.as_rate()";
                    var errorRateResult = await _datadogService.QueryMetricsAsync(errorRateQuery, from, to, cancellationToken);
                    
                    if (errorRateResult?.Series != null && errorRateResult.Series.Any())
                    {
                        var maxErrorRate = errorRateResult.Series
                            .SelectMany(s => s.Points)
                            .Where(p => p.Value.HasValue)
                            .Select(p => p.Value!.Value)
                            .DefaultIfEmpty(0)
                            .Max();

                        if (maxErrorRate > 0.01) // 1% error rate
                        {
                            recommendations.Add(new TroubleshootingRecommendation
                            {
                                Title = "Elevated Error Rate Detected",
                                Description = $"Error rate reached {maxErrorRate * 100:F2}%, which is above normal thresholds.",
                                Severity = maxErrorRate > 0.05 ? "Critical" : "Warning",
                                Category = "Error",
                                Steps = new List<string>
                                {
                                    "Review error logs for the service",
                                    "Check for recent deployments or configuration changes",
                                    "Analyze error patterns and stack traces",
                                    "Review service dependencies for cascading failures"
                                },
                                RelatedMetrics = new Dictionary<string, string>
                                {
                                    ["error_rate"] = errorRateQuery
                                },
                                Confidence = 0.9
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MetricsTroubleshooter analysis");
            }

            return recommendations;
        }
    }
}

