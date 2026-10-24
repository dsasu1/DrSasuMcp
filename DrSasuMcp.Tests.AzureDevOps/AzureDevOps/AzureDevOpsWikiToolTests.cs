using DrSasuMcp.AzureDevOps.AzureDevOps;
using DrSasuMcp.AzureDevOps.AzureDevOps.Analyzers;
using DrSasuMcp.AzureDevOps.AzureDevOps.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DrSasuMcp.Tests.AzureDevOps
{
    public class AzureDevOpsWikiToolTests
    {
        private readonly Mock<IAzureDevOpsService> _serviceMock = new();
        private readonly AzureDevOpsTool _tool;

        public AzureDevOpsWikiToolTests()
        {
            _tool = new AzureDevOpsTool(
                _serviceMock.Object,
                new Mock<IDiffService>().Object,
                Array.Empty<ICodeAnalyzer>(),
                new Mock<ILogger<AzureDevOpsTool>>().Object);
        }

        [Fact]
        public async Task AzureListWikis_WithArguments_ReturnsWikis()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetWikisAsync("myorg", "myproject", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<WikiInfo> { new() { Name = "myproject.wiki" }, new() { Name = "Api Docs" } });

            // Act
            var result = await _tool.AzureListWikis("myproject", "myorg");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.RowsAffected);
            Assert.Equal(2, Assert.IsType<List<WikiInfo>>(result.Data).Count);
        }

        [Fact]
        public async Task AzureListWikis_NoOrganization_FallsBackToDefault()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetDefaultOrganization()).Returns("defaultorg");
            _serviceMock
                .Setup(s => s.GetWikisAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<WikiInfo>());

            // Act
            var result = await _tool.AzureListWikis();

            // Assert
            Assert.True(result.Success);
            _serviceMock.Verify(s => s.GetWikisAsync("defaultorg", null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AzureListWikis_NoOrganizationAvailable_ReturnsError()
        {
            // Act
            var result = await _tool.AzureListWikis("myproject");

            // Assert
            Assert.False(result.Success);
            Assert.Contains(AzureDevOpsToolConstants.EnvAzureDevOpsOrg, result.Error);
        }

        [Fact]
        public async Task AzureGetWikiPage_UrlWithPageId_UsesUrlValuesOverArguments()
        {
            // Arrange
            SetupGetWikiPage();

            // Act
            var result = await _tool.AzureGetWikiPage(
                "https://dev.azure.com/urlorg/urlproject/_wiki/wikis/url.wiki/42/Home",
                wiki: "argwiki",
                project: "argproject",
                organization: "argorg",
                pageId: 7);

            // Assert
            Assert.True(result.Success);
            _serviceMock.Verify(s => s.GetWikiPageAsync(
                "urlorg", "urlproject", "url.wiki", null, 42, false, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AzureGetWikiPage_UrlWithPagePath_PassesPathAndOptions()
        {
            // Arrange
            SetupGetWikiPage();

            // Act
            var result = await _tool.AzureGetWikiPage(
                "https://dev.azure.com/myorg/myproject/_wiki/wikis/Api%20Docs?pagePath=%2FOverview",
                includeSubPages: true,
                version: "main");

            // Assert
            Assert.True(result.Success);
            _serviceMock.Verify(s => s.GetWikiPageAsync(
                "myorg", "myproject", "Api Docs", "/Overview", null, true, "main", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AzureGetWikiPage_PathWithArguments_UsesDefaultOrganization()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetDefaultOrganization()).Returns("defaultorg");
            SetupGetWikiPage();

            // Act
            var result = await _tool.AzureGetWikiPage("/Engineering/Onboarding", wiki: "myproject.wiki", project: "myproject");

            // Assert
            Assert.True(result.Success);
            _serviceMock.Verify(s => s.GetWikiPageAsync(
                "defaultorg", "myproject", "myproject.wiki", "/Engineering/Onboarding", null, false, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AzureGetWikiPage_PageIdArgument_PassesId()
        {
            // Arrange
            SetupGetWikiPage();

            // Act
            var result = await _tool.AzureGetWikiPage(wiki: "myproject.wiki", project: "myproject", organization: "myorg", pageId: 42);

            // Assert
            Assert.True(result.Success);
            _serviceMock.Verify(s => s.GetWikiPageAsync(
                "myorg", "myproject", "myproject.wiki", null, 42, false, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(null, "myproject")]
        [InlineData("myproject.wiki", null)]
        public async Task AzureGetWikiPage_PathWithoutWikiOrProject_ReturnsError(string? wiki, string? project)
        {
            // Act
            var result = await _tool.AzureGetWikiPage("/Home", wiki: wiki, project: project, organization: "myorg");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Wiki and project are required", result.Error);
            VerifyServiceNotCalled();
        }

        [Fact]
        public async Task AzureGetWikiPage_NoOrganizationAvailable_ReturnsError()
        {
            // Act
            var result = await _tool.AzureGetWikiPage("/Home", wiki: "myproject.wiki", project: "myproject");

            // Assert
            Assert.False(result.Success);
            Assert.Contains(AzureDevOpsToolConstants.EnvAzureDevOpsOrg, result.Error);
            VerifyServiceNotCalled();
        }

        [Fact]
        public async Task AzureGetWikiPage_InvalidUrl_ReturnsError()
        {
            // Act
            var result = await _tool.AzureGetWikiPage("https://dev.azure.com/myorg/myproject/_workitems/edit/1234");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Invalid Azure DevOps wiki URL", result.Error);
            VerifyServiceNotCalled();
        }

        [Fact]
        public async Task AzureGetWikiPage_NonPositivePageId_ReturnsError()
        {
            // Act
            var result = await _tool.AzureGetWikiPage(wiki: "w", project: "p", organization: "o", pageId: 0);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("positive", result.Error);
            VerifyServiceNotCalled();
        }

        [Fact]
        public async Task AzureGetWikiPage_Unauthorized_ReturnsAuthenticationError()
        {
            // Arrange
            SetupGetWikiPageThrows(new UnauthorizedAccessException("PAT lacks Wiki (Read)"));

            // Act
            var result = await _tool.AzureGetWikiPage("/Home", wiki: "w", project: "p", organization: "o");

            // Assert
            Assert.False(result.Success);
            Assert.StartsWith("Authentication failed:", result.Error);
            Assert.Contains("Wiki (Read)", result.Error);
        }

        [Fact]
        public async Task AzureGetWikiPage_HttpFailure_ReturnsFetchError()
        {
            // Arrange
            SetupGetWikiPageThrows(new HttpRequestException("Azure DevOps could not find wiki page '/Home'."));

            // Act
            var result = await _tool.AzureGetWikiPage("/Home", wiki: "w", project: "p", organization: "o");

            // Assert
            Assert.False(result.Success);
            Assert.StartsWith("Failed to fetch wiki page:", result.Error);
            Assert.Contains("could not find wiki page", result.Error);
        }

        private void SetupGetWikiPage()
        {
            _serviceMock
                .Setup(s => s.GetWikiPageAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                    It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WikiPageContent { Path = "/Home", Content = "# Home" });
        }

        private void SetupGetWikiPageThrows(Exception exception)
        {
            _serviceMock
                .Setup(s => s.GetWikiPageAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                    It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
        }

        private void VerifyServiceNotCalled()
        {
            _serviceMock.Verify(s => s.GetWikiPageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
