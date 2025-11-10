using DrSasuMcp.Tools.Datadog.Models;
using Microsoft.Extensions.Logging;

namespace DrSasuMcp.Tools.Datadog.Troubleshooters
{
    /// <summary>
    /// Troubleshooter for analyzing service map and dependency issues.
    /// </summary>
    public class ServiceMapTroubleshooter : ITroubleshooter
    {
        private readonly IDatadogService _datadogService;
        private readonly ILogger<ServiceMapTroubleshooter> _logger;

        public string TroubleshooterName => "ServiceMap";

        public ServiceMapTroubleshooter(
            IDatadogService datadogService,
            ILogger<ServiceMapTroubleshooter> logger)
        {
            _datadogService = datadogService;
            _logger = logger;
        }

        public bool SupportsIssueType(string issueType)
        {
            return issueType.Equals("service-map", StringComparison.OrdinalIgnoreCase) ||
                   issueType.Equals("dependencies", StringComparison.OrdinalIgnoreCase) ||
                   issueType.Equals("cascading", StringComparison.OrdinalIgnoreCase);
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

                var serviceMap = await _datadogService.GetServiceMapAsync(
                    context.ServiceName,
                    from,
                    to);

                if (serviceMap == null || !serviceMap.Nodes.Any())
                {
                    return recommendations;
                }

                // Analyze service health
                var unhealthyServices = serviceMap.Nodes
                    .Where(n => n.Health != null && 
                               (n.Health.Status == "error" || 
                                n.Health.ErrorRate > 0.05 ||
                                n.Health.Latency > 1000))
                    .ToList();

                if (unhealthyServices.Any())
                {
                    var service = unhealthyServices.First();
                    recommendations.Add(new TroubleshootingRecommendation
                    {
                        Title = "Unhealthy Service Detected",
                        Description = $"Service '{service.Service}' shows health issues: Status={service.Health?.Status}, ErrorRate={service.Health?.ErrorRate:P2}, Latency={service.Health?.Latency}ms",
                        Severity = service.Health?.ErrorRate > 0.1 ? "Critical" : "Warning",
                        Category = "Service Health",
                        Steps = new List<string>
                        {
                            "Check service logs for errors",
                            "Review service metrics for anomalies",
                            "Check service dependencies",
                            "Review recent deployments",
                            "Check resource utilization (CPU, memory)"
                        },
                        Confidence = 0.9
                    });
                }

                // Analyze dependencies
                if (!string.IsNullOrWhiteSpace(context.ServiceName))
                {
                    var serviceNode = serviceMap.Nodes
                        .FirstOrDefault(n => n.Service?.Equals(context.ServiceName, StringComparison.OrdinalIgnoreCase) == true);

                    if (serviceNode != null)
                    {
                        var dependencies = serviceMap.Edges
                            .Where(e => e.To?.Equals(context.ServiceName, StringComparison.OrdinalIgnoreCase) == true)
                            .Select(e => e.From)
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList();

                        var dependents = serviceMap.Edges
                            .Where(e => e.From?.Equals(context.ServiceName, StringComparison.OrdinalIgnoreCase) == true)
                            .Select(e => e.To)
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList();

                        if (dependencies.Count > 10)
                        {
                            recommendations.Add(new TroubleshootingRecommendation
                            {
                                Title = "High Dependency Count",
                                Description = $"Service '{context.ServiceName}' has {dependencies.Count} dependencies, which may indicate tight coupling and potential cascading failure risk.",
                                Severity = "Warning",
                                Category = "Architecture",
                                Steps = new List<string>
                                {
                                    "Review service architecture for decoupling opportunities",
                                    "Consider implementing circuit breakers",
                                    "Review dependency health and resilience",
                                    "Consider service mesh for better dependency management"
                                },
                                Confidence = 0.7
                            });
                        }

                        if (dependents.Count > 5)
                        {
                            recommendations.Add(new TroubleshootingRecommendation
                            {
                                Title = "Critical Service Dependency",
                                Description = $"Service '{context.ServiceName}' is a dependency for {dependents.Count} other services. Issues in this service could cascade to multiple services.",
                                Severity = "Warning",
                                Category = "Architecture",
                                Steps = new List<string>
                                {
                                    "Ensure high availability and resilience",
                                    "Implement comprehensive monitoring and alerting",
                                    "Review and test failure scenarios",
                                    "Consider redundancy and failover mechanisms"
                                },
                                Confidence = 0.8
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ServiceMapTroubleshooter analysis");
            }

            return recommendations;
        }
    }
}

