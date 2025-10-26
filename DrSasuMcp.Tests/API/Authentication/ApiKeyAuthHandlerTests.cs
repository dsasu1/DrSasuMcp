using DrSasuMcp.Tools.API.Authentication;
using DrSasuMcp.Tools.API.Models;
using FluentAssertions;
using System;
using System.Linq;
using System.Net.Http;
using Xunit;

namespace DrSasuMcp.Tests.API.Authentication
{
    public class ApiKeyAuthHandlerTests
    {
        private readonly ApiKeyAuthHandler _handler;

        public ApiKeyAuthHandlerTests()
        {
            _handler = new ApiKeyAuthHandler();
        }

        [Fact]
        public void SupportedType_ShouldReturnApiKey()
        {
            // Act
            var result = _handler.SupportedType;

            // Assert
            result.Should().Be(AuthType.ApiKey);
        }

        [Fact]
        public void ApplyAuthentication_WithValidApiKey_ShouldAddCustomHeader()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var config = new AuthenticationConfig
            {
                Type = AuthType.ApiKey,
                ApiKeyHeader = "X-API-Key",
                ApiKeyValue = "test-api-key-123"
            };

            // Act
            _handler.ApplyAuthentication(request, config);

            // Assert
            request.Headers.Should().ContainKey("X-API-Key");
            request.Headers.GetValues("X-API-Key").First().Should().Be("test-api-key-123");
        }

        [Theory]
        [InlineData(null, "value")]
        [InlineData("", "value")]
        [InlineData("   ", "value")]
        [InlineData("header", null)]
        [InlineData("header", "")]
        [InlineData("header", "   ")]
        [InlineData(null, null)]
        public void ApplyAuthentication_WithInvalidApiKey_ShouldThrowArgumentException(
            string? header, string? value)
        {
            // Arrange
            var request = new HttpRequestMessage();
            var config = new AuthenticationConfig
            {
                Type = AuthType.ApiKey,
                ApiKeyHeader = header,
                ApiKeyValue = value
            };

            // Act
            Action act = () => _handler.ApplyAuthentication(request, config);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*API Key header name and value are required*");
        }

        [Fact]
        public void ApplyAuthentication_WithCustomHeaderName_ShouldAddCorrectHeader()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var config = new AuthenticationConfig
            {
                Type = AuthType.ApiKey,
                ApiKeyHeader = "Authorization",
                ApiKeyValue = "CustomApiKey abc123"
            };

            // Act
            _handler.ApplyAuthentication(request, config);

            // Assert
            request.Headers.Should().ContainKey("Authorization");
            request.Headers.GetValues("Authorization").First().Should().Be("CustomApiKey abc123");
        }
    }
}

