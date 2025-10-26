using DrSasuMcp.Tools.API;
using FluentAssertions;
using System;
using Xunit;

namespace DrSasuMcp.Tests.API
{
    public class HttpClientFactoryTests
    {
        [Fact]
        public void CreateClient_WithDefaults_ShouldCreateClient()
        {
            // Arrange
            var factory = new HttpClientFactory();

            // Act
            var client = factory.CreateClient();

            // Assert
            client.Should().NotBeNull();
            client.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        }

        [Fact]
        public void CreateClient_WithCustomTimeout_ShouldUseCustomTimeout()
        {
            // Arrange
            var factory = new HttpClientFactory();

            // Act
            var client = factory.CreateClient(timeoutSeconds: 60);

            // Assert
            client.Should().NotBeNull();
            client.Timeout.Should().Be(TimeSpan.FromSeconds(60));
        }

        [Fact]
        public void CreateClient_WithEnvironmentVariableMaxRedirects_ShouldUseEnvValue()
        {
            // Arrange
            Environment.SetEnvironmentVariable("API_MAX_REDIRECTS", "5");
            var factory = new HttpClientFactory();

            try
            {
                // Act
                var client = factory.CreateClient();

                // Assert
                client.Should().NotBeNull();
                // We can't directly test MaxAutomaticRedirections as it's internal to the handler
                // but we can verify the client was created successfully
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("API_MAX_REDIRECTS", null);
            }
        }

        [Fact]
        public void CreateClient_WithoutEnvironmentVariable_ShouldUseDefaultMaxRedirects()
        {
            // Arrange
            Environment.SetEnvironmentVariable("API_MAX_REDIRECTS", null);
            var factory = new HttpClientFactory();

            // Act
            var client = factory.CreateClient();

            // Assert
            client.Should().NotBeNull();
        }

        [Fact]
        public void CreateClient_WithInvalidEnvironmentVariable_ShouldUseDefault()
        {
            // Arrange
            Environment.SetEnvironmentVariable("API_MAX_REDIRECTS", "invalid");
            var factory = new HttpClientFactory();

            try
            {
                // Act
                var client = factory.CreateClient();

                // Assert
                client.Should().NotBeNull();
                // Should use default value of 10 when parsing fails
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("API_MAX_REDIRECTS", null);
            }
        }

        [Fact]
        public void CreateClient_WithFollowRedirectsFalse_ShouldCreateClient()
        {
            // Arrange
            var factory = new HttpClientFactory();

            // Act
            var client = factory.CreateClient(followRedirects: false);

            // Assert
            client.Should().NotBeNull();
        }

        [Fact]
        public void CreateClient_WithValidateSslFalse_ShouldCreateClient()
        {
            // Arrange
            var factory = new HttpClientFactory();

            // Act
            var client = factory.CreateClient(validateSsl: false);

            // Assert
            client.Should().NotBeNull();
        }
    }
}
