using DrSasuMcp.AzureDevOps.AzureDevOps.Utils;
using Xunit;

namespace DrSasuMcp.Tests.AzureDevOps.Utils
{
    public class WikiUrlParserTests
    {
        [Fact]
        public void ParseWikiUrl_PageIdWithSlug_ReturnsPageId()
        {
            // Act
            var result = WikiUrlParser.ParseWikiUrl("https://dev.azure.com/myorg/myproject/_wiki/wikis/myproject.wiki/42/Getting-Started");

            // Assert
            Assert.NotNull(result);
            var (org, project, wiki, pageId, pagePath) = result.Value;
            Assert.Equal("myorg", org);
            Assert.Equal("myproject", project);
            Assert.Equal("myproject.wiki", wiki);
            Assert.Equal(42, pageId);
            Assert.Null(pagePath);
        }

        [Fact]
        public void ParseWikiUrl_PageIdWithoutSlug_ReturnsPageId()
        {
            // Act
            var result = WikiUrlParser.ParseWikiUrl("https://dev.azure.com/myorg/myproject/_wiki/wikis/myproject.wiki/42");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(42, result.Value.pageId);
            Assert.Null(result.Value.pagePath);
        }

        [Fact]
        public void ParseWikiUrl_NestedSlug_ReturnsPageId()
        {
            // Act
            var result = WikiUrlParser.ParseWikiUrl("https://dev.azure.com/myorg/myproject/_wiki/wikis/myproject.wiki/42/Engineering/Onboarding");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(42, result.Value.pageId);
        }

        [Fact]
        public void ParseWikiUrl_PagePathQuery_ReturnsDecodedPath()
        {
            // Act
            var result = WikiUrlParser.ParseWikiUrl("https://dev.azure.com/myorg/myproject/_wiki/wikis/myproject.wiki?pagePath=%2FEngineering%2FOn+boarding&anchor=setup");

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Value.pageId);
            Assert.Equal("/Engineering/On boarding", result.Value.pagePath);
        }

        [Fact]
        public void ParseWikiUrl_PagePathAndPageId_PrefersPagePath()
        {
            // Act
            var result = WikiUrlParser.ParseWikiUrl("https://dev.azure.com/myorg/myproject/_wiki/wikis/myproject.wiki/42/Old-Title?pagePath=%2FNew%20Title");

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Value.pageId);
            Assert.Equal("/New Title", result.Value.pagePath);
        }

        [Fact]
        public void ParseWikiUrl_WikiRoot_ReturnsRootPath()
        {
            // Act
            var result = WikiUrlParser.ParseWikiUrl("https://dev.azure.com/myorg/myproject/_wiki/wikis/myproject.wiki/");

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Value.pageId);
            Assert.Equal("/", result.Value.pagePath);
        }

        [Fact]
        public void ParseWikiUrl_EncodedSegments_AreDecoded()
        {
            // Act
            var result = WikiUrlParser.ParseWikiUrl("https://dev.azure.com/myorg/My%20Project/_wiki/wikis/Team%20Docs/7/Home");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("My Project", result.Value.project);
            Assert.Equal("Team Docs", result.Value.wiki);
        }

        [Fact]
        public void ParseWikiUrl_LegacyHost_ReturnsOrganization()
        {
            // Act
            var result = WikiUrlParser.ParseWikiUrl("https://myorg.visualstudio.com/myproject/_wiki/wikis/myproject.wiki/42/Home");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("myorg", result.Value.organization);
            Assert.Equal("myproject", result.Value.project);
            Assert.Equal(42, result.Value.pageId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("/Engineering/Onboarding")]
        [InlineData("https://dev.azure.com/myorg/myproject/_workitems/edit/1234")]
        [InlineData("https://dev.azure.com/myorg/myproject/_wiki/wikis")]
        [InlineData("https://github.com/myorg/myrepo/wiki/Home")]
        public void ParseWikiUrl_InvalidInput_ReturnsNull(string? url)
        {
            // Act
            var result = WikiUrlParser.ParseWikiUrl(url!);

            // Assert
            Assert.Null(result);
            Assert.False(WikiUrlParser.IsValidWikiUrl(url!));
        }
    }
}
