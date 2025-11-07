

# Azure DevOps PR Review Tool for DrSasuMcp

A comprehensive Model Context Protocol (MCP) server tool for analyzing Azure DevOps Pull Requests with automated code review, security analysis, and best practice validation.

## Overview

This tool allows AI assistants to fetch, analyze, and review Azure DevOps Pull Requests by:
- Retrieving PR metadata and file changes
- Generating detailed diffs using DiffPlex
- Running multiple code analyzers (Security, Quality, Best Practices)
- Providing actionable review comments with severity levels

## Features

### 🔍 Pull Request Analysis
- **PR Metadata** - Title, author, status, branches, reviewers
- **File Changes** - All modified, added, and deleted files
- **Line-by-Line Diffs** - Unified, side-by-side, and inline formats
- **Change Statistics** - Additions, deletions, change percentages

### 🛡️ Security Analysis
- Hardcoded credentials detection (passwords, API keys, tokens)
- SQL injection vulnerability detection
- Weak cryptography usage (MD5, SHA1)
- XSS vulnerability patterns
- Process execution and file path traversal checks
- **10+ security patterns** with actionable suggestions

### 📊 Code Quality Analysis
- File and method length validation
- Cyclomatic complexity detection
- Magic number identification
- TODO/FIXME comment tracking
- Naming convention validation
- Long line detection (>120 characters)
- **8+ quality checks** with improvement suggestions

### ✅ Best Practices Analysis
- Empty catch block detection
- Async/await pattern validation
- Resource management (IDisposable, using statements)
- Exception handling best practices
- HttpClient instantiation patterns
- Logging framework usage
- **13+ best practice checks** with recommendations

## MCP Exposed Methods

### 1. `ReviewPullRequest`
Performs comprehensive code review of a Pull Request.

**Parameters:**
- `prUrl` (required): Full Azure DevOps PR URL
- `includeAnalyzers` (optional): Comma-separated list - "security,quality,bestpractices" (default: all)
- `minIssueLevel` (optional): Minimum severity - "info", "warning", "critical" (default: info)

**Returns:**
```json
{
  "success": true,
  "data": {
    "pullRequestInfo": {
      "pullRequestId": 123,
      "title": "Add authentication feature",
      "author": "John Doe",
      "status": "Active"
    },
    "filesChanged": 5,
    "totalAdditions": 234,
    "totalDeletions": 67,
    "criticalIssues": 2,
    "warnings": 5,
    "suggestions": 8,
    "fileReviews": [
      {
        "filePath": "src/Services/AuthService.cs",
        "changeType": "Modified",
        "additions": 89,
        "deletions": 12,
        "comments": [
          {
            "line": 45,
            "level": "Critical",
            "analyzer": "Security",
            "code": "SEC001",
            "message": "Potential hardcoded password detected",
            "codeSnippet": "var password = \"secret123\";",
            "suggestion": "Use environment variables or Azure Key Vault"
          }
        ]
      }
    ],
    "overallAssessment": "Requires changes before merge",
    "reviewedAt": "2025-10-29T10:30:00Z",
    "reviewTimeMs": 3450
  }
}
```

**Example Usage:**
```
AI Assistant: "Review this PR for security issues: 
https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123"

Calls: ReviewPullRequest(
  prUrl: "https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123",
  includeAnalyzers: "security",
  minIssueLevel: "warning"
)
```

### 2. `GetPullRequestDiff`
Generates detailed diff for PR files.

**Parameters:**
- `prUrl` (required): Full Azure DevOps PR URL
- `filePath` (optional): Specific file to diff (e.g., "src/Program.cs")
- `diffFormat` (optional): Format - "unified", "sidebyside", "inline" (default: unified)

**Returns:** List of diff results with line-by-line changes

**Example Usage:**
```
AI: "Show me the diff for AuthService.cs in PR 123"

Calls: GetPullRequestDiff(
  prUrl: "https://dev.azure.com/org/project/_git/repo/pullrequest/123",
  filePath: "src/Services/AuthService.cs",
  diffFormat: "unified"
)
```

### 3. `GetPullRequestInfo`
Retrieves PR metadata without analysis.

**Parameters:**
- `prUrl` (required): Full Azure DevOps PR URL

**Returns:** PR information including title, author, status, branches, file count

### 4. `TestConnection`
Tests Azure DevOps connection and PAT authentication.

**Parameters:** None

**Returns:** Connection status and authentication validation

## Configuration

### Environment Variables

#### Required
```bash
AZURE_DEVOPS_PAT=your_personal_access_token
```

#### Optional
```bash
AZURE_DEVOPS_ORG=your_organization          # Default organization
AZURE_DEVOPS_MAX_FILES=100                  # Max files to analyze
AZURE_DEVOPS_TIMEOUT=60                     # Request timeout in seconds
```

### Personal Access Token (PAT) Setup

1. Go to Azure DevOps → User Settings → Personal Access Tokens
2. Click "New Token"
3. Set scopes:
   - ✅ **Code** (Read)
   - ✅ **Pull Request Threads** (Read)
4. Copy the token
5. Set environment variable:
   ```bash
   # Windows
   set AZURE_DEVOPS_PAT=your_token_here
   
   # Linux/Mac
   export AZURE_DEVOPS_PAT=your_token_here
   ```

## Supported PR URL Format

```
https://dev.azure.com/{organization}/{project}/_git/{repository}/pullrequest/{id}
```

**Examples:**
- `https://dev.azure.com/microsoft/vscode/_git/vscode/pullrequest/12345`
- `https://dev.azure.com/mycompany/MyProject/_git/MainRepo/pullrequest/42`

## Supported File Types

### Security & Best Practices
- `.cs` (C#)
- `.js`, `.ts`, `.jsx`, `.tsx` (JavaScript/TypeScript)
- `.java` (Java)
- `.py` (Python)
- `.go` (Go)
- `.php` (PHP)
- `.rb` (Ruby)

### Code Quality
- All of the above
- Plus language-specific quality checks

## Issue Codes Reference

### Security (SEC)
| Code | Severity | Description |
|------|----------|-------------|
| SEC001 | Critical | Hardcoded password |
| SEC002 | Critical | Hardcoded API key |
| SEC003 | Critical | Hardcoded secret/token |
| SEC004 | Critical | SQL injection vulnerability |
| SEC005 | Warning | Weak cryptography (MD5/SHA1) |
| SEC006 | Critical | XSS vulnerability |
| SEC007 | Critical | Use of eval() |
| SEC008 | Warning | Process execution |
| SEC009 | Warning | File path concatenation |
| SEC010 | Critical | Hardcoded auth token |

### Code Quality (QUAL)
| Code | Severity | Description |
|------|----------|-------------|
| QUAL001 | Warning | File too long |
| QUAL002 | Info | TODO/FIXME comment |
| QUAL003 | Info | Line too long |
| QUAL004 | Info | Magic number |
| QUAL005 | Warning | Method too long |
| QUAL006 | Warning | High complexity |
| QUAL007 | Info | Naming convention (private) |
| QUAL008 | Info | Naming convention (public) |

### Best Practices (BP)
| Code | Severity | Description |
|------|----------|-------------|
| BP001 | Critical | Empty catch block |
| BP002 | Warning | Incomplete error handling |
| BP003 | Warning | Blocking with .Result |
| BP004 | Warning | Blocking with .Wait() |
| BP005 | Warning | String concatenation in loop |
| BP006 | Info | Use 'is null' |
| BP007 | Info | Use 'is not null' |
| BP008 | Info | IDisposable implementation |
| BP009 | Warning | New HttpClient |
| BP010 | Info | Console.WriteLine usage |
| BP011 | Warning | Async without await |
| BP012 | Warning | Missing using statement |
| BP013 | Info | Generic exception catch |

## Architecture

```
AzureDevOpsTool
├── IAzureDevOpsService → REST API client
│   ├── GetPullRequestInfoAsync()
│   ├── GetPullRequestChangesAsync()
│   ├── GetFileContentAsync()
│   └── TestConnectionAsync()
│
├── IDiffService → DiffPlex integration
│   ├── GenerateUnifiedDiff()
│   ├── GenerateSideBySideDiff()
│   ├── GenerateInlineDiff()
│   └── CalculateStatistics()
│
└── ICodeAnalyzer → Analysis pipeline
    ├── SecurityAnalyzer
    ├── CodeQualityAnalyzer
    └── BestPracticesAnalyzer
```

## Example Workflows

### Workflow 1: Complete PR Review
```
User: "Review PR 123 for any issues"

AI calls ReviewPullRequest with all analyzers
→ Returns comprehensive review with all findings
→ AI summarizes critical issues and recommendations
```

### Workflow 2: Security-Focused Review
```
User: "Check this PR for security vulnerabilities"

AI calls ReviewPullRequest with includeAnalyzers="security"
→ Returns only security findings
→ AI highlights critical security issues
```

### Workflow 3: Diff Analysis
```
User: "What changed in the AuthService file?"

AI calls GetPullRequestDiff for specific file
→ Returns line-by-line diff
→ AI explains the changes
```

### Workflow 4: PR Summary
```
User: "Give me a quick summary of PR 456"

AI calls GetPullRequestInfo
→ Returns metadata
→ AI summarizes title, author, status, files changed
```

## Performance

- **PR Metadata Fetch**: < 2 seconds
- **Single File Diff**: < 100ms
- **Full PR Review (10 files)**: < 10 seconds
- **Full PR Review (50 files)**: < 30 seconds
- **Memory Usage**: < 200MB for typical PRs

## Error Handling

### Common Errors

**Authentication Failed**
```json
{
  "success": false,
  "error": "Authentication failed: Azure DevOps Personal Access Token not found"
}
```
**Solution:** Set `AZURE_DEVOPS_PAT` environment variable

**Invalid URL**
```json
{
  "success": false,
  "error": "Invalid Azure DevOps PR URL format"
}
```
**Solution:** Use correct URL format

**Network Error**
```json
{
  "success": false,
  "error": "Failed to connect to Azure DevOps: The remote name could not be resolved"
}
```
**Solution:** Check network connection and firewall settings

## Limitations

- Maximum 100 files per PR (configurable)
- Files larger than 1MB are skipped
- Binary files are not analyzed
- Analysis is language-specific (supported languages only)

## Future Enhancements

- AI-powered intelligent review comments
- Custom analysis rules configuration
- Post comments directly to Azure DevOps
- GitHub and GitLab support
- Historical trend analysis
- Web dashboard for review visualization

## Dependencies

- **DiffPlex 1.9.0** - Diff generation
- **System.Net.Http** - HTTP client
- **System.Text.Json** - JSON parsing
- **ModelContextProtocol** - MCP server integration

## License

Part of the DrSasuMcp project.

## Support

For issues or feature requests, please refer to the main DrSasuMcp project repository.

