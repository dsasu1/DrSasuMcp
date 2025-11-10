using DrSasuMcp.Tools.Datadog.Models;

namespace DrSasuMcp.Tools.Datadog
{
    /// <summary>
    /// Interface for Datadog API service operations.
    /// </summary>
    public interface IDatadogService
    {
        /// <summary>
        /// Tests the connection to Datadog API.
        /// </summary>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets account/organization information.
        /// </summary>
        Task<AccountInfo?> GetAccountInfoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries metrics.
        /// </summary>
        Task<MetricQueryResult?> QueryMetricsAsync(
            string query,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists available metrics.
        /// </summary>
        Task<List<string>> ListMetricsAsync(
            DateTime? from = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets metadata for a specific metric.
        /// </summary>
        Task<MetricMetadata?> GetMetricMetadataAsync(
            string metricName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries logs.
        /// </summary>
        Task<LogQueryResult?> QueryLogsAsync(
            string query,
            DateTime from,
            DateTime to,
            int? limit = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries traces.
        /// </summary>
        Task<TraceQueryResult?> QueryTracesAsync(
            string query,
            DateTime from,
            DateTime to,
            int? limit = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists monitors.
        /// </summary>
        Task<List<Models.Monitor>> ListMonitorsAsync(
            string? status = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets monitor details.
        /// </summary>
        Task<Models.Monitor?> GetMonitorAsync(
            long monitorId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets active alerts.
        /// </summary>
        Task<List<Alert>> GetActiveAlertsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets error tracking issues.
        /// </summary>
        Task<ErrorTrackingResult?> GetErrorIssuesAsync(
            string? serviceName = null,
            DateTime? from = null,
            DateTime? to = null,
            int? minCount = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets service map.
        /// </summary>
        Task<ServiceMap?> GetServiceMapAsync(
            string? serviceName = null,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists dashboards.
        /// </summary>
        Task<List<Dashboard>> ListDashboardsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets dashboard details.
        /// </summary>
        Task<Dashboard?> GetDashboardAsync(
            string dashboardId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries events.
        /// </summary>
        Task<EventQueryResult?> QueryEventsAsync(
            string? query = null,
            DateTime? from = null,
            DateTime? to = null,
            int? limit = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates an event.
        /// </summary>
        Task<Event?> CreateEventAsync(
            string title,
            string text,
            string? alertType = null,
            string? priority = null,
            List<string>? tags = null,
            CancellationToken cancellationToken = default);
    }
}

