using DrSasuMcp.Tools.Datadog.Models;
using Microsoft.Extensions.Logging;

namespace DrSasuMcp.Tools.Datadog.Troubleshooters
{
    /// <summary>
    /// Troubleshooter for analyzing trace-related issues.
    /// </summary>
    public class TracesTroubleshooter : ITroubleshooter
    {
        private readonly IDatadogService _datadogService;
        private readonly ILogger<TracesTroubleshooter> _logger;

        public string TroubleshooterName => "Traces";

        public TracesTroubleshooter(
            IDatadogService datadogService,
            ILogger<TracesTroubleshooter> logger)
        {
            _datadogService = datadogService;
            _logger = logger;
        }

        public bool SupportsIssueType(string issueType)
        {
            return issueType.Equals("traces", StringComparison.OrdinalIgnoreCase) ||
                   issueType.Equals("apm", StringComparison.OrdinalIgnoreCase) ||
                   issueType.Equals("latency", StringComparison.OrdinalIgnoreCase) ||
                   issueType.Equals("performance", StringComparison.OrdinalIgnoreCase);
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

                var query = !string.IsNullOrWhiteSpace(context.ServiceName)
                    ? $"service:{context.ServiceName}"
                    : "*";

                var traceResult = await _datadogService.QueryTracesAsync(query, from, to, limit: 100, cancellationToken);

                if (traceResult?.Traces != null && traceResult.Traces.Any())
                {
                    // Analyze latency
                    var highLatencyTraces = traceResult.Traces
                        .Where(t => t.Duration.HasValue && t.Duration > 1000) // > 1 second
                        .OrderByDescending(t => t.Duration)
                        .ToList();

                    if (highLatencyTraces.Any())
                    {
                        var topTrace = highLatencyTraces.First();
                        recommendations.Add(new TroubleshootingRecommendation
                        {
                            Title = "High Latency Traces Detected",
                            Description = $"Found {highLatencyTraces.Count} traces with latency > 1s. Highest latency: {topTrace.Duration}ms for service '{topTrace.Service}'.",
                            Severity = topTrace.Duration > 5000 ? "Critical" : "Warning",
                            Category = "Performance",
                            Steps = new List<string>
                            {
                                "Review slow trace spans to identify bottlenecks",
                                "Check database query performance",
                                "Review external API call latencies",
                                "Check for N+1 query problems",
                                "Review caching strategies"
                            },
                            RelatedTraces = new Dictionary<string, string>
                            {
                                ["slow_traces_query"] = query,
                                ["slow_trace_count"] = highLatencyTraces.Count.ToString()
                            },
                            Confidence = 0.85
                        });
                    }

                    // Analyze error traces
                    var errorTraces = traceResult.Traces
                        .Where(t => t.Spans.Any(s => s.Meta != null && 
                                                   s.Meta.ContainsKey("error") &&
                                                   s.Meta["error"]?.ToString() == "true"))
                        .ToList();

                    if (errorTraces.Any())
                    {
                        recommendations.Add(new TroubleshootingRecommendation
                        {
                            Title = "Error Traces Detected",
                            Description = $"Found {errorTraces.Count} traces containing errors. This indicates failures in request processing.",
                            Severity = errorTraces.Count > 10 ? "Critical" : "Warning",
                            Category = "Error",
                            Steps = new List<string>
                            {
                                "Review error spans in traces",
                                "Check error messages and stack traces",
                                "Identify common error patterns",
                                "Review service dependencies for failures",
                                "Check for timeout issues"
                            },
                            Confidence = 0.9
                        });
                    }

                    // Analyze span depth (potential performance issue)
                    var deepTraces = traceResult.Traces
                        .Where(t => t.Spans.Count > 20)
                        .ToList();

                    if (deepTraces.Any())
                    {
                        recommendations.Add(new TroubleshootingRecommendation
                        {
                            Title = "Deep Trace Spans Detected",
                            Description = $"Found {deepTraces.Count} traces with > 20 spans, indicating complex call chains that may impact performance.",
                            Severity = "Warning",
                            Category = "Performance",
                            Steps = new List<string>
                            {
                                "Review trace structure for optimization opportunities",
                                "Consider reducing call chain depth",
                                "Check for unnecessary service calls",
                                "Review microservice boundaries"
                            },
                            Confidence = 0.7
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TracesTroubleshooter analysis");
            }

            return recommendations;
        }
    }
}

