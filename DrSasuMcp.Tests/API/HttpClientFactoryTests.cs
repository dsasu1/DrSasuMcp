using DrSasuMcp.Tools.API;
using FluentAssertions;
using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace DrSasuMcp.Tests.API
{
    public class HttpClientFactoryTests
    {
        private readonly HttpClientFactory _factory;

        public HttpClientFactoryTests()
        {
            _factory = new HttpClientFactory();
        }

        [Fact]
        public void CreateClient_WithDefaultParameters_ShouldCreateClient()
        {
            // Act
            var client = _factory.CreateClient();

            // Assert
            client.Should().NotBeNull();
            client.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        }

        [Fact]
        public void CreateClient_WithCustomTimeout_ShouldSetTimeout()
        {
            // Act
            var client = _factory.CreateClient(timeoutSeconds: 60);

            // Assert
            client.Timeout.Should().Be(TimeSpan.FromSeconds(60));
        }

        [Fact]
        public void CreateClient_WithFollowRedirects_ShouldConfigureHandler()
        {
            // Act
            var client = _factory.CreateClient(followRedirects: true);

            // Assert
            client.Should().NotBeNull();
        }

        [Fact]
        public void CreateClient_WithoutFollowRedirects_ShouldConfigureHandler()
        {
            // Act
            var client = _factory.CreateClient(followRedirects: false);

            // Assert
            client.Should().NotBeNull();
        }

        [Fact]
        public void CreateClient_WithSslValidationDisabled_ShouldCreateClient()
        {
            // Act
            var client = _factory.CreateClient(validateSsl: false);

            // Assert
            client.Should().NotBeNull();
        }

        [Fact]
        public void CreateClient_WithAllParameters_ShouldCreateConfiguredClient()
        {
            // Act
            var client = _factory.CreateClient(
                timeoutSeconds: 45,
                followRedirects: false,
                validateSsl: false
            );

            // Assert
            client.Should().NotBeNull();
            client.Timeout.Should().Be(TimeSpan.FromSeconds(45));
        }

        [Fact]
        public void CreateClient_MultipleTimes_ShouldCreateSeparateInstances()
        {
            // Act
            var client1 = _factory.CreateClient();
            var client2 = _factory.CreateClient();

            // Assert
            client1.Should().NotBeSameAs(client2);
        }
    }
}

