using DrSasuMcp.Tools.API.Authentication;
using DrSasuMcp.Tools.API.Models;
using FluentAssertions;
using System.Net.Http;
using Xunit;

namespace DrSasuMcp.Tests.API.Authentication
{
    public class BearerAuthHandlerTests
    {
        private readonly BearerAuthHandler _handler;

        public BearerAuthHandlerTests()
        {
            _handler = new BearerAuthHandler();
        }

        [Fact]
        public void SupportedType_ShouldReturnBearer()
        {
            // Act
            var result = _handler.SupportedType;

            // Assert
            result.Should().Be(AuthType.Bearer);
        }

        [Fact]
        public void ApplyAuthentication_WithValidToken_ShouldSetAuthorizationHeader()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var config = new AuthenticationConfig
            {
                Type = AuthType.Bearer,
                Token = "test-token-123"
            };

            // Act
            _handler.ApplyAuthentication(request, config);

            // Assert
            request.Headers.Authorization.Should().NotBeNull();
            request.Headers.Authorization!.Scheme.Should().Be("Bearer");
            request.Headers.Authorization.Parameter.Should().Be("test-token-123");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ApplyAuthentication_WithInvalidToken_ShouldThrowArgumentException(string? token)
        {
            // Arrange
            var request = new HttpRequestMessage();
            var config = new AuthenticationConfig
            {
                Type = AuthType.Bearer,
                Token = token
            };

            // Act
            Action act = () => _handler.ApplyAuthentication(request, config);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Bearer token is required*");
        }
    }
}

