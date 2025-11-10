# DrSasuMcp 🚀

**DrSasuMcp** is a powerful [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server that extends AI assistants with database management and API testing capabilities. Built with .NET 8, it provides seamless integration between AI-powered workflows and enterprise systems.

[![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![MCP Protocol](https://img.shields.io/badge/MCP-v0.4.0-green.svg)](https://modelcontextprotocol.io/)

## 🌟 Overview

DrSasuMcp brings **SQL Server database management**, **HTTP API testing**, **Azure DevOps PR review**, and **Datadog monitoring & troubleshooting** directly into your AI assistant conversations. Execute queries, manage schemas, test APIs, review pull requests, troubleshoot issues, and monitor systems—all through natural language commands.

### Why DrSasuMcp?

- 🤖 **AI-Native**: Designed specifically for AI assistant integration via MCP
- 🛠️ **Production-Ready**: Comprehensive error handling, logging, and safety features
- 🔌 **Extensible**: Clean architecture for adding new tools and capabilities
- 🎯 **Type-Safe**: Built with C# for strong typing and reliability
- ⚡ **Async First**: Non-blocking operations for optimal performance

---

## 📦 Features

### 🗄️ SQL Database Tool

Complete SQL Server database management and querying capabilities:

- **Schema Exploration**
  - List all database tables
  - Describe table structures with columns, indexes, constraints, and foreign keys
  - Extended properties and metadata
  
- **Data Operations**
  - Execute SELECT queries
  - Insert, update, and delete records
  - Create and drop tables
  - Transaction support

- **Safety Features**
  - Read-only operations marked appropriately
  - Destructive operations flagged for safety
  - Connection pooling and async operations
  - Comprehensive error messages

[📖 SQL Tool Documentation](DrSasuMcp/Tools/SQL/README.md)

### 🌐 API Testing Tool

Full-featured HTTP API testing—a Postman alternative in your AI workflow:

- **HTTP Methods**
  - GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS
  - Custom headers and query parameters
  - Request body support with multiple content types
  
- **Authentication**
  - Bearer Token (OAuth 2.0, JWT)
  - Basic Authentication
  - API Key (custom headers)
  - Custom authentication headers
  
- **Response Validation**
  - Status code assertions
  - Header validation
  - JSONPath extraction and validation
  - Response time performance checks
  - Body content matching (text, regex)
  
- **Testing Features**
  - Single endpoint tests with multiple validations
  - Test suites for comprehensive API testing
  - Detailed test reports with pass/fail metrics
  - Performance timing and diagnostics

[📖 API Tool Documentation](DrSasuMcp/Tools/API/README.md)

### 🔍 Azure DevOps PR Review Tool

Automated code review for Azure DevOps Pull Requests with security, quality, and best practice analysis:

- **Pull Request Analysis**
  - Fetch PR metadata, file changes, and diffs
  - Support for all Azure DevOps PR URLs
  - Line-by-line change tracking with DiffPlex integration
  
- **Security Analysis (10 checks)**
  - Hardcoded credentials detection (passwords, API keys, tokens)
  - SQL injection vulnerability detection
  - Weak cryptography usage (MD5, SHA1)
  - XSS vulnerabilities and dangerous patterns
  - Process execution and path traversal checks
  
- **Code Quality Analysis (8 checks)**
  - File and method length validation
  - Cyclomatic complexity detection
  - Magic number identification
  - Naming convention validation
  - TODO/FIXME comment tracking
  
- **Best Practices Analysis (13 checks)**
  - Empty catch block detection
  - Async/await pattern validation
  - Resource management (IDisposable, using statements)
  - Exception handling best practices
  - HttpClient instantiation patterns
  
- **Multiple Diff Formats**
  - Unified diff (traditional patch format)
  - Side-by-side diff (visual comparison)
  - Inline diff with change statistics

[📖 Azure DevOps Tool Documentation](DrSasuMcp/Tools/AzureDevOps/README.md) | [🚀 Quick Start Guide](DrSasuMcp/Tools/AzureDevOps/QUICKSTART.md)

### 📊 Datadog Monitoring & Troubleshooting Tool

Comprehensive Datadog integration for monitoring, troubleshooting, and issue resolution:

- **Connection & Authentication**
  - Test Datadog API connectivity
  - Get account/organization information
  
- **Metrics Analysis**
  - Query metrics with time ranges and filters
  - List available metrics and get metadata
  - Analyze metric anomalies and trends
  
- **Logs Analysis**
  - Query and search logs with advanced filters
  - Analyze log patterns and statistics
  - Get log context for troubleshooting
  
- **Traces & APM**
  - Query distributed traces
  - Analyze trace performance and latency
  - Identify bottlenecks in service calls
  
- **Error Tracking**
  - Get error issues grouped by similarity
  - Track error trends and patterns
  - Analyze error context and correlations
  
- **Service Map**
  - Visualize service dependencies
  - Analyze service health and impact
  - Identify cascading failure risks
  
- **Monitors & Alerts**
  - List and query monitors
  - Get active alerts and monitor status
  - Analyze alert patterns
  
- **Dashboards**
  - List available dashboards
  - Get dashboard configurations and widgets
  
- **Events**
  - Query and create custom events
  - Track system events and changes
  
- **Advanced Troubleshooting**
  - Comprehensive issue analysis with multiple troubleshooters
  - Root cause analysis with data correlation
  - Prioritized fix recommendations with impact/effort estimates
  - Intelligent troubleshooter selection based on issue description

[📖 Datadog Tool Documentation](DrSasuMcp/Tools/Datadog/README.md) | [📋 Implementation Plan](DrSasuMcp/Tools/Datadog/IMPLEMENTATION_PLAN.md)

---

## 🚀 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- SQL Server (for SQL tool features)
- Azure DevOps account with Personal Access Token (for Azure DevOps tool features)
- Datadog account with API Key (for Datadog tool features)
- MCP-compatible AI assistant (Claude Desktop, VS Code with MCP, etc.)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/DrSasuMcp.git
   cd DrSasuMcp
   ```

2. **Build the project**
   ```bash
   cd DrSasuMcp
   dotnet build
   ```

3. **Run the server**
   ```bash
   dotnet run
   ```

### Configuration

#### For Claude Desktop

Add to your Claude Desktop configuration file:

**Windows:** `%APPDATA%\Claude\claude_desktop_config.json`
**Mac:** `~/Library/Application Support/Claude/claude_desktop_config.json`

##### Option 1: Using `dotnet run` (Development)

```json
{
  "mcpServers": {
    "drsasumcp": {
     "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Projects\\personal\\DrSasuMcp\\DrSasuMcp\\DrSasuMcp.csproj"],
      "env": {
        "SQL_CONNECTION_STRING": "Server=.;Database=YourDatabase;Trusted_Connection=True;TrustServerCertificate=True",
        "API_DEFAULT_TIMEOUT": "30",
        "API_MAX_TIMEOUT": "300",
        "API_FOLLOW_REDIRECTS": "true",
        "API_VALIDATE_SSL": "true",
        "API_MAX_REDIRECTS": "10",
        "AZURE_DEVOPS_PAT": "your_azure_devops_personal_access_token",
        "DD_API_KEY": "your_datadog_api_key",
        "DD_APP_KEY": "your_datadog_application_key",
        "DD_SITE": "datadoghq.com"
      }
    }
  }
}
```

##### Option 2: Using Compiled Executable (Production)

First, publish the project:
```bash
dotnet publish -c Release -o ./publish
```

Then configure Claude Desktop:

**Windows:**
```json
{
  "mcpServers": {
    "drsasumcp": {
      "command": "C:\\Projects\\personal\\DrSasuMcp\\publish\\DrSasuMcp.exe",
      "env": {
        "SQL_CONNECTION_STRING": "Server=.;Database=YourDatabase;Trusted_Connection=True;TrustServerCertificate=True",
        "API_DEFAULT_TIMEOUT": "30",
        "API_MAX_TIMEOUT": "300",
        "API_FOLLOW_REDIRECTS": "true",
        "API_VALIDATE_SSL": "true",
        "API_MAX_REDIRECTS": "10",
        "AZURE_DEVOPS_PAT": "your_azure_devops_personal_access_token",
        "DD_API_KEY": "your_datadog_api_key",
        "DD_APP_KEY": "your_datadog_application_key",
        "DD_SITE": "datadoghq.com"
      }
    }
  }
}
```

**Mac/Linux:**
```json
{
  "mcpServers": {
    "drsasumcp": {
      "command": "/path/to/DrSasuMcp/publish/DrSasuMcp",
      "env": {
        "SQL_CONNECTION_STRING": "Server=localhost;Database=YourDatabase;User Id=sa;Password=YourPassword;TrustServerCertificate=True",
        "API_DEFAULT_TIMEOUT": "30",
        "API_MAX_TIMEOUT": "300",
        "API_FOLLOW_REDIRECTS": "true",
        "API_VALIDATE_SSL": "true",
        "API_MAX_REDIRECTS": "10",
        "AZURE_DEVOPS_PAT": "your_azure_devops_personal_access_token",
        "DD_API_KEY": "your_datadog_api_key",
        "DD_APP_KEY": "your_datadog_application_key",
        "DD_SITE": "datadoghq.com"
      }
    }
  }
}
```

> **Note:** Using the compiled executable (Option 2) is recommended for production use as it starts faster and doesn't require the .NET SDK to be installed (only the .NET runtime).

#### Environment Variables

**SQL Tool Configuration (Required):**
```bash
# Windows PowerShell
$env:SQL_CONNECTION_STRING = "Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True"

# Linux/Mac
export SQL_CONNECTION_STRING="Server=localhost;Database=test;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
```

**API Tool Configuration (Optional):**
```bash
# Windows PowerShell
$env:API_DEFAULT_TIMEOUT = "30"          # Default timeout in seconds (default: 30)
$env:API_MAX_TIMEOUT = "300"             # Maximum allowed timeout (default: 300)
$env:API_FOLLOW_REDIRECTS = "true"       # Follow HTTP redirects (default: true)
$env:API_VALIDATE_SSL = "true"           # Validate SSL certificates (default: true)
$env:API_MAX_REDIRECTS = "10"            # Maximum redirect hops (default: 10)

# Linux/Mac
export API_DEFAULT_TIMEOUT="30"
export API_MAX_TIMEOUT="300"
export API_FOLLOW_REDIRECTS="true"
export API_VALIDATE_SSL="true"
export API_MAX_REDIRECTS="10"
```

> **Note:** If API environment variables are not set, the tool will use sensible defaults. These values can also be overridden per-request through method parameters.

**Azure DevOps Tool Configuration (Required for PR reviews):**
```bash
# Windows PowerShell
$env:AZURE_DEVOPS_PAT = "your_personal_access_token"
$env:AZURE_DEVOPS_ORG = "your_organization"      # Optional
$env:AZURE_DEVOPS_MAX_FILES = "100"              # Optional (default: 100)
$env:AZURE_DEVOPS_TIMEOUT = "60"                 # Optional (default: 60)

# Linux/Mac
export AZURE_DEVOPS_PAT="your_personal_access_token"
export AZURE_DEVOPS_ORG="your_organization"
export AZURE_DEVOPS_MAX_FILES="100"
export AZURE_DEVOPS_TIMEOUT="60"
```

**Setting up Azure DevOps Personal Access Token:**
1. Go to Azure DevOps → User Settings → Personal Access Tokens
2. Click "New Token"
3. Set required scopes: **Code (Read)** and **Pull Request Threads (Read)**
4. Copy the token and set it as an environment variable

> **Note:** The Azure DevOps PAT is required to use PR review features. Other environment variables are optional with sensible defaults.

**Datadog Tool Configuration (Required for Datadog features):**
```bash
# Windows PowerShell
$env:DD_API_KEY = "your_datadog_api_key"
$env:DD_APP_KEY = "your_datadog_application_key"  # Optional, for admin operations
$env:DD_SITE = "datadoghq.com"                    # Optional (default: datadoghq.com)
$env:DD_TIMEOUT_SECONDS = "60"                    # Optional (default: 60)
$env:DD_MAX_RESULTS = "1000"                      # Optional (default: 1000)

# Linux/Mac
export DD_API_KEY="your_datadog_api_key"
export DD_APP_KEY="your_datadog_application_key"
export DD_SITE="datadoghq.com"
export DD_TIMEOUT_SECONDS="60"
export DD_MAX_RESULTS="1000"
```

**Setting up Datadog API Keys:**
1. Go to Datadog → Organization Settings → API Keys
2. Click "New Key" to create an API key
3. For admin operations (monitors, dashboards), also create an Application Key
4. Copy the keys and set them as environment variables

> **Note:** The Datadog API key is required for all operations. The Application key is optional but required for some admin operations. Site can be `datadoghq.com` (US), `datadoghq.eu` (EU), `us3.datadoghq.com`, `us5.datadoghq.com`, or `ap1.datadoghq.com`.

---

## 💡 Usage Examples

### SQL Database Operations

```
You: "Show me all tables in my database"
AI: Lists all tables with schema names

You: "Describe the Users table"
AI: Shows complete table structure with columns, types, indexes, and constraints

You: "Get all users who registered in the last 30 days"
AI: Executes: SELECT * FROM Users WHERE RegistrationDate >= DATEADD(day, -30, GETDATE())

You: "Create a Products table with Id, Name, and Price columns"
AI: Executes CREATE TABLE with appropriate schema
```

### API Testing

```
You: "Test the GitHub API - get user info for 'octocat'"
AI: Sends GET request to https://api.github.com/users/octocat

You: "Check if my API at localhost:5000/health returns 200 status"
AI: Executes test with status code validation

You: "Test my authentication endpoint with these credentials"
AI: Sends POST with authentication and validates response

You: "Run a complete test suite on my API endpoints"
AI: Executes multiple tests with detailed pass/fail reporting
```

### Azure DevOps PR Review

```
You: "Review this PR: https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123"
AI: Analyzes PR with security, quality, and best practice checks

You: "Check PR 456 for security vulnerabilities"
AI: Runs security analysis and reports hardcoded secrets, SQL injection risks, etc.

You: "Show me the diff for AuthService.cs in PR 789"
AI: Displays unified diff with line-by-line changes

You: "What changed in this PR and are there any critical issues?"
AI: Provides PR summary with file changes and prioritized issue list
```

### Datadog Monitoring & Troubleshooting

```
You: "Troubleshoot high error rate in payment-service"
AI: Analyzes metrics, logs, traces, and errors to provide recommendations

You: "What's the root cause of the performance degradation?"
AI: Performs root cause analysis by correlating data across all sources

You: "Get active alerts from Datadog"
AI: Lists all currently active alerts with monitor details

You: "Query CPU metrics for the last hour"
AI: Queries and returns CPU usage metrics with time series data

You: "Show me the service map for payment-service"
AI: Displays service dependencies and health status

You: "Recommend fixes for the database timeout errors"
AI: Generates prioritized fix recommendations with impact/effort estimates
```

---

## 🏗️ Architecture

```
DrSasuMcp/
├── Program.cs                    # Application entry point and DI configuration
├── DrSasuMcp.csproj             # Project file with dependencies
└── Tools/
    ├── OperationResult.cs       # Shared result model
    ├── SQL/
    │   ├── SQLTool.cs           # MCP-exposed SQL operations
    │   ├── SQLToolConstant.cs   # SQL query constants
    │   ├── ISqlConnectionFactory.cs
    │   ├── SqlConnectionFactory.cs
    │   └── README.md            # SQL tool documentation
    ├── API/
    │   ├── APITool.cs           # MCP-exposed API operations
    │   ├── APIToolConstants.cs  # API configuration constants
    │   ├── IHttpClientFactory.cs
    │   ├── HttpClientFactory.cs
    │   ├── Models/              # Request/Response models
    │   ├── Authentication/      # Auth handlers (Bearer, Basic, API Key)
    │   ├── Validators/          # Response validators
    │   └── README.md            # API tool documentation
    └── AzureDevOps/
        ├── AzureDevOpsTool.cs   # MCP-exposed PR review operations
        ├── AzureDevOpsService.cs # Azure DevOps REST API client
        ├── DiffService.cs       # DiffPlex integration for diffs
        ├── Models/              # PR, FileChange, ReviewComment models
        ├── Analyzers/           # Security, Quality, BestPractices
        ├── Utils/               # PR URL parser
        ├── README.md            # Azure DevOps tool documentation
        └── QUICKSTART.md        # Quick start guide
    └── Datadog/
        ├── DatadogTool.cs       # MCP-exposed Datadog operations
        ├── DatadogService.cs    # Datadog REST API client
        ├── IDatadogService.cs   # Service interface
        ├── DatadogToolConstants.cs # Configuration constants
        ├── Models/              # Metric, Log, Trace, Error, ServiceMap models
        ├── Troubleshooters/     # Metrics, Logs, Traces, Errors, ServiceMap troubleshooters
        ├── Utils/               # QueryBuilder, TimeRangeParser
        ├── README.md            # Datadog tool documentation
        ├── IMPLEMENTATION_PLAN.md # Implementation details
        └── TOOLS_LIST.md        # Complete tools list
```

### Design Principles

- **Dependency Injection**: All services registered and managed via DI container
- **Interface-Based**: Clean abstractions for testability and extensibility
- **Async/Await**: Non-blocking operations throughout
- **MCP-Native**: Automatic tool discovery via `[McpServerTool]` attributes
- **Error Handling**: Comprehensive exception handling with detailed error messages
- **Environment-Aware**: Configuration via environment variables with sensible defaults

---

## 🔧 Development

### Building from Source

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests (when available)
dotnet test

# Publish release build
dotnet publish -c Release -o ./publish
```

### Adding New Tools

1. Create a new tool class in `Tools/YourTool/`
2. Decorate with `[McpServerToolType]` attribute
3. Add methods with `[McpServerTool]` attribute
4. Register dependencies in `Program.cs`
5. The MCP server will automatically discover your tools

Example:
```csharp
[McpServerToolType]
public class MyNewTool
{
    [McpServerTool]
    public async Task<OperationResult> MyOperation(string parameter)
    {
        // Your implementation
        return new OperationResult 
        { 
            Success = true, 
            Data = result 
        };
    }
}
```

---

## 📚 Documentation

- **[SQL Tool Documentation](DrSasuMcp/Tools/SQL/README.md)** - Complete guide to database operations
- **[API Tool Documentation](DrSasuMcp/Tools/API/README.md)** - Complete guide to API testing
- **[Azure DevOps Tool Documentation](DrSasuMcp/Tools/AzureDevOps/README.md)** - Complete guide to PR reviews
- **[Azure DevOps Quick Start](DrSasuMcp/Tools/AzureDevOps/QUICKSTART.md)** - 5-minute setup guide
- **[Datadog Tool Documentation](DrSasuMcp/Tools/Datadog/README.md)** - Complete guide to Datadog monitoring and troubleshooting
- **[MCP Protocol](https://modelcontextprotocol.io/)** - Model Context Protocol specification

---

## 🔒 Security Considerations

### SQL Tool
- ⚠️ Accepts raw SQL queries—validate inputs in production
- Grant minimal necessary database permissions
- Never commit connection strings with credentials
- Use encrypted connections for remote databases
- Enable auditing for destructive operations

### API Tool
- Validate SSL certificates by default (can be disabled via environment variable)
- Secure credential storage for authentication
- Request/response logging for debugging
- Timeout protection against hanging requests
- Configure timeout limits via `API_MAX_TIMEOUT`

### Azure DevOps Tool
- PAT stored only in environment variables, never logged
- Read-only access to Azure DevOps (no write operations)
- SSL validation always enabled for API connections
- File content never logged to protect sensitive data
- Configurable file size and count limits

### Datadog Tool
- API keys stored only in environment variables, never logged
- Read-only operations by default (except event creation)
- SSL validation always enabled for API connections
- Comprehensive error handling and retry logic
- Configurable timeouts and result limits
- Intelligent troubleshooter selection based on issue description

---

## 🛣️ Roadmap

### Recently Added
- [x] Azure DevOps PR Review Tool with DiffPlex integration
- [x] Security, Quality, and Best Practices analyzers
- [x] Multiple diff formats (unified, side-by-side, inline)
- [x] Datadog Monitoring & Troubleshooting Tool
- [x] Advanced troubleshooting with root cause analysis
- [x] Intelligent fix recommendations with impact/effort estimates

### Planned Features
- [ ] AI-powered intelligent PR review comments
- [ ] Post review comments back to Azure DevOps
- [ ] GitHub PR review support
- [ ] GitLab MR review support
- [ ] Additional database support (PostgreSQL, MySQL, SQLite)
- [ ] GraphQL API testing
- [ ] WebSocket testing
- [ ] File system operations tool
- [ ] Cloud provider integration tools (AWS, Azure, GCP)
- [ ] Docker container management
- [ ] Git operations tool
- [ ] Docker containerization

### Community Feedback
Have ideas for new tools or features? [Open an issue](https://github.com/yourusername/DrSasuMcp/issues)!

---

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Standards
- Follow C# coding conventions
- Add XML documentation comments for public APIs
- Include unit tests for new features
- Update relevant documentation

---

## 📋 Requirements

- **Runtime**: .NET 8.0 or later
- **SQL Tool**: SQL Server 2017+ (requires `STRING_AGG` function)
- **Operating System**: Windows, Linux, or macOS
- **MCP Client**: Any MCP-compatible AI assistant

### NuGet Dependencies
- `DiffPlex` (^1.9.0) - Diff generation and analysis
- `Microsoft.Data.SqlClient` (^6.1.2) - SQL Server connectivity
- `Microsoft.Extensions.Hosting` (^8.0.1) - Hosting and DI infrastructure
- `ModelContextProtocol` (^0.4.0-preview.3) - MCP server framework

---

## 🐛 Troubleshooting

### SQL Connection Issues
```
Error: "Connection string is not set"
Solution: Set SQL_CONNECTION_STRING environment variable
```

### MCP Server Not Detected
```
Issue: AI assistant doesn't see the tools
Solution: 
1. Verify configuration file syntax
2. Restart AI assistant
3. Check server logs for errors
```

### Build Errors
```
Issue: Build fails with dependency errors
Solution: 
dotnet restore
dotnet clean
dotnet build
```

### API Timeout Issues
```
Issue: Requests timing out
Solution: 
1. Increase API_DEFAULT_TIMEOUT environment variable
2. Check network connectivity
3. Verify target API is responsive
```

### Azure DevOps Authentication Issues
```
Issue: "Authentication failed" or "PAT not found"
Solution: 
1. Set AZURE_DEVOPS_PAT environment variable
2. Verify PAT has not expired
3. Ensure PAT has Code (Read) and Pull Request Threads (Read) permissions
4. Restart your MCP client after setting environment variable
```

### Azure DevOps PR URL Issues
```
Issue: "Invalid Azure DevOps PR URL format"
Solution: 
URL must be in format:
https://dev.azure.com/{organization}/{project}/_git/{repository}/pullrequest/{id}
```

### Datadog Authentication Issues
```
Issue: "Authentication failed" or "API key not configured"
Solution: 
1. Set DD_API_KEY environment variable
2. Verify API key is valid and not expired
3. For admin operations, also set DD_APP_KEY
4. Check DD_SITE matches your Datadog region
5. Restart your MCP client after setting environment variables
```

### Datadog Query Issues
```
Issue: "Time range exceeds maximum" or "Invalid time format"
Solution: 
1. Time range is limited to 168 hours (7 days) maximum
2. Use relative time formats: "1h", "30m", "24h", "1h ago", "now"
3. Or use ISO 8601 format: "2024-01-01T00:00:00Z"
4. Ensure start time is before end time
```

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👏 Acknowledgments

- Built with [Model Context Protocol](https://modelcontextprotocol.io/)
- Inspired by the need for AI-native database and API tools
- Thanks to the MCP community for feedback and contributions

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/DrSasuMcp/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/DrSasuMcp/discussions)
- **Documentation**: See individual tool READMEs in `/Tools/` directories

---

**Ready to supercharge your AI workflows? Get started now!** 🚀

---

<div align="center">

Made with ❤️ by the DrSasuMcp team

[⭐ Star this repo](https://github.com/yourusername/DrSasuMcp) | [🐛 Report Bug](https://github.com/yourusername/DrSasuMcp/issues) | [💡 Request Feature](https://github.com/yourusername/DrSasuMcp/issues)

</div>

