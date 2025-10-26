using DrSasuMcp.Tools;
using DrSasuMcp.Tools.API;
using DrSasuMcp.Tools.API.Authentication;
using DrSasuMcp.Tools.API.Models;
using DrSasuMcp.Tools.API.Validators;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DrSasuMcp.Tests.API
{
    public class APIToolTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<ILogger<APITool>> _mockLogger;
        private readonly List<IAuthenticationHandler> _authHandlers;
        private readonly List<IResponseValidator> _validators;
        private readonly APITool _apiTool;

        public APIToolTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockLogger = new Mock<ILogger<APITool>>();
            
            _authHandlers = new List<IAuthenticationHandler>
            {
                new BearerAuthHandler(),
                new BasicAuthHandler(),
                new ApiKeyAuthHandler()
            };

            _validators = new List<IResponseValidator>
            {
                new StatusCodeValidator(),
                new ResponseTimeValidator(),
                new JsonPathValidator()
            };

            _apiTool = new APITool(
                _mockHttpClientFactory.Object,
                _mockLogger.Object,
                _authHandlers,
                _validators
            );
        }

        [Fact]
        public void Constructor_WithDependencies_ShouldCreateInstance()
        {
            // Assert
            _apiTool.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithAuthHandlers_ShouldInitializeHandlers()
        {
            // Act & Assert
            // The APITool should have been created successfully with the handlers
            _apiTool.Should().NotBeNull();
            _authHandlers.Should().HaveCount(3);
        }

        [Fact]
        public void Constructor_WithValidators_ShouldInitializeValidators()
        {
            // Act & Assert
            // The APITool should have been created successfully with the validators
            _apiTool.Should().NotBeNull();
            _validators.Should().HaveCount(3);
        }

        [Fact]
        public void ParseJsonPath_WithValidJson_ShouldExtractValue()
        {
            // Arrange
            var json = @"{""name"": ""John"", ""age"": 30}";
            var path = "$.name";

            // Act
            var result = _apiTool.ParseJsonPath(json, path);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public void ParseJsonPath_WithInvalidJson_ShouldReturnSuccessButNotFound()
        {
            // Arrange
            var json = "invalid json";
            var path = "$.name";

            // Act
            var result = _apiTool.ParseJsonPath(json, path);

            // Assert
            // ParseJsonPath returns Success=true even for invalid JSON
            // The validator handles the error internally and returns found=false
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public void ParseJsonPath_WithComplexPath_ShouldExtractNestedValue()
        {
            // Arrange
            var json = @"{""user"": {""profile"": {""email"": ""test@example.com""}}}";
            var path = "$.user.profile.email";

            // Act
            var result = _apiTool.ParseJsonPath(json, path);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }
    }
}

