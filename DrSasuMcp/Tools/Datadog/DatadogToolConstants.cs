namespace DrSasuMcp.Tools.Datadog
{
    /// <summary>
    /// Constants and configuration values for the Datadog tool.
    /// </summary>
    public static class DatadogToolConstants
    {
        // Environment Variables
        /// <summary>
        /// Environment variable name for Datadog API Key.
        /// </summary>
        public const string EnvDatadogApiKey = "DD_API_KEY";

        /// <summary>
        /// Environment variable name for Datadog Application Key.
        /// </summary>
        public const string EnvDatadogAppKey = "DD_APP_KEY";

        /// <summary>
        /// Environment variable name for Datadog site.
        /// </summary>
        public const string EnvDatadogSite = "DD_SITE";

        /// <summary>
        /// Environment variable name for request timeout in seconds.
        /// </summary>
        public const string EnvDatadogTimeout = "DD_TIMEOUT_SECONDS";

        /// <summary>
        /// Environment variable name for maximum results per query.
        /// </summary>
        public const string EnvDatadogMaxResults = "DD_MAX_RESULTS";

        // Default Values
        /// <summary>
        /// Default timeout for HTTP requests in seconds.
        /// </summary>
        public const int DefaultTimeoutSeconds = 60;

        /// <summary>
        /// Default maximum results per query.
        /// </summary>
        public const int DefaultMaxResults = 1000;

        /// <summary>
        /// Default Datadog site.
        /// </summary>
        public const string DefaultSite = "datadoghq.com";

        // API Configuration
        /// <summary>
        /// Base URL template for Datadog API.
        /// Format: site
        /// </summary>
        public const string BaseUrlTemplate = "https://api.{0}";

        // API Endpoints
        /// <summary>
        /// Endpoint to validate API key.
        /// </summary>
        public const string ValidateApiKeyEndpoint = "/api/v1/validate";

        /// <summary>
        /// Endpoint to get account/organization info.
        /// </summary>
        public const string GetAccountInfoEndpoint = "/api/v1/org";

        /// <summary>
        /// Endpoint to query metrics.
        /// </summary>
        public const string QueryMetricsEndpoint = "/api/v1/query";

        /// <summary>
        /// Endpoint to list available metrics.
        /// </summary>
        public const string ListMetricsEndpoint = "/api/v2/metrics";

        /// <summary>
        /// Endpoint to get metric metadata.
        /// </summary>
        public const string GetMetricMetadataEndpoint = "/api/v1/metrics/{0}";

        /// <summary>
        /// Endpoint to query logs.
        /// </summary>
        public const string QueryLogsEndpoint = "/api/v2/logs/events/search";

        /// <summary>
        /// Endpoint to query logs with aggregation.
        /// </summary>
        public const string QueryLogsAggregationEndpoint = "/api/v2/logs/analytics/aggregate";

        /// <summary>
        /// Endpoint to list monitors.
        /// </summary>
        public const string ListMonitorsEndpoint = "/api/v1/monitor";

        /// <summary>
        /// Endpoint to get monitor details.
        /// </summary>
        public const string GetMonitorEndpoint = "/api/v1/monitor/{0}";

        /// <summary>
        /// Endpoint to get monitor history.
        /// </summary>
        public const string GetMonitorHistoryEndpoint = "/api/v1/monitor/{0}/history";

        /// <summary>
        /// Endpoint to list dashboards.
        /// </summary>
        public const string ListDashboardsEndpoint = "/api/v1/dashboard";

        /// <summary>
        /// Endpoint to get dashboard details.
        /// </summary>
        public const string GetDashboardEndpoint = "/api/v1/dashboard/{0}";

        /// <summary>
        /// Endpoint to query events.
        /// </summary>
        public const string QueryEventsEndpoint = "/api/v1/events";

        /// <summary>
        /// Endpoint to create event.
        /// </summary>
        public const string CreateEventEndpoint = "/api/v1/events";

        /// <summary>
        /// Endpoint to get service map.
        /// </summary>
        public const string GetServiceMapEndpoint = "/api/v1/service_map";

        /// <summary>
        /// Endpoint to query traces.
        /// </summary>
        public const string QueryTracesEndpoint = "/api/v0.2/traces";

        /// <summary>
        /// Endpoint to get error tracking events.
        /// </summary>
        public const string GetErrorTrackingEventsEndpoint = "/api/v1/rum/error-tracking/events";

        // HTTP Headers
        /// <summary>
        /// Header name for API key.
        /// </summary>
        public const string HeaderApiKey = "DD-API-KEY";

        /// <summary>
        /// Header name for application key.
        /// </summary>
        public const string HeaderAppKey = "DD-APPLICATION-KEY";

        // Query Parameters
        /// <summary>
        /// Maximum query time range in hours.
        /// </summary>
        public const int MaxQueryTimeRangeHours = 168; // 7 days

        /// <summary>
        /// Default query time range in hours.
        /// </summary>
        public const int DefaultQueryTimeRangeHours = 1;
    }
}

