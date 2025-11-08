using DrSasuMcp.Tools.API.Authentication;
using DrSasuMcp.Tools.API.Models;
using DrSasuMcp.Tools.API.Validators;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API
{
    [McpServerToolType]
    public partial class APITool
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<APITool> _logger;
        private readonly Dictionary<AuthType, IAuthenticationHandler> _authHandlers;
        private readonly Dictionary<ValidationType, IResponseValidator> _validators;
        
        // Configuration values (can be overridden by environment variables)
        private readonly int _defaultTimeoutSeconds;
        private readonly int _maxTimeoutSeconds;
        private readonly bool _defaultFollowRedirects;
        private readonly bool _defaultValidateSsl;
        private readonly int _defaultMaxRedirects;

        public APITool(
            IHttpClientFactory httpClientFactory,
            ILogger<APITool> logger,
            IEnumerable<IAuthenticationHandler> authHandlers,
            IEnumerable<IResponseValidator> validators)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _authHandlers = authHandlers.ToDictionary(h => h.SupportedType);
            _validators = validators.ToDictionary(v => v.SupportedType);
            
            // Read configuration from environment variables or use defaults
            _defaultTimeoutSeconds = GetIntFromEnv(EnvApiDefaultTimeout, DefaultTimeoutSeconds);
            _maxTimeoutSeconds = GetIntFromEnv(EnvApiMaxTimeout, MaxTimeoutSeconds);
            _defaultFollowRedirects = GetBoolFromEnv(EnvApiFollowRedirects, DefaultFollowRedirects);
            _defaultValidateSsl = GetBoolFromEnv(EnvApiValidateSsl, DefaultValidateSsl);
            _defaultMaxRedirects = GetIntFromEnv(EnvApiMaxRedirects, DefaultMaxRedirects);
            
            _logger.LogInformation("API Tool initialized with timeout={Timeout}s, maxTimeout={MaxTimeout}s, followRedirects={FollowRedirects}, validateSsl={ValidateSsl}, maxRedirects={MaxRedirects}",
                _defaultTimeoutSeconds, _maxTimeoutSeconds, _defaultFollowRedirects, _defaultValidateSsl, _defaultMaxRedirects);
        }
        
        private static int GetIntFromEnv(string varName, int defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(varName);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }
        
        private static bool GetBoolFromEnv(string varName, bool defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(varName);
            if (string.IsNullOrEmpty(value))
                return defaultValue;
                
            return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        #region HTTP Request Methods

        [McpServerTool(
            Title = "HTTP: Send GET Request",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Executes an HTTP GET request to the specified URL")]
        public async Task<OperationResult> HTTPSendGetRequest(
            [Description("The URL to send the request to")] string url,
            [Description("Optional headers as JSON object")] string? headers = null,
            [Description("Optional query parameters as JSON object")] string? queryParams = null,
            [Description("Optional authentication config as JSON")] string? auth = null,
            [Description("Request timeout in seconds")] int timeoutSeconds = 0,
            [Description("Follow redirects")] bool? followRedirects = null,
            [Description("Validate SSL certificates")] bool? validateSsl = null)
        {
            return await ExecuteHttpRequest(
                HttpMethod.Get,
                url,
                body: null,
                contentType: null,
                headers,
                queryParams,
                auth,
                timeoutSeconds > 0 ? timeoutSeconds : _defaultTimeoutSeconds,
                followRedirects ?? _defaultFollowRedirects,
                validateSsl ?? _defaultValidateSsl);
        }

        [McpServerTool(
            Title = "HTTP: Send POST Request",
            ReadOnly = false,
            Idempotent = false,
            Destructive = false),
            Description("Executes an HTTP POST request with an optional body")]
        public async Task<OperationResult> HTTPSendPostRequest(
            [Description("The URL to send the request to")] string url,
            [Description("Request body content")] string? body = null,
            [Description("Content type (e.g., application/json)")] string contentType = ContentTypeJson,
            [Description("Optional headers as JSON object")] string? headers = null,
            [Description("Optional query parameters as JSON object")] string? queryParams = null,
            [Description("Optional authentication config as JSON")] string? auth = null,
            [Description("Request timeout in seconds")] int timeoutSeconds = 0,
            [Description("Follow redirects")] bool? followRedirects = null,
            [Description("Validate SSL certificates")] bool? validateSsl = null)
        {
            return await ExecuteHttpRequest(
                HttpMethod.Post,
                url,
                body,
                contentType,
                headers,
                queryParams,
                auth,
                timeoutSeconds > 0 ? timeoutSeconds : _defaultTimeoutSeconds,
                followRedirects ?? _defaultFollowRedirects,
                validateSsl ?? _defaultValidateSsl);
        }

        [McpServerTool(
            Title = "HTTP: Send PUT Request",
            ReadOnly = false,
            Idempotent = true,
            Destructive = false),
            Description("Executes an HTTP PUT request with an optional body")]
        public async Task<OperationResult> HTTPSendPutRequest(
            [Description("The URL to send the request to")] string url,
            [Description("Request body content")] string? body = null,
            [Description("Content type (e.g., application/json)")] string contentType = ContentTypeJson,
            [Description("Optional headers as JSON object")] string? headers = null,
            [Description("Optional query parameters as JSON object")] string? queryParams = null,
            [Description("Optional authentication config as JSON")] string? auth = null,
            [Description("Request timeout in seconds")] int timeoutSeconds = 0,
            [Description("Follow redirects")] bool? followRedirects = null,
            [Description("Validate SSL certificates")] bool? validateSsl = null)
        {
            return await ExecuteHttpRequest(
                HttpMethod.Put,
                url,
                body,
                contentType,
                headers,
                queryParams,
                auth,
                timeoutSeconds > 0 ? timeoutSeconds : _defaultTimeoutSeconds,
                followRedirects ?? _defaultFollowRedirects,
                validateSsl ?? _defaultValidateSsl);
        }

        [McpServerTool(
            Title = "HTTP: Send PATCH Request",
            ReadOnly = false,
            Idempotent = false,
            Destructive = false),
            Description("Executes an HTTP PATCH request with an optional body")]
        public async Task<OperationResult> HTTPSendPatchRequest(
            [Description("The URL to send the request to")] string url,
            [Description("Request body content")] string? body = null,
            [Description("Content type (e.g., application/json)")] string contentType = ContentTypeJson,
            [Description("Optional headers as JSON object")] string? headers = null,
            [Description("Optional query parameters as JSON object")] string? queryParams = null,
            [Description("Optional authentication config as JSON")] string? auth = null,
            [Description("Request timeout in seconds")] int timeoutSeconds = 0,
            [Description("Follow redirects")] bool? followRedirects = null,
            [Description("Validate SSL certificates")] bool? validateSsl = null)
        {
            return await ExecuteHttpRequest(
                HttpMethod.Patch,
                url,
                body,
                contentType,
                headers,
                queryParams,
                auth,
                timeoutSeconds > 0 ? timeoutSeconds : _defaultTimeoutSeconds,
                followRedirects ?? _defaultFollowRedirects,
                validateSsl ?? _defaultValidateSsl);
        }

        [McpServerTool(
            Title = "HTTP: Send DELETE Request",
            ReadOnly = false,
            Idempotent = true,
            Destructive = true),
            Description("Executes an HTTP DELETE request")]
        public async Task<OperationResult> HTTPSendDeleteRequest(
            [Description("The URL to send the request to")] string url,
            [Description("Optional headers as JSON object")] string? headers = null,
            [Description("Optional query parameters as JSON object")] string? queryParams = null,
            [Description("Optional authentication config as JSON")] string? auth = null,
            [Description("Request timeout in seconds")] int timeoutSeconds = 0,
            [Description("Follow redirects")] bool? followRedirects = null,
            [Description("Validate SSL certificates")] bool? validateSsl = null)
        {
            return await ExecuteHttpRequest(
                HttpMethod.Delete,
                url,
                body: null,
                contentType: null,
                headers,
                queryParams,
                auth,
                timeoutSeconds > 0 ? timeoutSeconds : _defaultTimeoutSeconds,
                followRedirects ?? _defaultFollowRedirects,
                validateSsl ?? _defaultValidateSsl);
        }

        [McpServerTool(
            Title = "HTTP: Send HEAD Request",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Executes an HTTP HEAD request to check endpoint availability without downloading the body")]
        public async Task<OperationResult> HTTPSendHeadRequest(
            [Description("The URL to send the request to")] string url,
            [Description("Optional headers as JSON object")] string? headers = null,
            [Description("Optional authentication config as JSON")] string? auth = null,
            [Description("Request timeout in seconds")] int timeoutSeconds = 0,
            [Description("Validate SSL certificates")] bool? validateSsl = null)
        {
            return await ExecuteHttpRequest(
                HttpMethod.Head,
                url,
                body: null,
                contentType: null,
                headers,
                queryParamsJson: null,
                auth,
                timeoutSeconds > 0 ? timeoutSeconds : _defaultTimeoutSeconds,
                followRedirects: false,
                validateSsl ?? _defaultValidateSsl);
        }

        [McpServerTool(
            Title = "HTTP: Send OPTIONS Request",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Executes an HTTP OPTIONS request to discover allowed HTTP methods for an endpoint")]
        public async Task<OperationResult> SendOptionsRequest(
            [Description("The URL to send the request to")] string url,
            [Description("Optional headers as JSON object")] string? headers = null,
            [Description("Optional authentication config as JSON")] string? auth = null,
            [Description("Request timeout in seconds")] int timeoutSeconds = 0,
            [Description("Validate SSL certificates")] bool? validateSsl = null)
        {
            return await ExecuteHttpRequest(
                HttpMethod.Options,
                url,
                body: null,
                contentType: null,
                headers,
                queryParamsJson: null,
                auth,
                timeoutSeconds > 0 ? timeoutSeconds : _defaultTimeoutSeconds,
                followRedirects: false,
                validateSsl ?? _defaultValidateSsl);
        }

        #endregion

        #region Testing Methods

        [McpServerTool(
            Title = "HTTP: Execute API Test",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Executes an API test with validation rules and returns detailed test results")]
        public async Task<OperationResult> HTTPExecuteTest(
            [Description("HTTP method (GET, POST, PUT, PATCH, DELETE)")] string method,
            [Description("The URL to test")] string url,
            [Description("Optional request body")] string? body = null,
            [Description("Content type for request body")] string contentType = ContentTypeJson,
            [Description("Optional headers as JSON object")] string? headers = null,
            [Description("Optional query parameters as JSON object")] string? queryParams = null,
            [Description("Optional authentication config as JSON")] string? auth = null,
            [Description("Validation rules as JSON array")] string? validationRules = null,
            [Description("Expected status code")] int? expectedStatus = null,
            [Description("Maximum response time in milliseconds")] int? maxResponseTimeMs = null,
            [Description("Request timeout in seconds")] int timeoutSeconds = 0)
        {
            try
            {
                // Parse HTTP method
                var httpMethod = ParseHttpMethod(method);

                // Execute the request
                var requestResult = await ExecuteHttpRequest(
                    httpMethod,
                    url,
                    body,
                    contentType,
                    headers,
                    queryParams,
                    auth,
                    timeoutSeconds > 0 ? timeoutSeconds : _defaultTimeoutSeconds,
                    _defaultFollowRedirects,
                    _defaultValidateSsl);

                if (!requestResult.Success)
                {
                    return new OperationResult(false, requestResult.Error);
                }

                var response = requestResult.Data as HttpResponseResult;
                if (response == null)
                {
                    return new OperationResult(false, "Failed to get response data");
                }

                // Build validation rules
                var rules = new List<ValidationRule>();

                // Add expected status rule if provided
                if (expectedStatus.HasValue)
                {
                    rules.Add(new ValidationRule
                    {
                        Type = ValidationType.StatusCode,
                        Operator = "equals",
                        ExpectedValue = expectedStatus.Value,
                        Description = "Expected status code"
                    });
                }

                // Add response time rule if provided
                if (maxResponseTimeMs.HasValue)
                {
                    rules.Add(new ValidationRule
                    {
                        Type = ValidationType.ResponseTime,
                        Operator = "lessThan",
                        ExpectedValue = maxResponseTimeMs.Value,
                        Description = "Maximum response time"
                    });
                }

                // Parse additional validation rules if provided
                if (!string.IsNullOrWhiteSpace(validationRules))
                {
                    var additionalRules = JsonSerializer.Deserialize<List<ValidationRule>>(
                        validationRules,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (additionalRules != null)
                    {
                        rules.AddRange(additionalRules);
                    }
                }

                // Execute validations
                var testResult = ValidateResponse(response, rules);

                return new OperationResult(true, null, 0, testResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExecuteTest failed: {Message}", ex.Message);
                return new OperationResult(false, ex.Message);
            }
        }

        [McpServerTool(
            Title = "HTTP: Execute Test Suite",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Executes multiple API tests in sequence and returns aggregated results")]
        public async Task<OperationResult> HTTPExecuteTestSuite(
            [Description("Base URL for all tests")] string? baseUrl = null,
            [Description("Test suite configuration as JSON")] string tests = "[]",
            [Description("Stop execution on first failure")] bool stopOnFailure = false)
        {
            try
            {
                var testConfigs = JsonSerializer.Deserialize<List<TestConfig>>(
                    tests,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (testConfigs == null || !testConfigs.Any())
                {
                    return new OperationResult(false, "No tests provided");
                }

                var results = new List<object>();
                var totalTests = testConfigs.Count;
                var passedTests = 0;
                var failedTests = 0;
                var totalTime = 0L;

                foreach (var testConfig in testConfigs)
                {
                    var testUrl = string.IsNullOrWhiteSpace(testConfig.Url)
                        ? $"{baseUrl?.TrimEnd('/')}/{testConfig.Path.TrimStart('/')}"
                        : testConfig.Url;

                    var httpMethod = ParseHttpMethod(testConfig.Method);

                    var requestResult = await ExecuteHttpRequest(
                        httpMethod,
                        testUrl,
                        testConfig.Body,
                        ContentTypeJson,
                        testConfig.Headers != null ? JsonSerializer.Serialize(testConfig.Headers) : null,
                        testConfig.QueryParameters != null ? JsonSerializer.Serialize(testConfig.QueryParameters) : null,
                        testConfig.Authentication != null ? JsonSerializer.Serialize(testConfig.Authentication) : null,
                        _defaultTimeoutSeconds,
                        _defaultFollowRedirects,
                        _defaultValidateSsl);

                    if (!requestResult.Success)
                    {
                        failedTests++;
                        results.Add(new
                        {
                            name = testConfig.Name,
                            passed = false,
                            error = requestResult.Error
                        });

                        if (stopOnFailure)
                            break;
                        
                        continue;
                    }

                    var response = requestResult.Data as HttpResponseResult;
                    if (response == null)
                    {
                        failedTests++;
                        results.Add(new
                        {
                            name = testConfig.Name,
                            passed = false,
                            error = "Failed to get response data"
                        });

                        if (stopOnFailure)
                            break;
                        
                        continue;
                    }

                    totalTime += response.ResponseTimeMs;

                    // Build validation rules
                    var rules = new List<ValidationRule>();

                    if (testConfig.ExpectedStatus.HasValue)
                    {
                        rules.Add(new ValidationRule
                        {
                            Type = ValidationType.StatusCode,
                            Operator = "equals",
                            ExpectedValue = testConfig.ExpectedStatus.Value
                        });
                    }

                    if (testConfig.MaxResponseTimeMs.HasValue)
                    {
                        rules.Add(new ValidationRule
                        {
                            Type = ValidationType.ResponseTime,
                            Operator = "lessThan",
                            ExpectedValue = testConfig.MaxResponseTimeMs.Value
                        });
                    }

                    if (testConfig.Validations != null)
                    {
                        rules.AddRange(testConfig.Validations);
                    }

                    var testResult = ValidateResponse(response, rules);

                    if (testResult.TestPassed)
                    {
                        passedTests++;
                    }
                    else
                    {
                        failedTests++;
                    }

                    results.Add(new
                    {
                        name = testConfig.Name,
                        passed = testResult.TestPassed,
                        responseTime = response.ResponseTimeMs,
                        statusCode = response.StatusCode,
                        validationResults = testResult.ValidationResults
                    });

                    if (stopOnFailure && !testResult.TestPassed)
                        break;
                }

                return new OperationResult(true, null, 0, new
                {
                    totalTests,
                    passedTests,
                    failedTests,
                    totalTimeMs = totalTime,
                    results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExecuteTestSuite failed: {Message}", ex.Message);
                return new OperationResult(false, ex.Message);
            }
        }

        #endregion

        #region Utility Methods

        [McpServerTool(
            Title = "JSON: Parse JSON Path",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Extracts a value from JSON using a JSONPath expression")]
        public OperationResult JSONParseJsonPath(
            [Description("JSON content to parse")] string json,
            [Description("JSONPath expression (e.g., $.users[0].name)")] string path)
        {
            try
            {
                var validator = new JsonPathValidator();
                var dummyResponse = new HttpResponseResult { Body = json };
                var dummyRule = new ValidationRule { Target = path, Operator = "exists" };

                var result = validator.Validate(dummyResponse, dummyRule);

                return new OperationResult(true, null, 0, new
                {
                    value = result.ActualValue,
                    path,
                    found = result.IsValid
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ParseJsonPath failed: {Message}", ex.Message);
                return new OperationResult(false, ex.Message);
            }
        }

        [McpServerTool(
            Title = "HTTP: Inspect Endpoint",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Performs a detailed inspection of an endpoint including response headers, timing, and metadata")]
        public async Task<OperationResult> HTTPInspectEndpoint(
            [Description("The URL to inspect")] string url,
            [Description("Optional authentication config as JSON")] string? auth = null)
        {
            try
            {
                var requestResult = await ExecuteHttpRequest(
                    HttpMethod.Get,
                    url,
                    body: null,
                    contentType: null,
                    headersJson: null,
                    queryParamsJson: null,
                    auth,
                    _defaultTimeoutSeconds,
                    followRedirects: false,
                    _defaultValidateSsl);

                if (!requestResult.Success)
                {
                    return new OperationResult(false, requestResult.Error);
                }

                var response = requestResult.Data as HttpResponseResult;
                if (response == null)
                {
                    return new OperationResult(false, "Failed to get response data");
                }

                var inspection = new
                {
                    url,
                    statusCode = response.StatusCode,
                    statusDescription = response.StatusDescription,
                    responseTimeMs = response.ResponseTimeMs,
                    contentLength = response.ContentLength,
                    contentType = response.ContentType,
                    headers = response.Headers,
                    timestamp = response.Timestamp,
                    isSuccess = response.IsSuccess
                };

                return new OperationResult(true, null, 0, inspection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InspectEndpoint failed: {Message}", ex.Message);
                return new OperationResult(false, ex.Message);
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task<OperationResult> ExecuteHttpRequest(
            HttpMethod method,
            string url,
            string? body,
            string? contentType,
            string? headersJson,
            string? queryParamsJson,
            string? authJson,
            int timeoutSeconds,
            bool followRedirects,
            bool validateSsl)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Validate and construct URL with query parameters
                var uriBuilder = new UriBuilder(url);
                if (!string.IsNullOrWhiteSpace(queryParamsJson))
                {
                    var queryParams = JsonSerializer.Deserialize<Dictionary<string, string>>(
                        queryParamsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (queryParams != null && queryParams.Any())
                    {
                        var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                        foreach (var param in queryParams)
                        {
                            query[param.Key] = param.Value;
                        }
                        uriBuilder.Query = query.ToString();
                    }
                }

                // Create HTTP client
                using var client = _httpClientFactory.CreateClient(timeoutSeconds, followRedirects, validateSsl);

                // Create request message
                var request = new HttpRequestMessage(method, uriBuilder.Uri);

                // Add headers
                if (!string.IsNullOrWhiteSpace(headersJson))
                {
                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(
                        headersJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                }

                // Add authentication
                if (!string.IsNullOrWhiteSpace(authJson))
                {
                    var authConfig = JsonSerializer.Deserialize<AuthenticationConfig>(
                        authJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (authConfig != null && authConfig.Type != AuthType.None)
                    {
                        if (_authHandlers.TryGetValue(authConfig.Type, out var handler))
                        {
                            handler.ApplyAuthentication(request, authConfig);
                        }
                        else
                        {
                            return new OperationResult(false, $"Authentication type '{authConfig.Type}' is not supported");
                        }
                    }
                }

                // Add body for applicable methods
                if (!string.IsNullOrWhiteSpace(body) && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
                {
                    var actualContentType = contentType ?? ContentTypeJson;
                    request.Content = new StringContent(body, Encoding.UTF8, actualContentType);
                }

                // Execute request
                var response = await client.SendAsync(request);
                stopwatch.Stop();

                // Build response result
                var result = new HttpResponseResult
                {
                    StatusCode = (int)response.StatusCode,
                    StatusDescription = response.ReasonPhrase ?? string.Empty,
                    Headers = response.Headers
                        .Concat(response.Content.Headers)
                        .ToDictionary(
                            h => h.Key,
                            h => string.Join(", ", h.Value)),
                    Body = await response.Content.ReadAsStringAsync(),
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    ContentLength = response.Content.Headers.ContentLength ?? 0,
                    ContentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty,
                    IsSuccess = response.IsSuccessStatusCode,
                    Timestamp = DateTime.UtcNow
                };

                return new OperationResult(true, null, 0, result);
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "HTTP request failed: {Message}", ex.Message);
                return new OperationResult(false, $"HTTP request failed: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Request timed out: {Message}", ex.Message);
                return new OperationResult(false, $"Request timed out after {timeoutSeconds} seconds");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "ExecuteHttpRequest failed: {Message}", ex.Message);
                return new OperationResult(false, ex.Message);
            }
        }

        private TestResult ValidateResponse(HttpResponseResult response, List<ValidationRule> rules)
        {
            var result = new TestResult
            {
                ResponseTimeMs = response.ResponseTimeMs,
                Response = response,
                TotalValidations = rules.Count
            };

            foreach (var rule in rules)
            {
                if (_validators.TryGetValue(rule.Type, out var validator))
                {
                    var validationResult = validator.Validate(response, rule);
                    result.ValidationResults.Add(validationResult);

                    if (validationResult.IsValid)
                    {
                        result.PassedValidations++;
                    }
                    else
                    {
                        result.FailedValidations++;
                    }
                }
                else
                {
                    result.FailedValidations++;
                    result.ValidationResults.Add(new ValidationResult
                    {
                        IsValid = false,
                        Message = $"Validator not found for type '{rule.Type}'",
                        ValidationType = rule.Type
                    });
                }
            }

            result.TestPassed = result.FailedValidations == 0;
            return result;
        }

        private HttpMethod ParseHttpMethod(string method)
        {
            return method.ToUpperInvariant() switch
            {
                "GET" => HttpMethod.Get,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                "PATCH" => HttpMethod.Patch,
                "DELETE" => HttpMethod.Delete,
                "HEAD" => HttpMethod.Head,
                "OPTIONS" => HttpMethod.Options,
                _ => throw new ArgumentException($"Unsupported HTTP method: {method}")
            };
        }

        #endregion
    }
}

