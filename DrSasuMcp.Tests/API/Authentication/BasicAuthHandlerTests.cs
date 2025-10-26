using DrSasuMcp.Tools.API.Authentication;
using DrSasuMcp.Tools.API.Models;
using FluentAssertions;
using System;
using System.Net.Http;
using System.Text;
using Xunit;

namespace DrSasuMcp.Tests.API.Authentication
{
    public class BasicAuthHandlerTests
    {
        private readonly BasicAuthHandler _handler;

        public BasicAuthHandlerTests()
        {
            _handler = new BasicAuthHandler();
        }

        [Fact]
        public void SupportedType_ShouldReturnBasic()
        {
            // Act
            var result = _handler.SupportedType;

            // Assert
            result.Should().Be(AuthType.Basic);
        }

        [Fact]
        public void ApplyAuthentication_WithValidCredentials_ShouldSetAuthorizationHeader()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var config = new AuthenticationConfig
            {
                Type = AuthType.Basic,
                Username = "testuser",
                Password = "testpass"
            };
            var expectedCredentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes("testuser:testpass")
            );

            // Act
            _handler.ApplyAuthentication(request, config);

            // Assert
            request.Headers.Authorization.Should().NotBeNull();
            request.Headers.Authorization!.Scheme.Should().Be("Basic");
            request.Headers.Authorization.Parameter.Should().Be(expectedCredentials);
        }

        [Theory]
        [InlineData(null, "password")]
        [InlineData("", "password")]
        [InlineData("   ", "password")]
        [InlineData("username", null)]
        [InlineData("username", "")]
        [InlineData("username", "   ")]
        [InlineData(null, null)]
        public void ApplyAuthentication_WithInvalidCredentials_ShouldThrowArgumentException(
            string? username, string? password)
        {
            // Arrange
            var request = new HttpRequestMessage();
            var config = new AuthenticationConfig
            {
                Type = AuthType.Basic,
                Username = username,
                Password = password
            };

            // Act
            Action act = () => _handler.ApplyAuthentication(request, config);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Username and password are required*");
        }

        [Fact]
        public void ApplyAuthentication_WithSpecialCharacters_ShouldEncodeCorrectly()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var config = new AuthenticationConfig
            {
                Type = AuthType.Basic,
                Username = "user@example.com",
                Password = "p@$$w0rd!"
            };
            var expectedCredentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes("user@example.com:p@$$w0rd!")
            );

            // Act
            _handler.ApplyAuthentication(request, config);

            // Assert
            request.Headers.Authorization!.Parameter.Should().Be(expectedCredentials);
        }
    }
}

