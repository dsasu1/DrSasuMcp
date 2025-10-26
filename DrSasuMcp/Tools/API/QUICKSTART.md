# Quick Integration Guide - API Testing Tool

## 🚀 Quick Start

This guide will help you integrate the API Testing Tool into your DrSasuMcp MCP server.

## Step 1: Add NuGet Package (Optional)

If you're using .NET Core/5+, you may need to add this package for query string parsing:

```bash
dotnet add package System.Web.HttpUtility --version 4.3.0
```

**OR** modify the query string parsing code in `APITool.cs` (see IMPLEMENTATION_NOTES.md).

## Step 2: Register Services

Add these registrations to your DI container (typically in `Program.cs` or `Startup.cs`):

```csharp
using DrSasuMcp.Tools.API;
using DrSasuMcp.Tools.API.Authentication;
using DrSasuMcp.Tools.API.Validators;

// In your service configuration:

// HTTP Client Factory
services.AddSingleton<IHttpClientFactory, HttpClientFactory>();

// Authentication Handlers
services.AddSingleton<IAuthenticationHandler, BearerAuthHandler>();
services.AddSingleton<IAuthenticationHandler, BasicAuthHandler>();
services.AddSingleton<IAuthenticationHandler, ApiKeyAuthHandler>();
services.AddSingleton<IAuthenticationHandler, CustomAuthHandler>();

// Response Validators
services.AddSingleton<IResponseValidator, StatusCodeValidator>();
services.AddSingleton<IResponseValidator, HeaderValidator>();
services.AddSingleton<IResponseValidator, JsonPathValidator>();
services.AddSingleton<IResponseValidator, ResponseTimeValidator>();
services.AddSingleton<IResponseValidator, BodyContainsValidator>();
services.AddSingleton<IResponseValidator, BodyEqualsValidator>();
services.AddSingleton<IResponseValidator, BodyRegexValidator>();

// API Tool (auto-discovered by MCP via [McpServerToolType])
services.AddSingleton<APITool>();
```

## Step 3: Verify Registration

Build and run your MCP server:

```bash
dotnet build
dotnet run
```

Check the logs for successful tool registration. You should see `APITool` registered with 11 methods.

## Step 4: Test with AI Assistant

Try these commands with your AI assistant:

### Test 1: Simple GET Request
```
"Test the GitHub API - get the microsoft user information"
```

Expected: Successfully retrieves GitHub user data with response time.

### Test 2: With Authentication
```
"Send a GET request to my API at localhost:5000/api/data with bearer token ABC123"
```

Expected: Adds Authorization header and makes request.

### Test 3: POST with Body
```
"POST to httpbin.org/post with JSON body: {\"name\": \"test\", \"value\": 123}"
```

Expected: Successfully sends POST with JSON body.

### Test 4: API Test with Validation
```
"Test my API at localhost:5000/health - verify it returns 200 and responds in under 500ms"
```

Expected: Executes test with status code and response time validations.

### Test 5: JSONPath Extraction
```
"Get the login field from this JSON: {\"user\": {\"login\": \"john\", \"id\": 123}}"
```

Expected: Extracts "john" using JSONPath `$.user.login`.

## Step 5: Verify MCP Tools Are Exposed

In your AI assistant, you should see these new MCP tools available:
- `mcp_Sasu_Mcp_send_get_request`
- `mcp_Sasu_Mcp_send_post_request`
- `mcp_Sasu_Mcp_send_put_request`
- `mcp_Sasu_Mcp_send_patch_request`
- `mcp_Sasu_Mcp_send_delete_request`
- `mcp_Sasu_Mcp_send_head_request`
- `mcp_Sasu_Mcp_send_options_request`
- `mcp_Sasu_Mcp_execute_test`
- `mcp_Sasu_Mcp_execute_test_suite`
- `mcp_Sasu_Mcp_parse_json_path`
- `mcp_Sasu_Mcp_inspect_endpoint`

## Common Issues & Solutions

### Issue: "Type or namespace 'HttpUtility' could not be found"

**Solution**: Add the NuGet package:
```bash
dotnet add package System.Web.HttpUtility
```

OR modify the code in `APITool.cs` around line 520 to use manual query string parsing.

### Issue: "No validator found for type 'X'"

**Solution**: Ensure all validators are registered in DI. Check that you've added all 7 validator registrations.

### Issue: "Authentication type 'Bearer' is not supported"

**Solution**: Ensure all authentication handlers are registered in DI. Check that you've added all 4 auth handler registrations.

### Issue: MCP tools not showing up

**Solution**: 
1. Verify `[McpServerToolType]` attribute is present on `APITool` class
2. Check that `APITool` is registered in DI
3. Restart the MCP server
4. Check server logs for registration errors

## Environment Variables (Optional)

You can configure default behavior with these environment variables:

```bash
# Windows (PowerShell)
$env:API_DEFAULT_TIMEOUT = "30"
$env:API_MAX_TIMEOUT = "300"
$env:API_FOLLOW_REDIRECTS = "true"
$env:API_VALIDATE_SSL = "true"
$env:API_MAX_REDIRECTS = "10"

# Linux/Mac
export API_DEFAULT_TIMEOUT=30
export API_MAX_TIMEOUT=300
export API_FOLLOW_REDIRECTS=true
export API_VALIDATE_SSL=true
export API_MAX_REDIRECTS=10
```

## Example: Full Integration in Program.cs

```csharp
using DrSasuMcp.Tools.API;
using DrSasuMcp.Tools.API.Authentication;
using DrSasuMcp.Tools.API.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// ... your existing service registrations ...

// Add API Testing Tool
builder.Services.AddSingleton<IHttpClientFactory, HttpClientFactory>();

// Authentication
builder.Services.AddSingleton<IAuthenticationHandler, BearerAuthHandler>();
builder.Services.AddSingleton<IAuthenticationHandler, BasicAuthHandler>();
builder.Services.AddSingleton<IAuthenticationHandler, ApiKeyAuthHandler>();
builder.Services.AddSingleton<IAuthenticationHandler, CustomAuthHandler>();

// Validators
builder.Services.AddSingleton<IResponseValidator, StatusCodeValidator>();
builder.Services.AddSingleton<IResponseValidator, HeaderValidator>();
builder.Services.AddSingleton<IResponseValidator, JsonPathValidator>();
builder.Services.AddSingleton<IResponseValidator, ResponseTimeValidator>();
builder.Services.AddSingleton<IResponseValidator, BodyContainsValidator>();
builder.Services.AddSingleton<IResponseValidator, BodyEqualsValidator>();
builder.Services.AddSingleton<IResponseValidator, BodyRegexValidator>();

// Tool
builder.Services.AddSingleton<APITool>();

var host = builder.Build();
await host.RunAsync();
```

## Next Steps

1. ✅ Integration complete - start testing!
2. 📖 Read the full [README.md](README.md) for detailed documentation
3. 🔧 Review [IMPLEMENTATION_NOTES.md](IMPLEMENTATION_NOTES.md) for technical details
4. 🚀 Start building API tests with your AI assistant!

## Support

For issues or questions:
1. Check the README.md for detailed documentation
2. Review IMPLEMENTATION_NOTES.md for implementation details
3. Examine the code - it's well-commented and structured

---

**Happy API Testing! 🎉**

