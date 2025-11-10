using DrSasuMcp.Tools.Datadog.Models;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DrSasuMcp.Tools.Datadog
{
    /// <summary>
    /// Implementation of Datadog REST API client.
    /// </summary>
    public class DatadogService : IDatadogService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DatadogService> _logger;
        private readonly string? _apiKey;
        private readonly string? _appKey;
        private readonly string _baseUrl;
        private readonly int _timeoutSeconds;
        private readonly int _maxResults;

        public DatadogService(ILogger<DatadogService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();

            // Get configuration from environment variables
            _apiKey = Environment.GetEnvironmentVariable(DatadogToolConstants.EnvDatadogApiKey);
            _appKey = Environment.GetEnvironmentVariable(DatadogToolConstants.EnvDatadogAppKey);
            
            var site = GetStringFromEnv(DatadogToolConstants.EnvDatadogSite, DatadogToolConstants.DefaultSite);
            _baseUrl = string.Format(DatadogToolConstants.BaseUrlTemplate, site);
            
            _timeoutSeconds = GetIntFromEnv(DatadogToolConstants.EnvDatadogTimeout, DatadogToolConstants.DefaultTimeoutSeconds);
            _maxResults = GetIntFromEnv(DatadogToolConstants.EnvDatadogMaxResults, DatadogToolConstants.DefaultMaxResults);
            
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
            _httpClient.BaseAddress = new Uri(_baseUrl);

            // Set up authentication headers
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add(DatadogToolConstants.HeaderApiKey, _apiKey);
            }

            if (!string.IsNullOrWhiteSpace(_appKey))
            {
                _httpClient.DefaultRequestHeaders.Add(DatadogToolConstants.HeaderAppKey, _appKey);
            }

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                var endpoint = DatadogToolConstants.ValidateApiKeyEndpoint;
                var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<AccountInfo?> GetAccountInfoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                var endpoint = DatadogToolConstants.GetAccountInfoEndpoint;
                var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var accountInfo = JsonSerializer.Deserialize<AccountInfo>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return accountInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get account info");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<MetricQueryResult?> QueryMetricsAsync(
            string query,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                var endpoint = DatadogToolConstants.QueryMetricsEndpoint;
                var fromUnix = ((DateTimeOffset)from).ToUnixTimeSeconds();
                var toUnix = ((DateTimeOffset)to).ToUnixTimeSeconds();

                var url = $"{endpoint}?query={Uri.EscapeDataString(query)}&from={fromUnix}&to={toUnix}";
                
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<MetricQueryResult>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query metrics: {Query}", query);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<string>> ListMetricsAsync(
            DateTime? from = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                var endpoint = DatadogToolConstants.ListMetricsEndpoint;
                var url = endpoint;
                
                var queryParams = new List<string>();
                if (from.HasValue)
                {
                    var fromUnix = ((DateTimeOffset)from.Value).ToUnixTimeSeconds();
                    queryParams.Add($"filter[from]={fromUnix}");
                }

                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                
                // Parse JSON flexibly to handle different response formats
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var metrics = new List<string>();

                // Try v2 format: data.data (array of metric objects)
                if (root.TryGetProperty("data", out var dataElement))
                {
                    if (dataElement.ValueKind == JsonValueKind.Array)
                    {
                        // v2 format: data is an array
                        foreach (var item in dataElement.EnumerateArray())
                        {
                            if (item.TryGetProperty("id", out var idElement))
                            {
                                var id = idElement.GetString();
                                if (!string.IsNullOrEmpty(id))
                                {
                                    metrics.Add(id);
                                }
                            }
                            else if (item.TryGetProperty("attributes", out var attrsElement) &&
                                     attrsElement.TryGetProperty("name", out var nameElement))
                            {
                                var name = nameElement.GetString();
                                if (!string.IsNullOrEmpty(name))
                                {
                                    metrics.Add(name);
                                }
                            }
                        }
                    }
                    else if (dataElement.TryGetProperty("data", out var nestedData) &&
                             nestedData.ValueKind == JsonValueKind.Array)
                    {
                        // Nested data.data format
                        foreach (var item in nestedData.EnumerateArray())
                        {
                            if (item.TryGetProperty("id", out var idElement))
                            {
                                var id = idElement.GetString();
                                if (!string.IsNullOrEmpty(id))
                                {
                                    metrics.Add(id);
                                }
                            }
                        }
                    }
                    else if (dataElement.TryGetProperty("metrics", out var metricsElement) &&
                             metricsElement.ValueKind == JsonValueKind.Array)
                    {
                        // data.metrics format
                        foreach (var item in metricsElement.EnumerateArray())
                        {
                            if (item.TryGetProperty("id", out var idElement))
                            {
                                var id = idElement.GetString();
                                if (!string.IsNullOrEmpty(id))
                                {
                                    metrics.Add(id);
                                }
                            }
                        }
                    }
                }

                // Try v1 format: direct "metrics" array
                if (metrics.Count == 0 && root.TryGetProperty("metrics", out var v1Metrics) &&
                    v1Metrics.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in v1Metrics.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            metrics.Add(item.GetString() ?? string.Empty);
                        }
                    }
                }

                return metrics.Where(m => !string.IsNullOrEmpty(m)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list metrics");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<MetricMetadata?> GetMetricMetadataAsync(
            string metricName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                var endpoint = string.Format(DatadogToolConstants.GetMetricMetadataEndpoint, metricName);
                var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var metadata = JsonSerializer.Deserialize<MetricMetadata>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get metric metadata: {MetricName}", metricName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<LogQueryResult?> QueryLogsAsync(
            string query,
            DateTime from,
            DateTime to,
            int? limit = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                var endpoint = DatadogToolConstants.QueryLogsEndpoint;
                var fromUnix = ((DateTimeOffset)from).ToUnixTimeMilliseconds();
                var toUnix = ((DateTimeOffset)to).ToUnixTimeMilliseconds();
                
                var requestBody = new
                {
                    filter = new
                    {
                        query = query,
                        from = fromUnix,
                        to = toUnix
                    },
                    page = new
                    {
                        limit = limit ?? _maxResults
                    },
                    sort = "timestamp"
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                
                // Use flexible JSON parsing to handle varying response structures
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                var result = new LogQueryResult();

                // Parse events - try different response structures
                var events = new List<LogEvent>();
                
                // Try v2 format: data (array)
                if (root.TryGetProperty("data", out var dataElement))
                {
                    if (dataElement.ValueKind == JsonValueKind.Array)
                    {
                        // Direct array of events
                        foreach (var eventElement in dataElement.EnumerateArray())
                        {
                            var logEvent = ParseLogEvent(eventElement);
                            events.Add(logEvent);
                        }
                    }
                    else if (dataElement.ValueKind == JsonValueKind.Object)
                    {
                        // Nested data structure: data.data or data.events
                        if (dataElement.TryGetProperty("data", out var nestedData) && nestedData.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var eventElement in nestedData.EnumerateArray())
                            {
                                var logEvent = ParseLogEvent(eventElement);
                                events.Add(logEvent);
                            }
                        }
                        else if (dataElement.TryGetProperty("events", out var eventsArray) && eventsArray.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var eventElement in eventsArray.EnumerateArray())
                            {
                                var logEvent = ParseLogEvent(eventElement);
                                events.Add(logEvent);
                            }
                        }
                    }
                }
                
                // Try alternative: events (direct array)
                if (events.Count == 0 && root.TryGetProperty("events", out var rootEvents) && rootEvents.ValueKind == JsonValueKind.Array)
                {
                    foreach (var eventElement in rootEvents.EnumerateArray())
                    {
                        var logEvent = ParseLogEvent(eventElement);
                        events.Add(logEvent);
                    }
                }
                
                result.Events = events;
                
                // Parse meta
                if (root.TryGetProperty("meta", out var metaElement) && metaElement.ValueKind == JsonValueKind.Object)
                {
                    result.Meta = new LogMeta();
                    
                    if (metaElement.TryGetProperty("elapsed", out var elapsedElement) && elapsedElement.ValueKind == JsonValueKind.Number)
                        result.Meta.Elapsed = elapsedElement.GetInt32();
                    
                    if (metaElement.TryGetProperty("page", out var pageElement))
                    {
                        // Handle both int and string types
                        if (pageElement.ValueKind == JsonValueKind.Number)
                            result.Meta.Page = pageElement.GetInt32();
                        else if (pageElement.ValueKind == JsonValueKind.String)
                            result.Meta.Page = pageElement.GetString();
                        else
                            result.Meta.Page = pageElement.GetRawText();
                    }
                    
                    if (metaElement.TryGetProperty("page_count", out var pageCountElement) && pageCountElement.ValueKind == JsonValueKind.Number)
                        result.Meta.PageCount = pageCountElement.GetInt32();
                    
                    if (metaElement.TryGetProperty("total_count", out var totalCountElement) && totalCountElement.ValueKind == JsonValueKind.Number)
                        result.Meta.TotalCount = totalCountElement.GetInt32();
                }

                // Parse status and error
                if (root.TryGetProperty("status", out var rootStatusElement))
                    result.Status = rootStatusElement.GetString();
                
                if (root.TryGetProperty("error", out var errorElement))
                    result.Error = errorElement.GetString();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query logs: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Parses a single log event from a JsonElement.
        /// </summary>
        private LogEvent ParseLogEvent(JsonElement eventElement)
        {
            var logEvent = new LogEvent();
            
            if (eventElement.TryGetProperty("id", out var idElement))
                logEvent.Id = idElement.GetString();
            
            if (eventElement.TryGetProperty("content", out var contentElement))
                logEvent.Content = contentElement.GetString();
            
            if (eventElement.TryGetProperty("attributes", out var attrsElement) && attrsElement.ValueKind == JsonValueKind.Object)
            {
                logEvent.Attributes = ParseAttributes(attrsElement);
            }
            
            if (eventElement.TryGetProperty("tags", out var tagsElement) && tagsElement.ValueKind == JsonValueKind.Array)
            {
                logEvent.Tags = new Dictionary<string, string>();
                foreach (var tagElement in tagsElement.EnumerateArray())
                {
                    var tagStr = tagElement.GetString();
                    if (!string.IsNullOrEmpty(tagStr))
                    {
                        var parts = tagStr.Split(':');
                        if (parts.Length >= 2)
                        {
                            logEvent.Tags[parts[0]] = string.Join(":", parts.Skip(1));
                        }
                        else
                        {
                            logEvent.Tags[tagStr] = string.Empty;
                        }
                    }
                }
            }
            
            if (eventElement.TryGetProperty("timestamp", out var timestampElement))
            {
                if (timestampElement.ValueKind == JsonValueKind.Number && timestampElement.TryGetInt64(out var timestampMs))
                {
                    logEvent.Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).DateTime;
                }
            }
            
            if (eventElement.TryGetProperty("service", out var serviceElement))
                logEvent.Service = serviceElement.GetString();
            
            if (eventElement.TryGetProperty("status", out var eventStatusElement))
                logEvent.Status = eventStatusElement.GetString();
            
            return logEvent;
        }

        /// <summary>
        /// Recursively parses JSON attributes into a dictionary, preserving nested structures.
        /// </summary>
        private Dictionary<string, object> ParseAttributes(JsonElement element)
        {
            var result = new Dictionary<string, object>();
            
            if (element.ValueKind != JsonValueKind.Object)
                return result;

            foreach (var prop in element.EnumerateObject())
            {
                result[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => prop.Value.TryGetInt32(out var intVal) ? (object)intVal : prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null!,
                    JsonValueKind.Object => ParseAttributes(prop.Value), // Recursively parse nested objects
                    JsonValueKind.Array => ParseArray(prop.Value), // Parse arrays
                    _ => prop.Value.GetRawText()
                };
            }

            return result;
        }

        /// <summary>
        /// Parses a JSON array into a list of objects.
        /// </summary>
        private List<object> ParseArray(JsonElement element)
        {
            var result = new List<object>();
            
            if (element.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var item in element.EnumerateArray())
            {
                result.Add(item.ValueKind switch
                {
                    JsonValueKind.String => item.GetString() ?? string.Empty,
                    JsonValueKind.Number => item.TryGetInt32(out var intVal) ? (object)intVal : item.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null!,
                    JsonValueKind.Object => ParseAttributes(item), // Recursively parse nested objects
                    JsonValueKind.Array => ParseArray(item), // Recursively parse nested arrays
                    _ => item.GetRawText()
                });
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<TraceQueryResult?> QueryTracesAsync(
            string query,
            DateTime from,
            DateTime to,
            int? limit = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                var endpoint = DatadogToolConstants.QueryTracesEndpoint;
                var fromUnix = ((DateTimeOffset)from).ToUnixTimeSeconds();
                var toUnix = ((DateTimeOffset)to).ToUnixTimeSeconds();

                var url = $"{endpoint}?query={Uri.EscapeDataString(query)}&from={fromUnix}&to={toUnix}";
                if (limit.HasValue)
                {
                    url += $"&limit={limit.Value}";
                }

                var response = await _httpClient.GetAsync(url, cancellationToken);
                
                // Handle 404 gracefully - APM/traces may not be enabled
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Traces endpoint returned 404 - APM may not be enabled or endpoint may not be available");
                    return new TraceQueryResult
                    {
                        Traces = new List<Trace>(),
                        Status = "not_found",
                        Error = "Traces endpoint not found. APM (Application Performance Monitoring) may not be enabled in this account, or the endpoint may not be available via API."
                    };
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<TraceQueryResult>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result;
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("404"))
            {
                _logger.LogWarning(httpEx, "Traces endpoint not found - APM may not be enabled");
                return new TraceQueryResult
                {
                    Traces = new List<Trace>(),
                    Status = "not_found",
                    Error = "Traces endpoint not found. APM (Application Performance Monitoring) may not be enabled in this account, or the endpoint may not be available via API."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query traces: {Query}", query);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<Models.Monitor>> ListMonitorsAsync(
            string? status = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                var endpoint = DatadogToolConstants.ListMonitorsEndpoint;
                var url = endpoint;
                
                if (!string.IsNullOrWhiteSpace(status))
                {
                    url += $"?status={Uri.EscapeDataString(status)}";
                }

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var monitors = JsonSerializer.Deserialize<List<Models.Monitor>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return monitors ?? new List<Models.Monitor>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list monitors");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Models.Monitor?> GetMonitorAsync(
            long monitorId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                var endpoint = string.Format(DatadogToolConstants.GetMonitorEndpoint, monitorId);
                var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var monitor = JsonSerializer.Deserialize<Models.Monitor>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return monitor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get monitor: {MonitorId}", monitorId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<Alert>> GetActiveAlertsAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                // Get all monitors with alert status
                var monitors = await ListMonitorsAsync("Alert", cancellationToken);
                
                var alerts = new List<Alert>();
                foreach (var monitor in monitors.Where(m => m.Status == "Alert"))
                {
                    alerts.Add(new Alert
                    {
                        MonitorId = monitor.Id,
                        MonitorName = monitor.Name,
                        Status = monitor.Status,
                        Message = monitor.Message
                    });
                }

                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active alerts");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ErrorTrackingResult?> GetErrorIssuesAsync(
            string? serviceName = null,
            DateTime? from = null,
            DateTime? to = null,
            int? minCount = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                var endpoint = DatadogToolConstants.GetErrorTrackingEventsEndpoint;
                var url = endpoint;
                
                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(serviceName))
                {
                    queryParams.Add($"service={Uri.EscapeDataString(serviceName)}");
                }
                if (from.HasValue)
                {
                    var fromUnix = ((DateTimeOffset)from.Value).ToUnixTimeSeconds();
                    queryParams.Add($"from={fromUnix}");
                }
                if (to.HasValue)
                {
                    var toUnix = ((DateTimeOffset)to.Value).ToUnixTimeSeconds();
                    queryParams.Add($"to={toUnix}");
                }
                if (minCount.HasValue)
                {
                    queryParams.Add($"min_count={minCount.Value}");
                }

                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await _httpClient.GetAsync(url, cancellationToken);
                
                // Handle 404 gracefully - RUM error tracking may not be enabled
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Error tracking endpoint returned 404 - RUM error tracking may not be enabled");
                    return new ErrorTrackingResult
                    {
                        Issues = new List<ErrorIssue>(),
                        Status = "not_found",
                        Error = "Error tracking endpoint not found. RUM error tracking may not be enabled in this account."
                    };
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<ErrorTrackingResult>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result;
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("404"))
            {
                _logger.LogWarning(httpEx, "Error tracking endpoint not found - RUM may not be enabled");
                return new ErrorTrackingResult
                {
                    Issues = new List<ErrorIssue>(),
                    Status = "not_found",
                    Error = "Error tracking endpoint not found. RUM error tracking may not be enabled in this account."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get error issues");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceMap?> GetServiceMapAsync(
            string? serviceName = null,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                var endpoint = DatadogToolConstants.GetServiceMapEndpoint;
                var url = endpoint;
                
                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(serviceName))
                {
                    queryParams.Add($"service={Uri.EscapeDataString(serviceName)}");
                }
                if (from.HasValue)
                {
                    var fromUnix = ((DateTimeOffset)from.Value).ToUnixTimeSeconds();
                    queryParams.Add($"from={fromUnix}");
                }
                if (to.HasValue)
                {
                    var toUnix = ((DateTimeOffset)to.Value).ToUnixTimeSeconds();
                    queryParams.Add($"to={toUnix}");
                }

                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await _httpClient.GetAsync(url, cancellationToken);
                
                // Handle 404 gracefully - Service map may not be available via API
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Service map endpoint returned 404 - Service map may not be available via API or APM may not be enabled");
                    // Try alternative endpoint: /api/v2/service_dependencies
                    var altEndpoint = "/api/v2/service_dependencies";
                    var altUrl = altEndpoint;
                    if (queryParams.Any())
                    {
                        altUrl += "?" + string.Join("&", queryParams);
                    }
                    
                    var altResponse = await _httpClient.GetAsync(altUrl, cancellationToken);
                    if (altResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return new ServiceMap
                        {
                            Nodes = new List<ServiceNode>(),
                            Edges = new List<ServiceEdge>()
                        };
                    }
                    
                    altResponse.EnsureSuccessStatusCode();
                    var altContent = await altResponse.Content.ReadAsStringAsync(cancellationToken);
                    var altServiceMap = JsonSerializer.Deserialize<ServiceMap>(altContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return altServiceMap;
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var serviceMap = JsonSerializer.Deserialize<ServiceMap>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return serviceMap;
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("404"))
            {
                _logger.LogWarning(httpEx, "Service map endpoint not found - may not be available via API");
                return new ServiceMap
                {
                    Nodes = new List<ServiceNode>(),
                    Edges = new List<ServiceEdge>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get service map");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<Dashboard>> ListDashboardsAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                var endpoint = DatadogToolConstants.ListDashboardsEndpoint;
                var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<DashboardListResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result?.Dashboards ?? new List<Dashboard>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list dashboards");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Dashboard?> GetDashboardAsync(
            string dashboardId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                var endpoint = string.Format(DatadogToolConstants.GetDashboardEndpoint, dashboardId);
                var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var dashboard = JsonSerializer.Deserialize<Dashboard>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get dashboard: {DashboardId}", dashboardId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<EventQueryResult?> QueryEventsAsync(
            string? query = null,
            DateTime? from = null,
            DateTime? to = null,
            int? limit = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                var endpoint = DatadogToolConstants.QueryEventsEndpoint;
                var url = endpoint;
                
                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(query))
                {
                    queryParams.Add($"text={Uri.EscapeDataString(query)}");
                }
                if (from.HasValue)
                {
                    var fromUnix = ((DateTimeOffset)from.Value).ToUnixTimeSeconds();
                    queryParams.Add($"start={fromUnix}");
                }
                if (to.HasValue)
                {
                    var toUnix = ((DateTimeOffset)to.Value).ToUnixTimeSeconds();
                    queryParams.Add($"end={toUnix}");
                }
                if (limit.HasValue)
                {
                    queryParams.Add($"limit={limit.Value}");
                }

                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<EventQueryResult>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query events");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Event?> CreateEventAsync(
            string title,
            string text,
            string? alertType = null,
            string? priority = null,
            List<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateAuthentication();

                var endpoint = DatadogToolConstants.CreateEventEndpoint;
                var requestBody = new
                {
                    title = title,
                    text = text,
                    alert_type = alertType ?? "info",
                    priority = priority ?? "normal",
                    tags = tags ?? new List<string>()
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<EventCreateResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result?.Event;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create event");
                throw;
            }
        }

        private void ValidateAuthentication()
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new UnauthorizedAccessException(
                    $"Datadog API key not configured. Set {DatadogToolConstants.EnvDatadogApiKey} environment variable.");
            }
        }

        private static int GetIntFromEnv(string varName, int defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(varName);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        private static string GetStringFromEnv(string varName, string defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(varName);
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }
    }

    /// <summary>
    /// Response model for metric list API v1.
    /// </summary>
    internal class MetricListResponse
    {
        public List<string>? Metrics { get; set; }
    }

    /// <summary>
    /// Response model for metric list API v2.
    /// </summary>
    internal class MetricListV2Response
    {
        public MetricListV2Data? Data { get; set; }
    }

    /// <summary>
    /// Data container for v2 metrics list.
    /// </summary>
    internal class MetricListV2Data
    {
        public List<MetricListV2Item>? Metrics { get; set; }
    }

    /// <summary>
    /// Individual metric item in v2 response.
    /// </summary>
    internal class MetricListV2Item
    {
        public string? Id { get; set; }
        public MetricListV2Attributes? Attributes { get; set; }
    }

    /// <summary>
    /// Attributes for v2 metric item.
    /// </summary>
    internal class MetricListV2Attributes
    {
        public string? Name { get; set; }
    }

    /// <summary>
    /// Response model for dashboard list API.
    /// </summary>
    internal class DashboardListResponse
    {
        public List<Dashboard>? Dashboards { get; set; }
    }

    /// <summary>
    /// Response model for event create API.
    /// </summary>
    internal class EventCreateResponse
    {
        public Event? Event { get; set; }
        public string? Status { get; set; }
    }
}

