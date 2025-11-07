using DrSasuMcp.Tools.AzureDevOps.Utils;
using Xunit;

namespace DrSasuMcp.Tests.AzureDevOps.Utils
{
    public class PrUrlParserTests
    {
        [Fact]
        public void ParsePrUrl_ValidUrl_ReturnsComponents()
        {
            // Arrange
            var url = "https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123";

            // Act
            var result = PrUrlParser.ParsePrUrl(url);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("myorg", result.Value.organization);
            Assert.Equal("myproject", result.Value.project);
            Assert.Equal("myrepo", result.Value.repository);
            Assert.Equal(123, result.Value.pullRequestId);
        }

        [Theory]
        [InlineData("https://dev.azure.com/contoso/MyProject/_git/MainRepo/pullrequest/456")]
        [InlineData("https://dev.azure.com/test-org/test_project/_git/test.repo/pullrequest/1")]
        [InlineData("https://dev.azure.com/a/b/_git/c/pullrequest/999999")]
        public void ParsePrUrl_VariousValidUrls_ReturnsComponents(string url)
        {
            // Act
            var result = PrUrlParser.ParsePrUrl(url);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Value.organization);
            Assert.NotEmpty(result.Value.project);
            Assert.NotEmpty(result.Value.repository);
            Assert.True(result.Value.pullRequestId > 0);
        }

        [Theory]
        [InlineData("https://github.com/owner/repo/pull/123")]
        [InlineData("invalid-url")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("https://dev.azure.com/org/project")]
        [InlineData("https://dev.azure.com/org/project/_git/repo")]
        public void ParsePrUrl_InvalidUrl_ReturnsNull(string? url)
        {
            // Act
            var result = PrUrlParser.ParsePrUrl(url!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void IsValidPrUrl_ValidUrl_ReturnsTrue()
        {
            // Arrange
            var url = "https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123";

            // Act
            var result = PrUrlParser.IsValidPrUrl(url);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("https://github.com/owner/repo/pull/123")]
        [InlineData("invalid-url")]
        [InlineData("")]
        public void IsValidPrUrl_InvalidUrl_ReturnsFalse(string url)
        {
            // Act
            var result = PrUrlParser.IsValidPrUrl(url);

            // Assert
            Assert.False(result);
        }
    }
}

