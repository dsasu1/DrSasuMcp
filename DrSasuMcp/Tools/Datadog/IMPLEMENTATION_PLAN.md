# Datadog Tool Implementation Plan

## Overview
This document outlines the plan for implementing a comprehensive Datadog tool type that enables effective troubleshooting, issue detection, and fix recommendations through the MCP server.

## Architecture

The Datadog tool will follow the same architectural patterns as existing tools (API, AzureDevOps, SQL):

```
DrSasuMcp/Tools/Datadog/
├── DatadogTool.cs                    # Main tool class with [McpServerToolType]
├── DatadogToolConstants.cs          # Constants and configuration
├── IDatadogService.cs                # Service interface
├── DatadogService.cs                 # Datadog API client implementation
├── Troubleshooters/                  # Issue analysis and recommendations
│   ├── ITroubleshooter.cs
│   ├── MetricsTroubleshooter.cs
│   ├── LogsTroubleshooter.cs
│   ├── TracesTroubleshooter.cs
│   ├── ErrorTrackingTroubleshooter.cs
│   └── ServiceMapTroubleshooter.cs
├── Models/                           # Data models
│   ├── MetricQuery.cs
│   ├── MetricResult.cs
│   ├── LogQuery.cs
│   ├── LogResult.cs
│   ├── TraceQuery.cs
│   ├── TraceResult.cs
│   ├── ErrorIssue.cs
│   ├── ServiceMapNode.cs
│   ├── ServiceMapEdge.cs
│   ├── TroubleshootingRecommendation.cs
│   ├── AlertStatus.cs
│   ├── Monitor.cs
│   └── Dashboard.cs
├── Utils/                            # Utility classes
│   ├── QueryBuilder.cs              # Build Datadog queries
│   └── TimeRangeParser.cs           # Parse time ranges
├── README.md                         # Complete documentation
└── QUICKSTART.md                     # Quick setup guide
```

## Required Tools List

### 1. Connection & Authentication Tools
- **Test Connection**: Verify Datadog API credentials and connectivity
- **Get Account Info**: Retrieve account/organization information

### 2. Metrics Tools
- **Query Metrics**: Execute metric queries with time ranges
- **List Available Metrics**: Get list of available metric names
- **Get Metric Metadata**: Get details about a specific metric
- **Compare Metrics**: Compare metrics across time periods or services
- **Analyze Metric Anomalies**: Detect and analyze metric anomalies

### 3. Logs Tools
- **Query Logs**: Execute log queries with filters
- **Search Logs**: Search logs by text, tags, or attributes
- **Get Log Context**: Get surrounding log context for a specific log entry
- **Analyze Log Patterns**: Identify patterns and trends in logs
- **Get Log Statistics**: Get aggregated statistics from logs

### 4. Traces & APM Tools
- **Query Traces**: Query distributed traces
- **Get Trace Details**: Get detailed information about a specific trace
- **Analyze Trace Performance**: Analyze performance bottlenecks in traces
- **Get Service Dependencies**: Get service dependency map
- **Analyze Service Health**: Analyze health of specific services

### 5. Error Tracking Tools
- **Get Error Issues**: Retrieve error issues grouped by similarity
- **Get Error Details**: Get detailed information about a specific error
- **Get Error Trends**: Analyze error trends over time
- **Get Error Context**: Get context around errors (logs, traces, metrics)
- **Recommend Error Fixes**: AI-powered recommendations for fixing errors

### 6. Service Map Tools
- **Get Service Map**: Retrieve service dependency map
- **Analyze Service Impact**: Analyze impact of service issues
- **Get Service Health**: Get health status of services
- **Trace Service Dependencies**: Trace dependencies for a service

### 7. Monitors & Alerts Tools
- **List Monitors**: List all monitors
- **Get Monitor Status**: Get current status of monitors
- **Get Monitor History**: Get alert history for a monitor
- **Get Active Alerts**: Get currently active alerts
- **Analyze Alert Patterns**: Analyze patterns in alerts

### 8. Dashboards Tools
- **List Dashboards**: List available dashboards
- **Get Dashboard**: Get dashboard configuration and widgets
- **Query Dashboard Data**: Query data for dashboard widgets

### 9. Troubleshooting Tools (High-Level)
- **Troubleshoot Issue**: Comprehensive troubleshooting for a reported issue
- **Analyze Performance Degradation**: Analyze performance issues
- **Investigate Error Spike**: Investigate sudden error increases
- **Recommend Fixes**: Generate actionable fix recommendations
- **Get Root Cause Analysis**: Perform root cause analysis

### 10. Events Tools
- **Query Events**: Query Datadog events
- **Create Event**: Create a custom event
- **Get Event Details**: Get details about a specific event

## Implementation Details

### Authentication
- **API Key**: `DD_API_KEY` environment variable
- **Application Key**: `DD_APP_KEY` environment variable (for admin operations)
- **Site**: `DD_SITE` environment variable (default: `datadoghq.com`)

### Base API URLs
- **US**: `https://api.datadoghq.com`
- **EU**: `https://api.datadoghq.eu`
- **US3**: `https://us3.datadoghq.com`
- **US5**: `https://us5.datadoghq.com`
- **AP1**: `https://ap1.datadoghq.com`

### Key API Endpoints
1. **Metrics**: `/api/v1/query`, `/api/v1/metrics`
2. **Logs**: `/api/v1/logs-queries/list`, `/api/v2/logs/events/search`
3. **Traces**: `/api/v0.2/traces`, `/api/v1/trace`
4. **Errors**: `/api/v1/rum/error-tracking/events`
5. **Monitors**: `/api/v1/monitor`
6. **Dashboards**: `/api/v1/dashboard`
7. **Service Map**: `/api/v1/service_map`

### Data Models

#### MetricQuery
```csharp
public class MetricQuery
{
    public string Query { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string? Aggregation { get; set; }
    public int? Limit { get; set; }
}
```

#### TroubleshootingRecommendation
```csharp
public class TroubleshootingRecommendation
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Severity { get; set; } // Critical, Warning, Info
    public string Category { get; set; } // Performance, Error, Configuration, etc.
    public List<string> Steps { get; set; }
    public Dictionary<string, string> RelatedMetrics { get; set; }
    public Dictionary<string, string> RelatedLogs { get; set; }
}
```

## Troubleshooter Pattern

Similar to Azure DevOps analyzers, troubleshooters will analyze data and provide recommendations:

```csharp
public interface ITroubleshooter
{
    string TroubleshooterName { get; }
    bool SupportsIssueType(string issueType);
    Task<List<TroubleshootingRecommendation>> AnalyzeAsync(
        TroubleshootingContext context);
}
```

## Configuration

### Environment Variables
- `DD_API_KEY`: Datadog API key (required)
- `DD_APP_KEY`: Datadog Application key (optional, for admin ops)
- `DD_SITE`: Datadog site (default: `datadoghq.com`)
- `DD_TIMEOUT_SECONDS`: Request timeout (default: 60)
- `DD_MAX_RESULTS`: Maximum results per query (default: 1000)

## Error Handling

- Handle rate limiting (429 responses)
- Handle authentication errors (403)
- Handle invalid queries (400)
- Provide helpful error messages with suggestions

## Logging

- Log all API requests (with sanitized credentials)
- Log query performance
- Log troubleshooting analysis results

## Testing Strategy

1. **Unit Tests**: Test query builders, parsers, troubleshooters
2. **Integration Tests**: Test against Datadog API (with test credentials)
3. **Mock Tests**: Test error handling and edge cases

## Documentation

1. **README.md**: Complete documentation with examples
2. **QUICKSTART.md**: 5-minute setup guide
3. **API Reference**: Document all tool methods
4. **Troubleshooting Guide**: Guide for using troubleshooting features

## Implementation Phases

### Phase 1: Core Infrastructure
- [ ] Create folder structure
- [ ] Implement DatadogService with authentication
- [ ] Implement basic models
- [ ] Create constants file
- [ ] Implement connection test tool

### Phase 2: Metrics & Logs
- [ ] Implement metrics query tools
- [ ] Implement logs query tools
- [ ] Create MetricsTroubleshooter
- [ ] Create LogsTroubleshooter

### Phase 3: Traces & APM
- [ ] Implement trace query tools
- [ ] Implement service map tools
- [ ] Create TracesTroubleshooter
- [ ] Create ServiceMapTroubleshooter

### Phase 4: Error Tracking
- [ ] Implement error tracking tools
- [ ] Create ErrorTrackingTroubleshooter
- [ ] Implement error context retrieval

### Phase 5: Monitors & Dashboards
- [ ] Implement monitor tools
- [ ] Implement dashboard tools
- [ ] Implement alert analysis

### Phase 6: High-Level Troubleshooting
- [ ] Implement comprehensive troubleshooting tool
- [ ] Implement root cause analysis
- [ ] Implement fix recommendations

### Phase 7: Documentation & Testing
- [ ] Write comprehensive documentation
- [ ] Create quick start guide
- [ ] Write unit tests
- [ ] Write integration tests

## Example Tool Method Signatures

```csharp
[McpServerTool(
    Title = "Datadog: Query Metrics",
    ReadOnly = true,
    Idempotent = true,
    Destructive = false),
    Description("Query Datadog metrics with time range and filters")]
public async Task<OperationResult> DatadogQueryMetrics(
    [Description("Metric query string (e.g., 'avg:system.cpu.user{*}')")] string query,
    [Description("Start time (ISO 8601 or relative like '1h ago')")] string from,
    [Description("End time (ISO 8601 or relative like 'now')")] string to,
    [Description("Aggregation method (avg, sum, max, min)")] string? aggregation = null)

[McpServerTool(
    Title = "Datadog: Troubleshoot Issue",
    ReadOnly = true,
    Idempotent = true,
    Destructive = false),
    Description("Comprehensive troubleshooting analysis for a reported issue")]
public async Task<OperationResult> DatadogTroubleshootIssue(
    [Description("Issue description or error message")] string issueDescription,
    [Description("Service name (optional)")] string? serviceName = null,
    [Description("Time range to analyze (e.g., '1h', '24h')")] string timeRange = "1h",
    [Description("Include troubleshooters (comma-separated: metrics,logs,traces,errors)")] string? includeTroubleshooters = null)

[McpServerTool(
    Title = "Datadog: Get Error Issues",
    ReadOnly = true,
    Idempotent = true,
    Destructive = false),
    Description("Retrieve error issues grouped by similarity")]
public async Task<OperationResult> DatadogGetErrorIssues(
    [Description("Service name filter (optional)")] string? serviceName = null,
    [Description("Start time (ISO 8601 or relative)")] string? from = null,
    [Description("End time (ISO 8601 or relative)")] string? to = null,
    [Description("Minimum error count threshold")] int? minCount = null)
```

## Success Criteria

1. ✅ All core tools implemented and functional
2. ✅ Comprehensive troubleshooting capabilities
3. ✅ Actionable fix recommendations
4. ✅ Well-documented with examples
5. ✅ Follows existing code patterns
6. ✅ Proper error handling and logging
7. ✅ Integration tests passing

## Next Steps

1. Review and approve this plan
2. Set up Datadog test account/API keys
3. Begin Phase 1 implementation
4. Iterate through phases with testing

