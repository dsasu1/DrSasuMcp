# DrSasuMcp 🚀

**DrSasuMcp** is a collection of powerful [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) servers that extend AI assistants with database management, API testing, code review, and monitoring capabilities. Built with .NET 8, each tool is a standalone MCP server providing seamless integration between AI-powered workflows and enterprise systems.

[![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![MCP Protocol](https://img.shields.io/badge/MCP-v0.4.0-green.svg)](https://modelcontextprotocol.io/)

## 🌟 Overview

DrSasuMcp brings **SQL Server and MongoDB database management**, **HTTP API testing**, **Azure DevOps PR review**, and **Datadog monitoring & troubleshooting** directly into your AI assistant conversations. Each capability is a standalone MCP server that can be deployed independently or together. Execute queries, manage schemas, test APIs, review pull requests, troubleshoot issues, and monitor systems—all through natural language commands.

### Why DrSasuMcp?

- 🤖 **AI-Native**: Designed specifically for AI assistant integration via MCP
- 🔧 **Modular Architecture**: Each tool is a separate MCP server—use only what you need
- 🛠️ **Production-Ready**: Comprehensive error handling, logging, and safety features
- 🔌 **Extensible**: Clean architecture for adding new tools and capabilities
- 🎯 **Type-Safe**: Built with C# for strong typing and reliability
- ⚡ **Async First**: Non-blocking operations for optimal performance
- 🧪 **Well-Tested**: Comprehensive unit tests for each tool
- 📦 **Independent Deployment**: Each tool can be deployed, updated, and scaled independently
- 🔒 **Isolation**: Issues in one tool don't affect others
- ⚙️ **Flexible Configuration**: Configure only the tools you need with their specific settings

---

## 🏛️ Multi-Project Architecture

DrSasuMcp uses a **modular multi-project architecture** where each tool is a completely independent MCP server. This design provides several key benefits:

### 🎯 Benefits

1. **Use What You Need**
   - Deploy only the tools your project requires
   - Reduce resource footprint by running fewer servers
   - Simpler configuration with tool-specific environment variables

2. **Independent Deployment**
   - Update one tool without affecting others
   - Roll back individual tools if issues arise
   - Different versioning for each tool

3. **Isolation & Reliability**
   - If one tool crashes, others continue running
   - Memory and resource isolation per tool
   - Independent logging and monitoring

4. **Easier Development**
   - Work on one tool without building the entire solution
   - Faster build times for individual tools
   - Clear separation of concerns and dependencies

5. **Flexible Scaling**
   - Scale frequently-used tools independently
   - Different deployment strategies per tool
   - Optimize resources based on actual usage

### 📦 Available Servers

Each of these is a standalone MCP server:

- **DrSasuMcp.SQL** - SQL Server database management
- **DrSasuMcp.MongoDB** - MongoDB database management
- **DrSasuMcp.API** - HTTP API testing
- **DrSasuMcp.AzureDevOps** - Pull request reviews
- **DrSasuMcp.Datadog** - Monitoring and troubleshooting

Mix and match based on your needs!

---

## 📦 Available Tools

DrSasuMcp provides five standalone MCP servers, each focused on a specific capability:

### 🗄️ SQL Database Tool
Complete SQL Server management—explore schemas, execute queries, and manage data with comprehensive safety features.

**[📖 Full Documentation](DrSasuMcp.SQL/README.md)**

### 🍃 MongoDB Database Tool
MongoDB operations with schema inference, CRUD operations, and collection management.

**[📖 Full Documentation](DrSasuMcp.MongoDB/README.md)**

### 🌐 API Testing Tool
HTTP API testing with authentication, validation, and test suites—your Postman alternative in AI workflows.

**[📖 Full Documentation](DrSasuMcp.API/README.md)**

### 🔍 Azure DevOps PR Review Tool
Automated code review with security, quality, and best practice analysis for Azure DevOps pull requests.

**[📖 Full Documentation](DrSasuMcp.AzureDevOps/README.md)**

### 📊 Datadog Monitoring & Troubleshooting Tool
Monitor, troubleshoot, and resolve issues with comprehensive Datadog integration and intelligent root cause analysis.

**[📖 Full Documentation](DrSasuMcp.Datadog/README.md)**

---

## 🚀 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- MCP-compatible AI assistant (Claude Desktop, VS Code with MCP, etc.)
- Specific prerequisites for each tool (SQL Server, MongoDB, etc.) - see individual tool documentation

### Installation

```bash
# Clone the repository
git clone https://github.com/yourusername/DrSasuMcp.git
cd DrSasuMcp

# Build all projects or individual tools
dotnet build DrSasuMcp.sln
# Or: dotnet build DrSasuMcp.SQL/DrSasuMcp.SQL.csproj
```

### Configuration

Each tool is a separate MCP server. Configure only the ones you need in your Claude Desktop config file:

**Windows:** `%APPDATA%\Claude\claude_desktop_config.json`  
**Mac:** `~/Library/Application Support/Claude/claude_desktop_config.json`

**Example - Using Development Mode:**

```json
{
  "mcpServers": {
    "drsasumcp-sql": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Projects\\personal\\DrSasuMcp\\DrSasuMcp.SQL\\DrSasuMcp.SQL.csproj"],
      "env": {
        "SQL_CONNECTION_STRING": "Server=.;Database=YourDatabase;Trusted_Connection=True;TrustServerCertificate=True"
      }
    }
  }
}
```

**Example - Using Published Executables (Recommended for Production):**

```bash
# Publish the tools you need
dotnet publish DrSasuMcp.SQL/DrSasuMcp.SQL.csproj -c Release -o ./publish/sql
```

```json
{
  "mcpServers": {
    "drsasumcp-sql": {
      "command": "C:\\Projects\\personal\\DrSasuMcp\\publish\\sql\\DrSasuMcp.SQL.exe",
      "env": {
        "SQL_CONNECTION_STRING": "Server=.;Database=YourDatabase;Trusted_Connection=True;TrustServerCertificate=True"
      }
    }
  }
}
```

> 📖 **For detailed configuration of each tool**, including all environment variables and options, see the individual tool documentation linked above.

### Running Individual Tools

Each tool can be run independently for testing:

```bash
# SQL Tool
cd DrSasuMcp.SQL
set SQL_CONNECTION_STRING="Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run

# MongoDB Tool  
cd DrSasuMcp.MongoDB
set MONGODB_CONNECTION_STRING="mongodb://localhost:27017/test"
dotnet run

# API Tool
cd DrSasuMcp.API
dotnet run
```

> **Note:** Use `set` on Windows or `export` on Linux/Mac. See individual tool documentation for all configuration options.

---

## 💡 Usage Examples

Once configured, you can interact with the tools through natural language:

**SQL Operations:**
```
"Show me all tables in my database"
"Describe the Users table"
"Get all users who registered in the last 30 days"
```

**MongoDB Operations:**
```
"Show me all collections"
"Get all users with age over 25"
"Update user John's age to 31"
```

**API Testing:**
```
"Test the GitHub API - get user info for 'octocat'"
"Check if my API at localhost:5000/health returns 200"
```

**Azure DevOps PR Review:**
```
"Review this PR: https://dev.azure.com/org/project/_git/repo/pullrequest/123"
"Check PR 456 for security vulnerabilities"
```

**Datadog Troubleshooting:**
```
"Troubleshoot high error rate in payment-service"
"Get active alerts from Datadog"
"Show me the service map for payment-service"
```

> 📖 **See each tool's documentation for complete usage guides and examples.**

---

## 🏗️ Architecture

DrSasuMcp follows a modular multi-project architecture where each tool is a standalone MCP server:

```
DrSasuMcp/
├── DrSasuMcp.sln                        # Solution file
├── README.md                            # This file
├── DrSasuMcp.Common/                    # Shared models and utilities
│   ├── DrSasuMcp.Common.csproj
│   └── Models/
│       └── OperationResult.cs           # Shared result model
├── DrSasuMcp.SQL/                       # SQL Server MCP Server
│   ├── Program.cs                       # SQL server entry point
│   ├── DrSasuMcp.SQL.csproj
│   ├── README.md                        # SQL tool documentation
│   └── SQL/
│       ├── SQLTool.cs                   # MCP-exposed SQL operations
│       ├── SQLToolConstant.cs
│       ├── ISqlConnectionFactory.cs
│       └── SqlConnectionFactory.cs
├── DrSasuMcp.MongoDB/                   # MongoDB MCP Server
│   ├── Program.cs                       # MongoDB server entry point
│   ├── DrSasuMcp.MongoDB.csproj
│   ├── README.md                        # MongoDB tool documentation
│   └── MongoDB/
│       ├── MongoDBTool.cs               # MCP-exposed MongoDB operations
│       ├── IMongoConnectionFactory.cs
│       └── MongoConnectionFactory.cs
├── DrSasuMcp.API/                       # API Testing MCP Server
│   ├── Program.cs                       # API server entry point
│   ├── DrSasuMcp.API.csproj
│   ├── README.md                        # API tool documentation
│   └── API/
│       ├── APITool.cs                   # MCP-exposed API operations
│       ├── APIToolConstants.cs
│       ├── IHttpClientFactory.cs
│       ├── HttpClientFactory.cs
│       ├── Models/                      # Request/Response models
│       ├── Authentication/              # Auth handlers
│       └── Validators/                  # Response validators
├── DrSasuMcp.AzureDevOps/               # Azure DevOps PR Review MCP Server
│   ├── Program.cs                       # Azure DevOps server entry point
│   ├── DrSasuMcp.AzureDevOps.csproj
│   ├── README.md                        # Azure DevOps tool documentation
│   └── AzureDevOps/
│       ├── AzureDevOpsTool.cs           # MCP-exposed PR review operations
│       ├── AzureDevOpsService.cs
│       ├── DiffService.cs
│       ├── Models/                      # PR, FileChange models
│       ├── Analyzers/                   # Security, Quality, BestPractices
│       └── Utils/                       # PR URL parser
├── DrSasuMcp.Datadog/                   # Datadog Monitoring MCP Server
│   ├── Program.cs                       # Datadog server entry point
│   ├── DrSasuMcp.Datadog.csproj
│   ├── README.md                        # Datadog tool documentation
│   └── Datadog/
│       ├── DatadogTool.cs               # MCP-exposed Datadog operations
│       ├── DatadogService.cs
│       ├── IDatadogService.cs
│       ├── DatadogToolConstants.cs
│       ├── Models/                      # Metric, Log, Trace models
│       ├── Troubleshooters/             # Analysis engines
│       └── Utils/                       # Query builders
└── Tests/                               # Test projects
    ├── DrSasuMcp.Tests/                 # Common tests
    ├── DrSasuMcp.Tests.SQL/             # SQL tool tests
    ├── DrSasuMcp.Tests.MongoDB/         # MongoDB tool tests
    ├── DrSasuMcp.Tests.API/             # API tool tests
    ├── DrSasuMcp.Tests.AzureDevOps/     # Azure DevOps tool tests
    └── DrSasuMcp.Tests.Datadog/         # Datadog tool tests
```

### Design Principles

- **Modular Architecture**: Each tool is a separate MCP server with its own entry point
- **Shared Common Library**: Common models and utilities in `DrSasuMcp.Common`
- **Independent Deployment**: Each server can be built, deployed, and run independently
- **Dependency Injection**: All services registered and managed via DI container
- **Interface-Based**: Clean abstractions for testability and extensibility
- **Async/Await**: Non-blocking operations throughout
- **MCP-Native**: Automatic tool discovery via `[McpServerTool]` attributes
- **Error Handling**: Comprehensive exception handling with detailed error messages
- **Environment-Aware**: Configuration via environment variables with sensible defaults
- **Well-Tested**: Each tool has its own test project with comprehensive coverage

---

## 🔧 Development

### Building from Source

```bash
# Clone and build all projects
git clone https://github.com/yourusername/DrSasuMcp.git
cd DrSasuMcp
dotnet build DrSasuMcp.sln

# Or build individual tools
dotnet build DrSasuMcp.SQL/DrSasuMcp.SQL.csproj

# Run tests
dotnet test DrSasuMcp.sln

# Publish for deployment
dotnet publish DrSasuMcp.SQL/DrSasuMcp.SQL.csproj -c Release -o ./publish/sql
```

### Adding New Tools

Create a new tool as a separate MCP server project:

```bash
# 1. Create project
dotnet new console -n DrSasuMcp.YourTool

# 2. Add to solution
dotnet sln DrSasuMcp.sln add DrSasuMcp.YourTool/DrSasuMcp.YourTool.csproj

# 3. Add reference to common library
cd DrSasuMcp.YourTool
dotnet add reference ../DrSasuMcp.Common/DrSasuMcp.Common.csproj

# 4. Add MCP packages
dotnet add package ModelContextProtocol
dotnet add package Microsoft.Extensions.Hosting
```

Create your tool class with `[McpServerTool]` attributes and configure hosting in `Program.cs`. See the [Architecture](#-architecture) section for details.

> 📖 **For a complete step-by-step guide**, see the individual tool projects as examples.

---

## 📚 Documentation

Each tool has comprehensive documentation with setup guides, API references, and examples:

- **[SQL Tool →](DrSasuMcp.SQL/README.md)** - SQL Server operations
- **[MongoDB Tool →](DrSasuMcp.MongoDB/README.md)** - MongoDB operations  
- **[API Tool →](DrSasuMcp.API/README.md)** - HTTP API testing
- **[Azure DevOps Tool →](DrSasuMcp.AzureDevOps/README.md)** - PR reviews
- **[Datadog Tool →](DrSasuMcp.Datadog/README.md)** - Monitoring & troubleshooting
- **[MCP Protocol →](https://modelcontextprotocol.io/)** - Model Context Protocol specification

---

## 🔒 Security Considerations

Each tool implements security best practices:

- **Database Tools**: Use parameterized queries, minimal permissions, encrypted connections
- **API Tool**: SSL validation by default, secure credential storage, timeout protection
- **Azure DevOps Tool**: Read-only access, PAT stored in environment variables only
- **Datadog Tool**: API keys in environment variables, SSL validation enabled

> 📖 **For detailed security guidelines**, see individual tool documentation.

---

## 🛣️ Roadmap

### Recently Added
- [x] **Modular multi-project architecture** - Each tool is now a separate MCP server
- [x] Comprehensive test projects for each tool
- [x] MongoDB Database Tool with full CRUD operations
- [x] Schema inference from sample documents
- [x] MongoDB query support with filters, projections, and sorting
- [x] Azure DevOps PR Review Tool with DiffPlex integration
- [x] Security, Quality, and Best Practices analyzers
- [x] Multiple diff formats (unified, side-by-side, inline)
- [x] Datadog Monitoring & Troubleshooting Tool
- [x] Advanced troubleshooting with root cause analysis
- [x] Intelligent fix recommendations with impact/effort estimates

### Planned Features
- [ ] PostgreSQL Database Tool
- [ ] MySQL Database Tool
- [ ] SQLite Database Tool
- [ ] AI-powered intelligent PR review comments
- [ ] Post review comments back to Azure DevOps
- [ ] GitHub PR review support
- [ ] GitLab MR review support
- [ ] MongoDB aggregation pipeline support
- [ ] MongoDB change streams support
- [ ] GraphQL API testing
- [ ] WebSocket testing
- [ ] File system operations tool
- [ ] Cloud provider integration tools (AWS, Azure, GCP)
- [ ] Docker container management
- [ ] Git operations tool
- [ ] Kubernetes operations tool

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

- **.NET 8.0 SDK** or later
- **MCP-compatible AI assistant** (Claude Desktop, VS Code with MCP, etc.)
- **Per-tool requirements**: See individual tool documentation

### Project Dependencies

- `ModelContextProtocol` (^0.4.0-preview.3) - MCP server framework
- `Microsoft.Extensions.Hosting` (^8.0.1) - Hosting and DI
- Tool-specific packages (SQL Client, MongoDB Driver, etc.)

> 📖 **For detailed requirements**, see each tool's documentation.

---

## 🐛 Troubleshooting

### Common Issues

**MCP Server Not Detected:**
- Verify configuration file syntax (JSON)
- Check project paths are correct
- Restart your MCP client
- Test server independently: `dotnet run --project <path>`

**Build Errors:**
```bash
dotnet restore DrSasuMcp.sln
dotnet clean DrSasuMcp.sln
dotnet build DrSasuMcp.sln
```

**Connection Issues:**
- Verify environment variables are set correctly
- Check server/database is accessible
- See individual tool documentation for specific troubleshooting

> 📖 **For tool-specific troubleshooting**, see each tool's documentation.

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
- **Documentation**: See individual tool READMEs linked above

---

**Ready to supercharge your AI workflows? Get started now!** 🚀

---

<div align="center">

Made with ❤️ by the DrSasuMcp team

[⭐ Star this repo](https://github.com/yourusername/DrSasuMcp) | [🐛 Report Bug](https://github.com/yourusername/DrSasuMcp/issues) | [💡 Request Feature](https://github.com/yourusername/DrSasuMcp/issues)

</div>

