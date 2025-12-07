# API Testing Tool for DrSasuMcp

A comprehensive Model Context Protocol (MCP) server tool for HTTP API testing and validation - a Postman alternative integrated directly into your AI assistant workflow.

## Overview

This tool provides a complete suite of HTTP request capabilities, authentication methods, and response validation features. It allows AI assistants to interact with REST APIs, execute tests, and validate responses programmatically.

## Features

### 🌐 HTTP Methods
- **GET** - Retrieve data from endpoints
- **POST** - Create new resources
- **PUT** - Update existing resources
- **PATCH** - Partially update resources
- **DELETE** - Remove resources
- **HEAD** - Check endpoint availability
- **OPTIONS** - Discover allowed methods

### 🔐 Authentication Support
- **Bearer Token** - OAuth 2.0 / JWT authentication
- **Basic Auth** - Username/password authentication
- **API Key** - Header-based API keys
- **Custom** - Custom header authentication

### ✅ Response Validation
- **Status Code** - Assert HTTP status codes
- **Headers** - Validate response headers
- **JSON Path** - Extract and validate JSON values
- **Response Time** - Performance assertions
- **Body Content** - Text matching and regex

### 🧪 Testing Features
- Single endpoint tests with multiple validations
- Test suites for comprehensive API testing
- Performance timing and metrics
- Detailed test result reporting

## Architecture

```
DrSasuMcp/Tools/API/
├── APITool.cs                    # Main MCP tool with exposed methods
├── APIToolConstants.cs           # Constants and configuration
├── IHttpClientFactory.cs         # HTTP client factory interface
├── HttpClientFactory.cs          # HTTP client implementation
├── Models/
│   ├── AuthType.cs              # Authentication type enum
│   ├── AuthenticationConfig.cs  # Auth configuration model
│   ├── HttpResponseResult.cs    # HTTP response wrapper
│   ├── ValidationType.cs        # Validation type enum
│   ├── ValidationRule.cs        # Validation rule model
│   ├── ValidationResult.cs      # Validation result model
│   ├── TestResult.cs            # Test execution result
│   └── TestSuiteConfig.cs       # Test suite configuration
├── Authentication/
│   ├── IAuthenticationHandler.cs
│   ├── BearerAuthHandler.cs
│   ├── BasicAuthHandler.cs
│   ├── ApiKeyAuthHandler.cs
│   └── CustomAuthHandler.cs
└── Validators/
    ├── IResponseValidator.cs
    ├── StatusCodeValidator.cs
    ├── HeaderValidator.cs
    ├── JsonPathValidator.cs
    ├── ResponseTimeValidator.cs
    └── BodyValidator.cs
```

## MCP Exposed Methods

### HTTP Request Methods

#### `SendGetRequest`
Execute GET requests with optional headers, query parameters, and authentication.

**Parameters:**
- `url` (required) - Target URL
- `headers` (optional) - JSON object with headers
- `queryParams` (optional) - JSON object with query parameters
- `auth` (optional) - JSON authentication configuration
- `timeoutSeconds` (optional) - Request timeout (default: 30)
- `followRedirects` (optional) - Follow HTTP redirects (default: true)
- `validateSsl` (optional) - Validate SSL certificates (default: true)

#### `SendPostRequest`, `SendPutRequest`, `SendPatchRequest`
Execute requests with body content.

**Additional Parameters:**
- `body` (optional) - Request body content
- `contentType` (optional) - Content type (default: "application/json")

#### `SendDeleteRequest`
Execute DELETE requests.

#### `SendHeadRequest`
Check endpoint availability without downloading body.

#### `SendOptionsRequest`
Discover allowed HTTP methods for an endpoint.

### Testing Methods

#### `ExecuteTest`
Run a complete API test with validation rules.

**Parameters:**
- `method` (required) - HTTP method
- `url` (required) - Target URL
- `body` (optional) - Request body
- `validationRules` (optional) - JSON array of validation rules
- `expectedStatus` (optional) - Expected status code
- `maxResponseTimeMs` (optional) - Maximum response time

**Example validation rules:**
```json
[
  {
    "type": "StatusCode",
    "operator": "equals",
    "expectedValue": 200
  },
  {
    "type": "JsonPath",
    "target": "$.users.length",
    "operator": "greaterThan",
    "expectedValue": 0
  },
  {
    "type": "ResponseTime",
    "operator": "lessThan",
    "expectedValue": 500
  }
]
```

#### `ExecuteTestSuite`
Run multiple tests in sequence.

**Parameters:**
- `baseUrl` (optional) - Base URL for all tests
- `tests` (required) - JSON array of test configurations
- `stopOnFailure` (optional) - Stop on first failure (default: false)

### Utility Methods

#### `ParseJsonPath`
Extract values from JSON using JSONPath expressions.

**Parameters:**
- `json` (required) - JSON content to parse
- `path` (required) - JSONPath expression (e.g., `$.users[0].name`)

#### `InspectEndpoint`
Get detailed endpoint information including headers, timing, and metadata.

**Parameters:**
- `url` (required) - URL to inspect
- `auth` (optional) - Authentication configuration

## Authentication Configuration

### Bearer Token
```json
{
  "type": "Bearer",
  "token": "your-jwt-token"
}
```

### Basic Auth
```json
{
  "type": "Basic",
  "username": "user",
  "password": "pass"
}
```

### API Key
```json
{
  "type": "ApiKey",
  "apiKeyHeader": "X-API-Key",
  "apiKeyValue": "your-api-key"
}
```

### Custom Headers
```json
{
  "type": "Custom",
  "customHeaders": {
    "X-Custom-Auth": "value",
    "X-Request-ID": "12345"
  }
}
```

## Validation Operators

### Comparison Operators
- `equals` - Exact match
- `notEquals` - Not equal
- `greaterThan` - Greater than (numeric)
- `lessThan` - Less than (numeric)
- `greaterThanOrEqual` - Greater than or equal (numeric)
- `lessThanOrEqual` - Less than or equal (numeric)

### Existence Operators
- `exists` - Value/header exists
- `notExists` - Value/header doesn't exist

### String Operators
- `contains` - String contains substring
- `matches` - Regex pattern match (body validation only)

## JSONPath Support

The tool supports basic JSONPath expressions:

- `$.field` - Root level field
- `$.nested.field` - Nested field
- `$.array[0]` - Array element by index
- `$.array.length` - Array length
- `$.users[0].name` - Nested array element field

## Environment Variables

Optional configuration via environment variables:

- `API_DEFAULT_TIMEOUT` - Default timeout in seconds (default: 30)
- `API_MAX_TIMEOUT` - Maximum allowed timeout (default: 300)
- `API_FOLLOW_REDIRECTS` - Follow redirects by default (default: true)
- `API_VALIDATE_SSL` - Validate SSL certificates (default: true)
- `API_MAX_REDIRECTS` - Maximum redirect hops (default: 10)

## Adding to MCP Client

Add the API Testing tool to your MCP client configuration:

### Claude Desktop Configuration

**Windows:** `%APPDATA%\Claude\claude_desktop_config.json`  
**Mac:** `~/Library/Application Support/Claude/claude_desktop_config.json`

**Development Mode (using `dotnet run`):**

```json
{
  "mcpServers": {
    "drsasumcp-api": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Projects\\personal\\DrSasuMcp\\DrSasuMcp.API\\DrSasuMcp.API.csproj"],
      "env": {
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

> **Note:** All environment variables are optional. The tool will use sensible defaults if not specified.

**Production Mode (using published executable):**

First, publish the project:
```bash
cd DrSasuMcp.API
dotnet publish -c Release -o ./publish
```

Then configure:

**Windows:**
```json
{
  "mcpServers": {
    "drsasumcp-api": {
      "command": "C:\\Projects\\personal\\DrSasuMcp\\DrSasuMcp.API\\publish\\DrSasuMcp.API.exe",
      "env": {
        "API_DEFAULT_TIMEOUT": "30",
        "API_VALIDATE_SSL": "true"
      }
    }
  }
}
```

**Mac/Linux:**
```json
{
  "mcpServers": {
    "drsasumcp-api": {
      "command": "/path/to/DrSasuMcp/DrSasuMcp.API/publish/DrSasuMcp.API",
      "env": {
        "API_DEFAULT_TIMEOUT": "30",
        "API_VALIDATE_SSL": "true"
      }
    }
  }
}
```

> **Note:** After updating your MCP client configuration, restart the client for changes to take effect.

## Response Format

### HTTP Response Result
```json
{
  "success": true,
  "data": {
    "statusCode": 200,
    "statusDescription": "OK",
    "headers": {
      "content-type": "application/json",
      "content-length": "1234"
    },
    "body": "{\"message\": \"Success\"}",
    "responseTimeMs": 145,
    "contentLength": 1234,
    "contentType": "application/json",
    "isSuccess": true,
    "timestamp": "2025-10-26T10:30:00Z"
  }
}
```

### Test Result
```json
{
  "success": true,
  "data": {
    "testPassed": true,
    "totalValidations": 3,
    "passedValidations": 3,
    "failedValidations": 0,
    "responseTimeMs": 145,
    "validationResults": [
      {
        "isValid": true,
        "message": "Status code 200 matches expected equals 200",
        "actualValue": 200,
        "expectedValue": 200,
        "validationType": "StatusCode",
        "target": "StatusCode"
      }
    ],
    "response": { /* full response object */ }
  }
}
```

## Usage Examples

### Simple GET Request
```
User: "Test the GitHub API - get info about the 'microsoft' user"
AI: Calls SendGetRequest with appropriate parameters
```

### POST with Authentication
```
User: "Create a new item in my API with this data: {name: 'Test'}"
AI: Calls SendPostRequest with body and auth configuration
```

### Complete API Test
```
User: "Test my API at /api/users - ensure it returns 200 and has users"
AI: Calls ExecuteTest with validation rules for status and JSONPath
```

### Test Suite
```
User: "Run health checks on all my API endpoints"
AI: Calls ExecuteTestSuite with multiple test configurations
```

## Dependencies

- `System.Net.Http` - HTTP client functionality
- `System.Text.Json` - JSON parsing and serialization
- `Microsoft.Extensions.Logging` - Logging framework
- `ModelContextProtocol.Server` - MCP server attributes

## Error Handling

The tool provides comprehensive error handling for:
- Network errors (timeouts, DNS failures, connection refused)
- SSL certificate errors
- Malformed JSON responses
- Invalid authentication
- HTTP errors (4xx, 5xx status codes)
- Validation failures

All errors are logged and returned in the `OperationResult` format with descriptive error messages.

## Extensibility

The tool is designed for easy extension:

### Adding New Authentication Types
1. Create a new `IAuthenticationHandler` implementation
2. Register it in the dependency injection container

### Adding New Validators
1. Create a new `IResponseValidator` implementation
2. Add the validation type to `ValidationType` enum
3. Register it in the dependency injection container

## License

Part of the DrSasuMcp project.

