# DrSasuMcp 🚀

**DrSasuMcp** is a powerful [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server that extends AI assistants with database management and API testing capabilities. Built with .NET 8, it provides seamless integration between AI-powered workflows and enterprise systems.

[![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![MCP Protocol](https://img.shields.io/badge/MCP-v0.4.0-green.svg)](https://modelcontextprotocol.io/)

## 🌟 Overview

DrSasuMcp brings **SQL Server database management** and **HTTP API testing** directly into your AI assistant conversations. Execute queries, manage schemas, test APIs, and validate responses—all through natural language commands.

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

---

## 🚀 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- SQL Server (for SQL tool features)
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
        "API_MAX_REDIRECTS": "10"
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
        "API_MAX_REDIRECTS": "10"
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
        "API_MAX_REDIRECTS": "10"
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
    └── API/
        ├── APITool.cs           # MCP-exposed API operations
        ├── APIToolConstants.cs  # API configuration constants
        ├── IHttpClientFactory.cs
        ├── HttpClientFactory.cs
        ├── Models/              # Request/Response models
        ├── Authentication/      # Auth handlers (Bearer, Basic, API Key)
        ├── Validators/          # Response validators
        └── README.md            # API tool documentation
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

---

## 🛣️ Roadmap

### Planned Features
- [ ] Additional database support (PostgreSQL, MySQL, SQLite)
- [ ] GraphQL API testing
- [ ] WebSocket testing
- [ ] File system operations tool
- [ ] Cloud provider integration tools (AWS, Azure, GCP)
- [ ] Docker container management
- [ ] Git operations tool
- [ ] Comprehensive test suite
- [ ] Performance benchmarks
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

