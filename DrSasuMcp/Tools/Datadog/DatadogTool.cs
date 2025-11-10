using DrSasuMcp.Tools;
using DrSasuMcp.Tools.Datadog.Models;
using DrSasuMcp.Tools.Datadog.Troubleshooters;
using DrSasuMcp.Tools.Datadog.Utils;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace DrSasuMcp.Tools.Datadog
{
    /// <summary>
    /// MCP tool for Datadog monitoring and troubleshooting.
    /// </summary>
    [McpServerToolType]
    public partial class DatadogTool
    {
        private readonly IDatadogService _datadogService;
        private readonly ILogger<DatadogTool> _logger;
        private readonly IEnumerable<ITroubleshooter> _troubleshooters;

        public DatadogTool(
            IDatadogService datadogService,
            ILogger<DatadogTool> logger,
            IEnumerable<ITroubleshooter> troubleshooters)
        {
            _datadogService = datadogService;
            _logger = logger;
            _troubleshooters = troubleshooters;
        }

        /// <summary>
        /// Tests the connection to Datadog using the configured API key and verifies authentication.
        /// </summary>
        [McpServerTool(
            Title = "Datadog: Test Connection",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Test the connection to Datadog using the configured API key and verify authentication")]
        public async Task<OperationResult> DatadogTestConnection()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Testing Datadog connection");

                var isConnected = await _datadogService.TestConnectionAsync();

                if (isConnected)
                {
                    // Try to get account info to verify full access
                    AccountInfo? accountInfo = null;
                    try
                    {
                        accountInfo = await _datadogService.GetAccountInfoAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Connection test passed but failed to get account info");
                    }

                    stopwatch.Stop();

                    return new OperationResult(
                        success: true,
                        data: new
                        {
                            connected = true,
                            message = "Successfully connected to Datadog",
                            accountName = accountInfo?.Name,
                            accountPublicId = accountInfo?.PublicId,
                            region = accountInfo?.Region,
                            connectionTimeMs = stopwatch.ElapsedMilliseconds
                        }
                    );
                }
                else
                {
                    stopwatch.Stop();
                    return new OperationResult(
                        success: false,
                        error: "Failed to connect to Datadog. Check your API key and network connection."
                    );
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Authentication failed");
                return new OperationResult(
                    success: false,
                    error: $"Authentication failed: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Connection test failed");
                return new OperationResult(
                    success: false,
                    error: $"Connection test failed: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Tests multiple Datadog API endpoints to verify they are accessible and not returning errors.
        /// </summary>
        // [McpServerTool(
        //     Title = "Datadog: Test Endpoints",
        //     ReadOnly = true,
        //     Idempotent = true,
        //     Destructive = false),
        //     Description("Test multiple Datadog API endpoints to verify they are accessible and not returning 404 or other errors")]
        public async Task<OperationResult> DatadogTestEndpoints()
        {
            var results = new Dictionary<string, object>();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Testing Datadog API endpoints");

                // Test 1: Connection/Validation
                try
                {
                    var connected = await _datadogService.TestConnectionAsync();
                    results["connection"] = new { status = connected ? "success" : "failed", message = connected ? "Connected successfully" : "Connection failed" };
                }
                catch (Exception ex)
                {
                    results["connection"] = new { status = "error", message = ex.Message };
                }

                // Test 2: Account Info
                try
                {
                    var accountInfo = await _datadogService.GetAccountInfoAsync();
                    results["account_info"] = new { status = accountInfo != null ? "success" : "no_data", message = accountInfo != null ? "Account info retrieved" : "No account info returned" };
                }
                catch (Exception ex)
                {
                    results["account_info"] = new { status = "error", message = ex.Message, httpStatus = ex.Message.Contains("404") ? "404" : ex.Message.Contains("401") ? "401" : "unknown" };
                }

                // Test 3: List Metrics
                try
                {
                    var metrics = await _datadogService.ListMetricsAsync(DateTime.UtcNow.AddHours(-1));
                    results["list_metrics"] = new { status = "success", message = $"Found {metrics.Count} metrics", count = metrics.Count };
                }
                catch (Exception ex)
                {
                    results["list_metrics"] = new { status = "error", message = ex.Message, httpStatus = ex.Message.Contains("404") ? "404" : ex.Message.Contains("400") ? "400" : "unknown" };
                }

                // Test 4: Error Tracking (may return 404 if RUM not enabled)
                try
                {
                    var errorIssues = await _datadogService.GetErrorIssuesAsync(null, DateTime.UtcNow.AddHours(-24), DateTime.UtcNow);
                    if (errorIssues?.Status == "not_found")
                    {
                        results["error_tracking"] = new { status = "not_available", message = "RUM error tracking not enabled (404)", note = "This is expected if RUM is not configured" };
                    }
                    else if (errorIssues?.Issues != null)
                    {
                        results["error_tracking"] = new { status = "success", message = $"Found {errorIssues.Issues.Count} error issues", count = errorIssues.Issues.Count };
                    }
                    else
                    {
                        results["error_tracking"] = new { status = "no_data", message = "No error issues returned" };
                    }
                }
                catch (Exception ex)
                {
                    results["error_tracking"] = new { status = "error", message = ex.Message, httpStatus = ex.Message.Contains("404") ? "404" : "unknown" };
                }

                // Test 5: Query Events
                try
                {
                    var events = await _datadogService.QueryEventsAsync("error", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
                    results["query_events"] = new { status = events?.Events != null ? "success" : "no_data", message = events?.Events != null ? $"Found {events.Events.Count} events" : "No events returned", count = events?.Events?.Count ?? 0 };
                }
                catch (Exception ex)
                {
                    results["query_events"] = new { status = "error", message = ex.Message, httpStatus = ex.Message.Contains("404") ? "404" : "unknown" };
                }

                // Test 6: List Monitors
                try
                {
                    var monitors = await _datadogService.ListMonitorsAsync();
                    results["list_monitors"] = new { status = "success", message = $"Found {monitors.Count} monitors", count = monitors.Count };
                }
                catch (Exception ex)
                {
                    results["list_monitors"] = new { status = "error", message = ex.Message, httpStatus = ex.Message.Contains("404") ? "404" : "unknown" };
                }

                // Test 7: List Dashboards
                try
                {
                    var dashboards = await _datadogService.ListDashboardsAsync();
                    results["list_dashboards"] = new { status = "success", message = $"Found {dashboards.Count} dashboards", count = dashboards.Count };
                }
                catch (Exception ex)
                {
                    results["list_dashboards"] = new { status = "error", message = ex.Message, httpStatus = ex.Message.Contains("404") ? "404" : "unknown" };
                }

                stopwatch.Stop();

                var successfulCount = 0;
                var failedCount = 0;
                var notAvailableCount = 0;

                foreach (var result in results.Values)
                {
                    var resultStr = result.ToString() ?? "";
                    if (resultStr.Contains("\"status\":\"success\""))
                        successfulCount++;
                    else if (resultStr.Contains("\"status\":\"error\""))
                        failedCount++;
                    else if (resultStr.Contains("\"status\":\"not_available\""))
                        notAvailableCount++;
                }

                var summary = new
                {
                    totalTests = results.Count,
                    successful = successfulCount,
                    failed = failedCount,
                    notAvailable = notAvailableCount,
                    testDurationMs = stopwatch.ElapsedMilliseconds
                };

                return new OperationResult(
                    success: true,
                    data: new
                    {
                        summary = summary,
                        endpointResults = results,
                        testedAt = DateTime.UtcNow
                    }
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Endpoint testing failed");
                return new OperationResult(
                    success: false,
                    error: $"Endpoint testing failed: {ex.Message}",
                    data: new { endpointResults = results, testDurationMs = stopwatch.ElapsedMilliseconds }
                );
            }
        }

        /// <summary>
        /// Gets account/organization information from Datadog.
        /// </summary>
        // [McpServerTool(
        //     Title = "Datadog: Get Account Info",
        //     ReadOnly = true,
        //     Idempotent = true,
        //     Destructive = false),
        //     Description("Get account/organization information from Datadog")]
        public async Task<OperationResult> DatadogGetAccountInfo()
        {
            try
            {
                _logger.LogInformation("Fetching Datadog account info");

                var accountInfo = await _datadogService.GetAccountInfoAsync();

                if (accountInfo == null)
                {
                    return new OperationResult(
                        success: false,
                        error: "Failed to retrieve account information"
                    );
                }

                return new OperationResult(success: true, data: accountInfo);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Authentication failed");
                return new OperationResult(
                    success: false,
                    error: $"Authentication failed: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get account info");
                return new OperationResult(
                    success: false,
                    error: $"Failed to get account info: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Queries Datadog metrics with time range and filters.
        /// </summary>
        [McpServerTool(
            Title = "Datadog: Query Metrics",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Query Datadog metrics with time range and filters")]
        public async Task<OperationResult> DatadogQueryMetrics(
            [Description("Metric query string (e.g., 'avg:system.cpu.user{*}')")] string query,
            [Description("Start time (ISO 8601 or relative like '1h ago')")] string from,
            [Description("End time (ISO 8601 or relative like 'now')")] string to)
        {
            try
            {
                _logger.LogInformation("Querying metrics: {Query}", query);

                var fromTime = TimeRangeParser.ParseTime(from);
                var toTime = TimeRangeParser.ParseTime(to);

                // Validate time range
                if (fromTime >= toTime)
                {
                    return new OperationResult(
                        success: false,
                        error: "Start time must be before end time"
                    );
                }

                var timeRange = toTime - fromTime;
                if (timeRange.TotalHours > DatadogToolConstants.MaxQueryTimeRangeHours)
                {
                    return new OperationResult(
                        success: false,
                        error: $"Time range exceeds maximum of {DatadogToolConstants.MaxQueryTimeRangeHours} hours"
                    );
                }

                var result = await _datadogService.QueryMetricsAsync(query, fromTime, toTime);

                if (result == null)
                {
                    return new OperationResult(
                        success: false,
                        error: "Failed to query metrics"
                    );
                }

                if (!string.IsNullOrWhiteSpace(result.Error))
                {
                    return new OperationResult(
                        success: false,
                        error: $"Metric query error: {result.Error}"
                    );
                }

                return new OperationResult(success: true, data: result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid time format");
                return new OperationResult(
                    success: false,
                    error: $"Invalid time format: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query metrics");
                return new OperationResult(
                    success: false,
                    error: $"Failed to query metrics: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Lists available metrics in Datadog.
        /// </summary>
        // [McpServerTool(
        //     Title = "Datadog: List Available Metrics",
        //     ReadOnly = true,
        //     Idempotent = true,
        //     Destructive = false),
        //     Description("List available metrics in Datadog, optionally filtered by time")]
        public async Task<OperationResult> DatadogListMetrics(
            [Description("Optional: Start time to filter metrics (ISO 8601 or relative like '1h ago')")] string? from = null)
        {
            try
            {
                _logger.LogInformation("Listing available metrics");

                DateTime? fromTime = null;
                if (!string.IsNullOrWhiteSpace(from))
                {
                    fromTime = TimeRangeParser.ParseTime(from);
                }

                var metrics = await _datadogService.ListMetricsAsync(fromTime);

                return new OperationResult(
                    success: true,
                    data: new
                    {
                        count = metrics.Count,
                        metrics = metrics
                    }
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid time format");
                return new OperationResult(
                    success: false,
                    error: $"Invalid time format: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list metrics");
                return new OperationResult(
                    success: false,
                    error: $"Failed to list metrics: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Gets metadata for a specific metric.
        /// </summary>
        // [McpServerTool(
        //     Title = "Datadog: Get Metric Metadata",
        //     ReadOnly = true,
        //     Idempotent = true,
        //     Destructive = false),
        //     Description("Get metadata for a specific metric including description, unit, type, and tags")]
        public async Task<OperationResult> DatadogGetMetricMetadata(
            [Description("Metric name (e.g., 'system.cpu.user')")] string metricName)
        {
            try
            {
                _logger.LogInformation("Getting metadata for metric: {MetricName}", metricName);

                var metadata = await _datadogService.GetMetricMetadataAsync(metricName);

                if (metadata == null)
                {
                    return new OperationResult(
                        success: false,
                        error: $"Metric '{metricName}' not found or metadata unavailable"
                    );
                }

                return new OperationResult(success: true, data: metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get metric metadata");
                return new OperationResult(
                    success: false,
                    error: $"Failed to get metric metadata: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Queries Datadog logs with filters and time range.
        /// </summary>
        [McpServerTool(
            Title = "Datadog: Query Logs",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Query Datadog logs with filters and time range")]
        public async Task<OperationResult> DatadogQueryLogs(
            [Description("Log query string (e.g., 'service:payment-service status:error')")] string query,
            [Description("Start time (ISO 8601 or relative like '1h ago')")] string from,
            [Description("End time (ISO 8601 or relative like 'now')")] string to,
            [Description("Maximum number of results to return")] int? limit = null)
        {
            try
            {
                _logger.LogInformation("Querying logs: {Query}", query);

                var fromTime = TimeRangeParser.ParseTime(from);
                var toTime = TimeRangeParser.ParseTime(to);

                // Validate time range
                if (fromTime >= toTime)
                {
                    return new OperationResult(
                        success: false,
                        error: "Start time must be before end time"
                    );
                }

                var timeRange = toTime - fromTime;
                if (timeRange.TotalHours > DatadogToolConstants.MaxQueryTimeRangeHours)
                {
                    return new OperationResult(
                        success: false,
                        error: $"Time range exceeds maximum of {DatadogToolConstants.MaxQueryTimeRangeHours} hours"
                    );
                }

                var result = await _datadogService.QueryLogsAsync(query, fromTime, toTime, limit);

                if (result == null)
                {
                    return new OperationResult(
                        success: false,
                        error: "Failed to query logs"
                    );
                }

                if (!string.IsNullOrWhiteSpace(result.Error))
                {
                    return new OperationResult(
                        success: false,
                        error: $"Log query error: {result.Error}"
                    );
                }

                return new OperationResult(success: true, data: result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid time format");
                return new OperationResult(
                    success: false,
                    error: $"Invalid time format: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query logs");
                return new OperationResult(
                    success: false,
                    error: $"Failed to query logs: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Gets distinct values for log attributes (e.g., instance names, client names, event types).
        /// </summary>
        [McpServerTool(
            Title = "Datadog: Get Log Attribute Values",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Get distinct values for specific log attributes like instance names, client names, event types, etc. Supports nested attributes like 'Properties.Details.EventType'")]
        public async Task<OperationResult> DatadogGetLogAttributeValues(
            [Description("Attribute name to get distinct values for (e.g., 'app.system_instance_name', 'app.system_client_name', 'Properties.Details.EventType'). Can include @ prefix which will be stripped.")] string attributeName,
            [Description("Optional: Base query to filter logs before extracting values")] string? baseQuery = null,
            [Description("Start time (ISO 8601 or relative like '7d ago')")] string from = "7d ago",
            [Description("End time (ISO 8601 or relative like 'now')")] string to = "now",
            [Description("Maximum number of log events to analyze (default: 1000)")] int? maxEvents = 1000)
        {
            try
            {
                _logger.LogInformation("Getting distinct values for attribute: {AttributeName}", attributeName);

                var fromTime = TimeRangeParser.ParseTime(from);
                var toTime = TimeRangeParser.ParseTime(to);

                // Validate time range
                if (fromTime >= toTime)
                {
                    return new OperationResult(
                        success: false,
                        error: "Start time must be before end time"
                    );
                }

                var timeRange = toTime - fromTime;
                if (timeRange.TotalHours > DatadogToolConstants.MaxQueryTimeRangeHours)
                {
                    return new OperationResult(
                        success: false,
                        error: $"Time range exceeds maximum of {DatadogToolConstants.MaxQueryTimeRangeHours} hours"
                    );
                }

                // Strip @ prefix if present (Datadog query syntax)
                var cleanAttributeName = attributeName.TrimStart('@');
                
                // Build query - use base query if provided, otherwise query for the attribute
                // Try without @ prefix first, as v2 API may handle it differently
                // If it's a nested attribute, try both with and without @
                var queryAttributeName = cleanAttributeName;
                
                var query = string.IsNullOrWhiteSpace(baseQuery) 
                    ? $"{queryAttributeName}:*" 
                    : $"{baseQuery} {queryAttributeName}:*";

                var result = await _datadogService.QueryLogsAsync(query, fromTime, toTime, maxEvents);

                if (result == null)
                {
                    return new OperationResult(
                        success: false,
                        error: "Failed to query logs"
                    );
                }

                if (!string.IsNullOrWhiteSpace(result.Error))
                {
                    return new OperationResult(
                        success: false,
                        error: $"Log query error: {result.Error}"
                    );
                }

                // Extract distinct values from attributes
                var distinctValues = new HashSet<string>();
                var attributePath = cleanAttributeName.Split('.');

                var eventsAnalyzed = 0;
                var sampleKeys = new HashSet<string>();
                var foundInFirstFew = false;
                
                foreach (var logEvent in result.Events ?? new List<LogEvent>())
                {
                    eventsAnalyzed++;
                    string? value = null;
                    
                    // Collect sample keys from first few events for debugging
                    if (eventsAnalyzed <= 5 && logEvent.Attributes != null)
                    {
                        foreach (var key in logEvent.Attributes.Keys)
                        {
                            sampleKeys.Add(key);
                        }
                    }
                    
                    // Strategy 1: Check if attributes are nested inside an "attributes" key
                    if (logEvent.Attributes != null && logEvent.Attributes.TryGetValue("attributes", out var nestedAttributes))
                    {
                        if (nestedAttributes is Dictionary<string, object> nestedDict)
                        {
                            // Try extracting with the path (e.g., ["app", "system_instance_name"])
                            value = ExtractAttributeValue(nestedDict, attributePath);
                            
                            // Try as direct key in nested dict (e.g., "app.system_instance_name")
                            if (string.IsNullOrWhiteSpace(value) && nestedDict.TryGetValue(cleanAttributeName, out var directValue))
                            {
                                value = directValue?.ToString();
                            }
                            
                            // Try with "attributes." prefix removed (in case path includes it)
                            if (string.IsNullOrWhiteSpace(value) && attributePath.Length > 0 && attributePath[0] == "attributes")
                            {
                                var pathWithoutAttributes = attributePath.Skip(1).ToArray();
                                if (pathWithoutAttributes.Length > 0)
                                {
                                    value = ExtractAttributeValue(nestedDict, pathWithoutAttributes);
                                }
                            }
                            
                            // Try checking if "app" exists as a key and then look inside it
                            if (string.IsNullOrWhiteSpace(value) && attributePath.Length >= 2 && nestedDict.TryGetValue(attributePath[0], out var appValue))
                            {
                                if (appValue is Dictionary<string, object> appDict)
                                {
                                    if (appDict.TryGetValue(attributePath[1], out var systemValue))
                                    {
                                        value = systemValue?.ToString();
                                    }
                                    else
                                    {
                                        // Try case-insensitive match
                                        var key = appDict.Keys.FirstOrDefault(k => 
                                            string.Equals(k, attributePath[1], StringComparison.OrdinalIgnoreCase));
                                        if (key != null)
                                        {
                                            value = appDict[key]?.ToString();
                                        }
                                    }
                                }
                            }
                        }
                        else if (nestedAttributes is JsonElement nestedJson && nestedJson.ValueKind == JsonValueKind.Object)
                        {
                            // Convert JsonElement to dictionary for extraction
                            var tempDict = new Dictionary<string, object>();
                            foreach (var jsonProp in nestedJson.EnumerateObject())
                            {
                                tempDict[jsonProp.Name] = jsonProp.Value;
                            }
                            value = ExtractAttributeValue(tempDict, attributePath);
                            
                            if (string.IsNullOrWhiteSpace(value) && nestedJson.TryGetProperty(cleanAttributeName, out var nestedProp))
                            {
                                value = nestedProp.ValueKind switch
                                {
                                    JsonValueKind.String => nestedProp.GetString(),
                                    JsonValueKind.Number => nestedProp.GetRawText(),
                                    JsonValueKind.True => "true",
                                    JsonValueKind.False => "false",
                                    _ => nestedProp.GetRawText()
                                };
                            }
                            
                            // Try checking if "app" exists as a property and then look inside it
                            if (string.IsNullOrWhiteSpace(value) && attributePath.Length >= 2 && nestedJson.TryGetProperty(attributePath[0], out var appElement))
                            {
                                if (appElement.ValueKind == JsonValueKind.Object && appElement.TryGetProperty(attributePath[1], out var systemElement))
                                {
                                    value = systemElement.ValueKind switch
                                    {
                                        JsonValueKind.String => systemElement.GetString(),
                                        JsonValueKind.Number => systemElement.GetRawText(),
                                        JsonValueKind.True => "true",
                                        JsonValueKind.False => "false",
                                        _ => systemElement.GetRawText()
                                    };
                                }
                            }
                        }
                    }
                    
                    // Strategy 2: Try top-level attributes dictionary
                    if (string.IsNullOrWhiteSpace(value) && logEvent.Attributes != null)
                    {
                        value = ExtractAttributeValue(logEvent.Attributes, attributePath);
                        
                        // Try as direct key
                        if (string.IsNullOrWhiteSpace(value) && logEvent.Attributes.TryGetValue(cleanAttributeName, out var directValue))
                        {
                            value = directValue?.ToString();
                        }
                        
                        // Try case-insensitive single key match
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            var key = logEvent.Attributes.Keys.FirstOrDefault(k => 
                                string.Equals(k, cleanAttributeName, StringComparison.OrdinalIgnoreCase));
                            if (key != null)
                            {
                                value = logEvent.Attributes[key]?.ToString();
                            }
                        }
                    }
                    
                    // Strategy 3: Check tags (Datadog often stores attributes as tags)
                    if (string.IsNullOrWhiteSpace(value) && logEvent.Tags != null)
                    {
                        // Tags are usually in format "key:value" or just "key"
                        var tagKey = cleanAttributeName.Replace(".", "_").Replace("@", "");
                        var matchingTag = logEvent.Tags.Keys.FirstOrDefault(t => 
                            t.Equals(tagKey, StringComparison.OrdinalIgnoreCase) ||
                            t.StartsWith(tagKey + ":", StringComparison.OrdinalIgnoreCase) ||
                            t.Contains(cleanAttributeName, StringComparison.OrdinalIgnoreCase));
                        
                        if (matchingTag != null && logEvent.Tags.TryGetValue(matchingTag, out var tagValue))
                        {
                            // If tag is "key:value", extract value part
                            if (tagValue.Contains(':'))
                            {
                                value = tagValue.Split(':').LastOrDefault()?.Trim();
                            }
                            else
                            {
                                value = tagValue;
                            }
                        }
                    }
                    
                    // Strategy 4: Try partial matches in nested attributes
                    if (string.IsNullOrWhiteSpace(value) && logEvent.Attributes != null)
                    {
                        var partialKey = logEvent.Attributes.Keys.FirstOrDefault(k => 
                            k.Contains("instance_name", StringComparison.OrdinalIgnoreCase) ||
                            k.Contains("system_instance", StringComparison.OrdinalIgnoreCase));
                        if (partialKey != null)
                        {
                            var partialValue = logEvent.Attributes[partialKey];
                            if (partialValue is Dictionary<string, object> partialDict)
                            {
                                if (partialDict.TryGetValue("system_instance_name", out var nestedValue) ||
                                    partialDict.TryGetValue("instance_name", out nestedValue))
                                {
                                    value = nestedValue?.ToString();
                                }
                            }
                            else
                            {
                                value = partialValue?.ToString();
                            }
                        }
                    }
                    
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        distinctValues.Add(value);
                        if (eventsAnalyzed <= 10) foundInFirstFew = true;
                    }
                }

                // If no values found, provide helpful debugging info
                if (distinctValues.Count == 0 && eventsAnalyzed > 0)
                {
                    return new OperationResult(
                        success: true,
                        data: new
                        {
                            attributeName = cleanAttributeName,
                            distinctValues = new List<string>(),
                            count = 0,
                            totalEventsAnalyzed = eventsAnalyzed,
                            query = query,
                            timeRange = new { from = fromTime, to = toTime },
                            message = $"Analyzed {eventsAnalyzed} events but found no values for attribute '{cleanAttributeName}'. The attribute may be stored differently in your logs.",
                            sampleAttributeKeys = sampleKeys.Take(20).OrderBy(k => k).ToList(),
                            suggestions = new
                            {
                                checkAttributeKeys = $"Found {sampleKeys.Count} different attribute keys in sample events. Check if the attribute name matches one of these keys.",
                                tryPartialMatch = "The attribute might be stored with a different name. Check the sampleAttributeKeys above.",
                                tryNestedPath = "If the attribute is nested, try the full path (e.g., 'app.system.instance_name')",
                                tryBroaderQuery = "Use baseQuery: '*' to search all logs and see available attributes"
                            },
                            examples = new
                            {
                                instanceNames = "Use attributeName: 'app.system_instance_name'",
                                clientNames = "Use attributeName: 'app.system_client_name'",
                                subClientNames = "Use attributeName: 'app.system_sub_client_name'",
                                eventTypes = "Use attributeName: 'Properties.Details.EventType' or '@Properties.Details.EventType'"
                            }
                        }
                    );
                }
                
                // If no events found, provide helpful debugging info
                if (eventsAnalyzed == 0)
                {
                    return new OperationResult(
                        success: true,
                        data: new
                        {
                            attributeName = cleanAttributeName,
                            distinctValues = new List<string>(),
                            count = 0,
                            totalEventsAnalyzed = 0,
                            query = query,
                            timeRange = new { from = fromTime, to = toTime },
                            message = "No log events found matching the query. Try using a broader baseQuery or different time range.",
                            suggestions = new
                            {
                                tryWithoutAtPrefix = $"Try: attributeName='{cleanAttributeName.Replace("@", "")}'",
                                tryBroaderQuery = "Use baseQuery: '*' to search all logs",
                                checkTimeRange = $"Current range: {timeRange.TotalHours:F1} hours (max: {DatadogToolConstants.MaxQueryTimeRangeHours} hours)"
                            },
                            examples = new
                            {
                                instanceNames = "Use attributeName: 'app.system_instance_name'",
                                clientNames = "Use attributeName: 'app.system_client_name'",
                                subClientNames = "Use attributeName: 'app.system_sub_client_name'",
                                eventTypes = "Use attributeName: 'Properties.Details.EventType' or '@Properties.Details.EventType'"
                            }
                        }
                    );
                }

                return new OperationResult(
                    success: true,
                    data: new
                    {
                        attributeName = cleanAttributeName,
                        distinctValues = distinctValues.OrderBy(v => v).ToList(),
                        count = distinctValues.Count,
                        totalEventsAnalyzed = eventsAnalyzed,
                        timeRange = new { from = fromTime, to = toTime },
                        examples = new
                        {
                            instanceNames = "Use attributeName: 'app.system_instance_name'",
                            clientNames = "Use attributeName: 'app.system_client_name'",
                            subClientNames = "Use attributeName: 'app.system_sub_client_name'",
                            eventTypes = "Use attributeName: 'Properties.Details.EventType' or '@Properties.Details.EventType'"
                        }
                    }
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid time format");
                return new OperationResult(
                    success: false,
                    error: $"Invalid time format: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get log attribute values");
                return new OperationResult(
                    success: false,
                    error: $"Failed to get log attribute values: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Extracts attribute value from nested dictionary using dot-notation path.
        /// </summary>
        private string? ExtractAttributeValue(Dictionary<string, object> attributes, string[] path)
        {
            if (path.Length == 0) return null;

            object? current = attributes;
            for (int i = 0; i < path.Length; i++)
            {
                if (current == null) return null;

                if (current is Dictionary<string, object> dict)
                {
                    if (!dict.TryGetValue(path[i], out current))
                    {
                        // Try case-insensitive match
                        var key = dict.Keys.FirstOrDefault(k => 
                            string.Equals(k, path[i], StringComparison.OrdinalIgnoreCase));
                        if (key != null)
                        {
                            current = dict[key];
                        }
                        else
                        {
                            return null;
                        }
                    }
                    
                    // If we got a JsonElement, continue with it
                    if (current is JsonElement jsonElement)
                    {
                        current = jsonElement;
                        continue;
                    }
                }
                else if (current is JsonElement jsonElement)
                {
                    if (jsonElement.ValueKind == JsonValueKind.Object)
                    {
                        if (jsonElement.TryGetProperty(path[i], out var prop))
                        {
                            current = prop;
                        }
                        else
                        {
                            // Try case-insensitive match
                            var found = false;
                            foreach (var prop2 in jsonElement.EnumerateObject())
                            {
                                if (string.Equals(prop2.Name, path[i], StringComparison.OrdinalIgnoreCase))
                                {
                                    current = prop2.Value;
                                    found = true;
                                    break;
                                }
                            }
                            if (!found) return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (current is List<object> list && list.Count > 0)
                {
                    // Handle arrays - take first element
                    current = list[0];
                    i--; // Retry current path segment
                    continue;
                }
                else
                {
                    return null;
                }
            }

            // Extract final value
            if (current is JsonElement finalElement)
            {
                return finalElement.ValueKind switch
                {
                    JsonValueKind.String => finalElement.GetString(),
                    JsonValueKind.Number => finalElement.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => finalElement.GetRawText()
                };
            }

            if (current is List<object> finalList && finalList.Count > 0)
            {
                return finalList[0]?.ToString();
            }

            return current?.ToString();
        }

        /// <summary>
        /// Searches logs by text, tags, or attributes.
        /// </summary>
        // [McpServerTool(
        //     Title = "Datadog: Search Logs",
        //     ReadOnly = true,
        //     Idempotent = true,
        //     Destructive = false),
        //     Description("Search logs by text, tags, or attributes with time range")]
        public async Task<OperationResult> DatadogSearchLogs(
            [Description("Search text or query (e.g., 'error', 'service:payment-service status:error')")] string searchText,
            [Description("Start time (ISO 8601 or relative like '1h ago')")] string from,
            [Description("End time (ISO 8601 or relative like 'now')")] string to,
            [Description("Maximum number of results to return")] int? limit = null)
        {
            try
            {
                _logger.LogInformation("Searching logs: {SearchText}", searchText);

                // Use QueryLogs with the search text as the query
                return await DatadogQueryLogs(searchText, from, to, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search logs");
                return new OperationResult(
                    success: false,
                    error: $"Failed to search logs: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Queries Datadog traces with filters and time range.
        /// </summary>
        [McpServerTool(
            Title = "Datadog: Query Traces",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Query Datadog distributed traces with filters and time range")]
        public async Task<OperationResult> DatadogQueryTraces(
            [Description("Trace query string (e.g., 'service:payment-service operation:checkout')")] string query,
            [Description("Start time (ISO 8601 or relative like '1h ago')")] string from,
            [Description("End time (ISO 8601 or relative like 'now')")] string to,
            [Description("Maximum number of traces to return")] int? limit = null)
        {
            try
            {
                _logger.LogInformation("Querying traces: {Query}", query);

                var fromTime = TimeRangeParser.ParseTime(from);
                var toTime = TimeRangeParser.ParseTime(to);

                // Validate time range
                if (fromTime >= toTime)
                {
                    return new OperationResult(
                        success: false,
                        error: "Start time must be before end time"
                    );
                }

                var timeRange = toTime - fromTime;
                if (timeRange.TotalHours > DatadogToolConstants.MaxQueryTimeRangeHours)
                {
                    return new OperationResult(
                        success: false,
                        error: $"Time range exceeds maximum of {DatadogToolConstants.MaxQueryTimeRangeHours} hours"
                    );
                }

                var result = await _datadogService.QueryTracesAsync(query, fromTime, toTime, limit);

                if (result == null)
                {
                    return new OperationResult(
                        success: false,
                        error: "Failed to query traces"
                    );
                }

                // Check if endpoint is not available (404 handled gracefully in service)
                if (result.Status == "not_found")
                {
                    return new OperationResult(
                        success: true,
                        data: new
                        {
                            message = result.Error ?? "Traces endpoint not available",
                            note = "APM (Application Performance Monitoring) may not be enabled in this account, or traces may not be available via API",
                            traces = new List<object>(),
                            status = "not_found"
                        }
                    );
                }

                if (!string.IsNullOrWhiteSpace(result.Error))
                {
                    return new OperationResult(
                        success: false,
                        error: $"Trace query error: {result.Error}"
                    );
                }

                return new OperationResult(success: true, data: result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid time format");
                return new OperationResult(
                    success: false,
                    error: $"Invalid time format: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query traces");
                return new OperationResult(
                    success: false,
                    error: $"Failed to query traces: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Lists monitors in Datadog.
        /// </summary>
        // [McpServerTool(
        //     Title = "Datadog: List Monitors",
        //     ReadOnly = true,
        //     Idempotent = true,
        //     Destructive = false),
        //     Description("List monitors in Datadog, optionally filtered by status")]
        public async Task<OperationResult> DatadogListMonitors(
            [Description("Optional: Filter by status (e.g., 'Alert', 'Warn', 'OK', 'No Data')")] string? status = null)
        {
            try
            {
                _logger.LogInformation("Listing monitors (status: {Status})", status ?? "all");

                var monitors = await _datadogService.ListMonitorsAsync(status);

                return new OperationResult(
                    success: true,
                    data: new
                    {
                        count = monitors.Count,
                        monitors = monitors
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list monitors");
                return new OperationResult(
                    success: false,
                    error: $"Failed to list monitors: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Gets details for a specific monitor.
        /// </summary>
        // [McpServerTool(
        //     Title = "Datadog: Get Monitor",
        //     ReadOnly = true,
        //     Idempotent = true,
        //     Destructive = false),
        //     Description("Get details for a specific monitor by ID")]
        public async Task<OperationResult> DatadogGetMonitor(
            [Description("Monitor ID")] long monitorId)
        {
            try
            {
                _logger.LogInformation("Getting monitor: {MonitorId}", monitorId);

                var monitor = await _datadogService.GetMonitorAsync(monitorId);

                if (monitor == null)
                {
                    return new OperationResult(
                        success: false,
                        error: $"Monitor '{monitorId}' not found"
                    );
                }

                return new OperationResult(success: true, data: monitor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get monitor");
                return new OperationResult(
                    success: false,
                    error: $"Failed to get monitor: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Gets currently active alerts from monitors.
        /// </summary>
        // [McpServerTool(
        //     Title = "Datadog: Get Active Alerts",
        //     ReadOnly = true,
        //     Idempotent = true,
        //     Destructive = false),
        //     Description("Get currently active alerts from monitors")]
        public async Task<OperationResult> DatadogGetActiveAlerts()
        {
            try
            {
                _logger.LogInformation("Getting active alerts");

                var alerts = await _datadogService.GetActiveAlertsAsync();

                return new OperationResult(
                    success: true,
                    data: new
                    {
                        count = alerts.Count,
                        alerts = alerts
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active alerts");
                return new OperationResult(
                    success: false,
                    error: $"Failed to get active alerts: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Gets error tracking issues grouped by similarity.
        /// </summary>
        [McpServerTool(
            Title = "Datadog: Get Error Issues",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Get error tracking issues grouped by similarity, optionally filtered by service and time range")]
        public async Task<OperationResult> DatadogGetErrorIssues(
            [Description("Optional: Service name filter")] string? serviceName = null,
            [Description("Optional: Start time (ISO 8601 or relative like '1h ago')")] string? from = null,
            [Description("Optional: End time (ISO 8601 or relative like 'now')")] string? to = null,
            [Description("Optional: Minimum error count threshold")] int? minCount = null)
        {
            try
            {
                _logger.LogInformation("Getting error issues (service: {ServiceName})", serviceName ?? "all");

                DateTime? fromTime = null;
                DateTime? toTime = null;

                if (!string.IsNullOrWhiteSpace(from))
                {
                    fromTime = TimeRangeParser.ParseTime(from);
                }
                if (!string.IsNullOrWhiteSpace(to))
                {
                    toTime = TimeRangeParser.ParseTime(to);
                }

                var result = await _datadogService.GetErrorIssuesAsync(serviceName, fromTime, toTime, minCount);

                if (result == null)
                {
                    return new OperationResult(
                        success: false,
                        error: "Failed to get error issues"
                    );
                }

                // Check if endpoint is not available (404 handled gracefully in service)
                if (result.Status == "not_found")
                {
                    return new OperationResult(
                        success: true,
                        data: new
                        {
                            message = result.Error ?? "Error tracking endpoint not available",
                            note = "RUM (Real User Monitoring) error tracking may not be enabled in this account",
                            issues = new List<object>(),
                            status = "not_found"
                        }
                    );
                }

                if (!string.IsNullOrWhiteSpace(result.Error) && result.Status != "not_found")
                {
                    return new OperationResult(
                        success: false,
                        error: $"Error tracking query error: {result.Error}"
                    );
                }

                return new OperationResult(success: true, data: result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid time format");
                return new OperationResult(
                    success: false,
                    error: $"Invalid time format: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get error issues");
                return new OperationResult(
                    success: false,
                    error: $"Failed to get error issues: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Gets the latest exception/error from Datadog error tracking or events.
        /// </summary>
        [McpServerTool(
            Title = "Datadog: Get Latest Exception",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Get the most recent exception/error from Datadog error tracking or events, optionally filtered by service")]
        public async Task<OperationResult> DatadogGetLatestException(
            [Description("Optional: Service name filter")] string? serviceName = null,
            [Description("Optional: Time range to search (e.g., '1h', '24h', '7d'). Default: '24h'")] string? timeRange = "24h")
        {
            try
            {
                _logger.LogInformation("Getting latest exception (service: {ServiceName}, timeRange: {TimeRange})", 
                    serviceName ?? "all", timeRange ?? "24h");

                var (from, to) = TimeRangeParser.ParseTimeRange(timeRange ?? "24h");

                // Try error tracking first (RUM error tracking)
                try
                {
                    var result = await _datadogService.GetErrorIssuesAsync(serviceName, from, to, minCount: 1);

                    // Check if endpoint is not available (404 handled gracefully in service)
                    if (result != null && result.Status == "not_found")
                    {
                        _logger.LogInformation("Error tracking endpoint not available (RUM may not be enabled), trying Events API");
                        // Continue to Events API fallback below
                    }
                    else if (result != null && !string.IsNullOrWhiteSpace(result.Error) && result.Status != "not_found")
                    {
                        throw new Exception($"Error tracking query error: {result.Error}");
                    }
                    else if (result?.Issues != null && result.Issues.Any())
                    {
                        // Sort by LastSeen descending to get the most recent
                        var latestException = result.Issues
                            .Where(i => i.LastSeen.HasValue)
                            .OrderByDescending(i => i.LastSeen)
                            .FirstOrDefault();

                        // If no LastSeen, fall back to FirstSeen
                        if (latestException == null)
                        {
                            latestException = result.Issues
                                .Where(i => i.FirstSeen.HasValue)
                                .OrderByDescending(i => i.FirstSeen)
                                .FirstOrDefault();
                        }

                        // If still no timestamp, just take the first one
                        if (latestException == null)
                        {
                            latestException = result.Issues.First();
                        }

                        return new OperationResult(
                            success: true,
                            data: new
                            {
                                source = "error_tracking",
                                latestException = latestException,
                                totalIssuesFound = result.Issues.Count,
                                searchTimeRange = new
                                {
                                    from = from,
                                    to = to
                                }
                            }
                        );
                    }
                }
                catch (HttpRequestException httpEx) when (httpEx.Message.Contains("404"))
                {
                    _logger.LogInformation("Error tracking endpoint not available (RUM may not be enabled), trying Events API");
                }

                // Fallback to Events API to find error events
                var eventsResult = await _datadogService.QueryEventsAsync(
                    query: "error",
                    from: from,
                    to: to);

                if (eventsResult?.Events != null && eventsResult.Events.Any())
                {
                    // Filter for error-type events and sort by date
                    var errorEvents = eventsResult.Events
                        .Where(e => e.AlertType?.Equals("error", StringComparison.OrdinalIgnoreCase) == true ||
                                   e.Title?.Contains("error", StringComparison.OrdinalIgnoreCase) == true ||
                                   e.Text?.Contains("error", StringComparison.OrdinalIgnoreCase) == true ||
                                   e.Text?.Contains("exception", StringComparison.OrdinalIgnoreCase) == true)
                        .OrderByDescending(e => e.DateHappened)
                        .ToList();

                    if (errorEvents.Any())
                    {
                        var latestEvent = errorEvents.First();
                        return new OperationResult(
                            success: true,
                            data: new
                            {
                                source = "events",
                                latestException = new
                                {
                                    id = latestEvent.Id?.ToString(),
                                    title = latestEvent.Title,
                                    text = latestEvent.Text,
                                    alertType = latestEvent.AlertType,
                                    dateHappened = latestEvent.DateHappened,
                                    tags = latestEvent.Tags,
                                    source = latestEvent.Source,
                                    priority = latestEvent.Priority
                                },
                                totalErrorEventsFound = errorEvents.Count,
                                totalEventsFound = eventsResult.Events.Count,
                                searchTimeRange = new
                                {
                                    from = from,
                                    to = to
                                }
                            }
                        );
                    }
                }

                // No errors found
                return new OperationResult(
                    success: true,
                    data: new
                    {
                        message = "No exceptions or error events found in the specified time range",
                        timeRange = $"{from:O} to {to:O}",
                        serviceName = serviceName
                    }
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid time format");
                return new OperationResult(
                    success: false,
                    error: $"Invalid time format: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get latest exception");
                return new OperationResult(
                    success: false,
                    error: $"Failed to get latest exception: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Gets service dependency map.
        /// </summary>
        [McpServerTool(
            Title = "Datadog: Get Service Map",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Get service dependency map showing relationships between services")]
        public async Task<OperationResult> DatadogGetServiceMap(
            [Description("Optional: Service name to focus on")] string? serviceName = null,
            [Description("Optional: Start time (ISO 8601 or relative like '1h ago')")] string? from = null,
            [Description("Optional: End time (ISO 8601 or relative like 'now')")] string? to = null)
        {
            try
            {
                _logger.LogInformation("Getting service map (service: {ServiceName})", serviceName ?? "all");

                DateTime? fromTime = null;
                DateTime? toTime = null;

                if (!string.IsNullOrWhiteSpace(from))
                {
                    fromTime = TimeRangeParser.ParseTime(from);
                }
                if (!string.IsNullOrWhiteSpace(to))
                {
                    toTime = TimeRangeParser.ParseTime(to);
                }

                var serviceMap = await _datadogService.GetServiceMapAsync(serviceName, fromTime, toTime);

                if (serviceMap == null)
                {
                    return new OperationResult(
                        success: false,
                        error: "Failed to get service map"
                    );
                }

                // Check if service map is empty (404 handled gracefully in service)
                if (serviceMap.Nodes.Count == 0 && serviceMap.Edges.Count == 0)
                {
                    return new OperationResult(
                        success: true,
                        data: new
                        {
                            message = "Service map endpoint not available or no service data found",
                            note = "Service map may not be available via API, or APM (Application Performance Monitoring) may not be enabled in this account",
                            nodes = new List<object>(),
                            edges = new List<object>(),
                            status = "not_available"
                        }
                    );
                }

                return new OperationResult(success: true, data: serviceMap);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid time format");
                return new OperationResult(
                    success: false,
                    error: $"Invalid time format: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get service map");
                return new OperationResult(
                    success: false,
                    error: $"Failed to get service map: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Analyzes impact of service issues on dependent services.
        /// </summary>
        // [McpServerTool(
        //     Title = "Datadog: Analyze Service Impact",
        //     ReadOnly = true,
        //     Idempotent = true,
        //     Destructive = false),
        //     Description("Analyze impact of service issues on dependent services using service map")]
        public async Task<OperationResult> DatadogAnalyzeServiceImpact(
            [Description("Service name to analyze")] string serviceName,
            [Description("Optional: Start time (ISO 8601 or relative like '1h ago')")] string? from = null,
            [Description("Optional: End time (ISO 8601 or relative like 'now')")] string? to = null)
        {
            try
            {
                _logger.LogInformation("Analyzing service impact for: {ServiceName}", serviceName);

                DateTime? fromTime = null;
                DateTime? toTime = null;

                if (!string.IsNullOrWhiteSpace(from))
                {
                    fromTime = TimeRangeParser.ParseTime(from);
                }
                if (!string.IsNullOrWhiteSpace(to))
                {
                    toTime = TimeRangeParser.ParseTime(to);
                }

                var serviceMap = await _datadogService.GetServiceMapAsync(serviceName, fromTime, toTime);

                if (serviceMap == null)
                {
                    return new OperationResult(
                        success: false,
                        error: "Failed to get service map for impact analysis"
                    );
                }

                // Analyze impact: find services that depend on the specified service
                var impactedServices = serviceMap.Edges
                    .Where(e => e.From?.Equals(serviceName, StringComparison.OrdinalIgnoreCase) == true)
                    .Select(e => e.To)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .ToList();

                var analysis = new
                {
                    serviceName = serviceName,
                    totalDependencies = serviceMap.Edges.Count(e => e.From?.Equals(serviceName, StringComparison.OrdinalIgnoreCase) == true),
                    impactedServices = impactedServices,
                    serviceHealth = serviceMap.Nodes.FirstOrDefault(n => n.Service?.Equals(serviceName, StringComparison.OrdinalIgnoreCase) == true)?.Health
                };

                return new OperationResult(success: true, data: analysis);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid time format");
                return new OperationResult(
                    success: false,
                    error: $"Invalid time format: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze service impact");
                return new OperationResult(
                    success: false,
                    error: $"Failed to analyze service impact: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Lists available dashboards.
        /// </summary>
        // [McpServerTool(
        //     Title = "Datadog: List Dashboards",
        //     ReadOnly = true,
        //     Idempotent = true,
        //     Destructive = false),
        //     Description("List available dashboards in Datadog")]
        public async Task<OperationResult> DatadogListDashboards()
        {
            try
            {
                _logger.LogInformation("Listing dashboards");

                var dashboards = await _datadogService.ListDashboardsAsync();

                return new OperationResult(
                    success: true,
                    data: new
                    {
                        count = dashboards.Count,
                        dashboards = dashboards
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list dashboards");
                return new OperationResult(
                    success: false,
                    error: $"Failed to list dashboards: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Gets dashboard details by ID.
        /// </summary>
        // [McpServerTool(
        //     Title = "Datadog: Get Dashboard",
        //     ReadOnly = true,
        //     Idempotent = true,
        //     Destructive = false),
        //     Description("Get dashboard details including widgets and configuration")]
        public async Task<OperationResult> DatadogGetDashboard(
            [Description("Dashboard ID")] string dashboardId)
        {
            try
            {
                _logger.LogInformation("Getting dashboard: {DashboardId}", dashboardId);

                var dashboard = await _datadogService.GetDashboardAsync(dashboardId);

                if (dashboard == null)
                {
                    return new OperationResult(
                        success: false,
                        error: $"Dashboard '{dashboardId}' not found"
                    );
                }

                return new OperationResult(success: true, data: dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get dashboard");
                return new OperationResult(
                    success: false,
                    error: $"Failed to get dashboard: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Queries Datadog events.
        /// </summary>
        // [McpServerTool(
        //     Title = "Datadog: Query Events",
        //     ReadOnly = true,
        //     Idempotent = true,
        //     Destructive = false),
        //     Description("Query Datadog events with optional filters and time range")]
        public async Task<OperationResult> DatadogQueryEvents(
            [Description("Optional: Search query")] string? query = null,
            [Description("Optional: Start time (ISO 8601 or relative like '1h ago')")] string? from = null,
            [Description("Optional: End time (ISO 8601 or relative like 'now')")] string? to = null,
            [Description("Optional: Maximum number of results")] int? limit = null)
        {
            try
            {
                _logger.LogInformation("Querying events: {Query}", query ?? "all");

                DateTime? fromTime = null;
                DateTime? toTime = null;

                if (!string.IsNullOrWhiteSpace(from))
                {
                    fromTime = TimeRangeParser.ParseTime(from);
                }
                if (!string.IsNullOrWhiteSpace(to))
                {
                    toTime = TimeRangeParser.ParseTime(to);
                }

                var result = await _datadogService.QueryEventsAsync(query, fromTime, toTime, limit);

                if (result == null)
                {
                    return new OperationResult(
                        success: false,
                        error: "Failed to query events"
                    );
                }

                if (!string.IsNullOrWhiteSpace(result.Error))
                {
                    return new OperationResult(
                        success: false,
                        error: $"Event query error: {result.Error}"
                    );
                }

                return new OperationResult(success: true, data: result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid time format");
                return new OperationResult(
                    success: false,
                    error: $"Invalid time format: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query events");
                return new OperationResult(
                    success: false,
                    error: $"Failed to query events: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Creates a custom event in Datadog.
        /// </summary>
        // [McpServerTool(
        //     Title = "Datadog: Create Event",
        //     ReadOnly = false,
        //     Idempotent = false,
        //     Destructive = false),
        //     Description("Create a custom event in Datadog")]
        public async Task<OperationResult> DatadogCreateEvent(
            [Description("Event title")] string title,
            [Description("Event text/description")] string text,
            [Description("Optional: Alert type (info, warning, error, success)")] string? alertType = null,
            [Description("Optional: Priority (normal, low)")] string? priority = null,
            [Description("Optional: Tags as comma-separated list")] string? tags = null)
        {
            try
            {
                _logger.LogInformation("Creating event: {Title}", title);

                List<string>? tagList = null;
                if (!string.IsNullOrWhiteSpace(tags))
                {
                    tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();
                }

                var evt = await _datadogService.CreateEventAsync(title, text, alertType, priority, tagList);

                if (evt == null)
                {
                    return new OperationResult(
                        success: false,
                        error: "Failed to create event"
                    );
                }

                return new OperationResult(success: true, data: evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create event");
                return new OperationResult(
                    success: false,
                    error: $"Failed to create event: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Comprehensive troubleshooting for a reported issue.
        /// </summary>
        [McpServerTool(
            Title = "Datadog: Troubleshoot Issue",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Comprehensive troubleshooting analysis for a reported issue with actionable recommendations")]
        public async Task<OperationResult> DatadogTroubleshootIssue(
            [Description("Issue description or error message")] string issueDescription,
            [Description("Optional: Service name")] string? serviceName = null,
            [Description("Time range to analyze (e.g., '1h', '24h')")] string timeRange = "1h",
            [Description("Optional: Comma-separated troubleshooters to include (metrics,logs,traces,errors,servicemap). Default: all")] string? includeTroubleshooters = null)
        {
            try
            {
                _logger.LogInformation("Troubleshooting issue: {IssueDescription}", issueDescription);

                var (from, to) = TimeRangeParser.ParseTimeRange(timeRange);
                var context = new TroubleshootingContext
                {
                    IssueDescription = issueDescription,
                    ServiceName = serviceName,
                    From = from,
                    To = to
                };

                // Select troubleshooters
                var selectedTroubleshooters = SelectTroubleshooters(includeTroubleshooters, issueDescription);
                if (!selectedTroubleshooters.Any())
                {
                    return new OperationResult(
                        success: false,
                        error: "No valid troubleshooters selected. Available: metrics, logs, traces, errors, servicemap"
                    );
                }

                var allRecommendations = new List<TroubleshootingRecommendation>();
                var analysisData = new Dictionary<string, object>();

                // Run selected troubleshooters
                foreach (var troubleshooter in selectedTroubleshooters)
                {
                    try
                    {
                        var recommendations = await troubleshooter.AnalyzeAsync(context);
                        allRecommendations.AddRange(recommendations);
                        analysisData[$"{troubleshooter.TroubleshooterName}Analyzed"] = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Troubleshooter {TroubleshooterName} failed", troubleshooter.TroubleshooterName);
                        analysisData[$"{troubleshooter.TroubleshooterName}Error"] = ex.Message;
                    }
                }

                // Sort recommendations by severity and confidence
                allRecommendations = allRecommendations
                    .OrderByDescending(r => r.Severity == "Critical" ? 3 : r.Severity == "Warning" ? 2 : 1)
                    .ThenByDescending(r => r.Confidence ?? 0)
                    .ToList();

                var result = new TroubleshootingResult
                {
                    IssueDescription = issueDescription,
                    Recommendations = allRecommendations,
                    AnalysisData = analysisData,
                    AnalyzedAt = DateTime.UtcNow
                };

                return new OperationResult(success: true, data: result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid time format");
                return new OperationResult(
                    success: false,
                    error: $"Invalid time format: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to troubleshoot issue");
                return new OperationResult(
                    success: false,
                    error: $"Failed to troubleshoot issue: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Performs root cause analysis for an issue.
        /// </summary>
        [McpServerTool(
            Title = "Datadog: Get Root Cause Analysis",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Perform root cause analysis for an issue by correlating metrics, logs, traces, and errors")]
        public async Task<OperationResult> DatadogGetRootCauseAnalysis(
            [Description("Issue description or error message")] string issueDescription,
            [Description("Optional: Service name")] string? serviceName = null,
            [Description("Time range to analyze (e.g., '1h', '24h')")] string timeRange = "1h")
        {
            try
            {
                _logger.LogInformation("Performing root cause analysis: {IssueDescription}", issueDescription);

                var (from, to) = TimeRangeParser.ParseTimeRange(timeRange);
                
                // Get comprehensive data
                var correlationData = new Dictionary<string, object>();

                // Get error issues
                if (!string.IsNullOrWhiteSpace(serviceName))
                {
                    var errorIssues = await _datadogService.GetErrorIssuesAsync(serviceName, from, to);
                    if (errorIssues?.Issues != null && errorIssues.Issues.Any())
                    {
                        correlationData["errorIssues"] = errorIssues.Issues.Take(5).ToList();
                    }
                }

                // Get service map
                var serviceMap = await _datadogService.GetServiceMapAsync(serviceName, from, to);
                if (serviceMap != null)
                {
                    correlationData["serviceMap"] = new
                    {
                        nodeCount = serviceMap.Nodes.Count,
                        edgeCount = serviceMap.Edges.Count,
                        unhealthyServices = serviceMap.Nodes.Count(n => n.Health?.Status == "error")
                    };
                }

                // Get active alerts
                var alerts = await _datadogService.GetActiveAlertsAsync();
                if (alerts.Any())
                {
                    correlationData["activeAlerts"] = alerts.Take(10).ToList();
                }

                // Run troubleshooting
                var troubleshootResult = await DatadogTroubleshootIssue(issueDescription, serviceName, timeRange);
                if (troubleshootResult.Success && troubleshootResult.Data is TroubleshootingResult tr)
                {
                    correlationData["troubleshootingRecommendations"] = tr.Recommendations;
                }

                // Identify most likely root cause
                var rootCauseAnalysis = new
                {
                    issueDescription = issueDescription,
                    serviceName = serviceName,
                    timeRange = new { from, to },
                    correlationData = correlationData,
                    likelyRootCauses = IdentifyRootCauses(correlationData),
                    analyzedAt = DateTime.UtcNow
                };

                return new OperationResult(success: true, data: rootCauseAnalysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform root cause analysis");
                return new OperationResult(
                    success: false,
                    error: $"Failed to perform root cause analysis: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Recommends fixes based on troubleshooting analysis.
        /// </summary>
        [McpServerTool(
            Title = "Datadog: Recommend Fixes",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Generate prioritized fix recommendations based on troubleshooting analysis")]
        public async Task<OperationResult> DatadogRecommendFixes(
            [Description("Issue description or error message")] string issueDescription,
            [Description("Optional: Service name")] string? serviceName = null,
            [Description("Time range to analyze (e.g., '1h', '24h')")] string timeRange = "1h")
        {
            try
            {
                _logger.LogInformation("Generating fix recommendations: {IssueDescription}", issueDescription);

                // Run troubleshooting first
                var troubleshootResult = await DatadogTroubleshootIssue(issueDescription, serviceName, timeRange);
                
                if (!troubleshootResult.Success || troubleshootResult.Data is not TroubleshootingResult tr)
                {
                    return troubleshootResult;
                }

                // Prioritize and format recommendations
                var prioritizedFixes = tr.Recommendations
                    .Select((r, index) => new
                    {
                        priority = index + 1,
                        severity = r.Severity,
                        title = r.Title,
                        description = r.Description,
                        category = r.Category,
                        steps = r.Steps,
                        confidence = r.Confidence,
                        estimatedImpact = EstimateImpact(r),
                        estimatedEffort = EstimateEffort(r)
                    })
                    .OrderByDescending(f => f.severity == "Critical" ? 3 : f.severity == "Warning" ? 2 : 1)
                    .ThenByDescending(f => f.confidence)
                    .ToList();

                var recommendations = new
                {
                    issueDescription = issueDescription,
                    totalRecommendations = prioritizedFixes.Count,
                    criticalFixes = prioritizedFixes.Where(f => f.severity == "Critical").ToList(),
                    warningFixes = prioritizedFixes.Where(f => f.severity == "Warning").ToList(),
                    infoFixes = prioritizedFixes.Where(f => f.severity == "Info").ToList(),
                    prioritizedFixes = prioritizedFixes,
                    generatedAt = DateTime.UtcNow
                };

                return new OperationResult(success: true, data: recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate fix recommendations");
                return new OperationResult(
                    success: false,
                    error: $"Failed to generate fix recommendations: {ex.Message}"
                );
            }
        }

        private List<ITroubleshooter> SelectTroubleshooters(string? includeTroubleshooters, string issueDescription)
        {
            if (string.IsNullOrWhiteSpace(includeTroubleshooters))
            {
                // Auto-select based on issue description
                var issueLower = issueDescription.ToLowerInvariant();
                var selected = _troubleshooters
                    .Where(t => t.SupportsIssueType(DetectIssueType(issueLower)))
                    .ToList();

                // If no auto-selection, use all
                return selected.Any() ? selected : _troubleshooters.ToList();
            }

            var requested = includeTroubleshooters
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant())
                .ToHashSet();

            return _troubleshooters
                .Where(t => requested.Contains(t.TroubleshooterName.ToLowerInvariant()))
                .ToList();
        }

        private string DetectIssueType(string issueDescription)
        {
            if (issueDescription.Contains("error") || issueDescription.Contains("exception"))
                return "errors";
            if (issueDescription.Contains("slow") || issueDescription.Contains("latency") || issueDescription.Contains("performance"))
                return "performance";
            if (issueDescription.Contains("log"))
                return "logs";
            if (issueDescription.Contains("trace") || issueDescription.Contains("apm"))
                return "traces";
            if (issueDescription.Contains("dependency") || issueDescription.Contains("service"))
                return "dependencies";
            return "metrics";
        }

        private List<string> IdentifyRootCauses(Dictionary<string, object> correlationData)
        {
            var rootCauses = new List<string>();

            if (correlationData.ContainsKey("errorIssues") && 
                correlationData["errorIssues"] is List<ErrorIssue> errors && 
                errors.Any())
            {
                rootCauses.Add($"High error volume: {errors.Count} distinct error issues detected");
            }

            if (correlationData.ContainsKey("activeAlerts") &&
                correlationData["activeAlerts"] is List<Alert> alerts &&
                alerts.Any())
            {
                rootCauses.Add($"Active alerts: {alerts.Count} monitors in alert state");
            }

            if (correlationData.ContainsKey("serviceMap") &&
                correlationData["serviceMap"] is { } sm &&
                sm.GetType().GetProperty("unhealthyServices")?.GetValue(sm) is int unhealthy &&
                unhealthy > 0)
            {
                rootCauses.Add($"Unhealthy services: {unhealthy} service(s) showing health issues");
            }

            return rootCauses;
        }

        private string EstimateImpact(TroubleshootingRecommendation recommendation)
        {
            return recommendation.Severity switch
            {
                "Critical" => "High - Immediate action required",
                "Warning" => "Medium - Should be addressed soon",
                _ => "Low - Consider for future improvement"
            };
        }

        private string EstimateEffort(TroubleshootingRecommendation recommendation)
        {
            // Simple heuristic based on category
            return recommendation.Category switch
            {
                "Configuration" => "Low - Configuration change",
                "Architecture" => "High - Requires architectural changes",
                "Performance" => "Medium - Code optimization needed",
                "Error" => "Medium - Code fix required",
                _ => "Medium - Requires investigation"
            };
        }
    }
}

