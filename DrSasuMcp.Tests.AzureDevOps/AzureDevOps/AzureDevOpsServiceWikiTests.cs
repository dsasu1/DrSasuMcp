using DrSasuMcp.AzureDevOps.AzureDevOps;
using DrSasuMcp.Tests.AzureDevOps.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;

namespace DrSasuMcp.Tests.AzureDevOps
{
    [Collection(EnvironmentVariableCollection.Name)]
    public class AzureDevOpsServiceWikiTests : IDisposable
    {
        private const string WikiPageJson = """
        {
          "id": 42,
          "path": "/Engineering/Onboarding",
          "content": "# Onboarding\n\nWelcome aboard.",
          "gitItemPath": "/Engineering/Onboarding.md",
          "isParentPage": true,
          "order": 1,
          "remoteUrl": "https://dev.azure.com/contoso/Contoso/_wiki/wikis/Contoso.wiki/42/Onboarding",
          "subPages": [
            {
              "id": 43,
              "path": "/Engineering/Onboarding/Laptop Setup",
              "isParentPage": false,
              "remoteUrl": "https://dev.azure.com/contoso/Contoso/_wiki/wikis/Contoso.wiki/43/Laptop-Setup"
            },
            {
              "id": 44,
              "path": "/Engineering/Onboarding/Accounts",
              "isParentPage": true
            }
          ]
        }
        """;

        private readonly string? _originalPat;
        private readonly string? _originalOrg;
        private readonly string? _originalMaxFileSize;

        public AzureDevOpsServiceWikiTests()
        {
            _originalPat = Environment.GetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsPat);
            _originalOrg = Environment.GetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsOrg);
            _originalMaxFileSize = Environment.GetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsMaxFileSize);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsPat, _originalPat);
            Environment.SetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsOrg, _originalOrg);
            Environment.SetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsMaxFileSize, _originalMaxFileSize);
        }

        [Fact]
        public async Task GetWikisAsync_ValidResponse_MapsFields()
        {
            // Arrange
            var json = """
            {
              "count": 2,
              "value": [
                {
                  "id": "b5b5c1c5-0000-0000-0000-000000000001",
                  "name": "Contoso.wiki",
                  "type": "projectWiki",
                  "projectId": "p-1",
                  "repositoryId": "r-1",
                  "mappedPath": "/",
                  "remoteUrl": "https://dev.azure.com/contoso/Contoso/_wiki/wikis/Contoso.wiki",
                  "versions": [ { "version": "wikiMaster" } ]
                },
                {
                  "id": "b5b5c1c5-0000-0000-0000-000000000002",
                  "name": "Api Docs",
                  "type": "codeWiki",
                  "mappedPath": "/docs",
                  "versions": [ { "version": "main" }, { "version": "release" } ]
                }
              ]
            }
            """;
            var handler = new StubHttpMessageHandler().RespondTo("/_apis/wiki/wikis?", json);
            var service = CreateService(handler);

            // Act
            var wikis = await service.GetWikisAsync("contoso", "Contoso");

            // Assert
            Assert.Equal(2, wikis.Count);
            Assert.Equal("Contoso.wiki", wikis[0].Name);
            Assert.Equal("projectWiki", wikis[0].Type);
            Assert.Equal("r-1", wikis[0].RepositoryId);
            Assert.Equal(new[] { "wikiMaster" }, wikis[0].Versions);
            Assert.Equal("https://dev.azure.com/contoso/Contoso/_wiki/wikis/Contoso.wiki", wikis[0].Url);
            Assert.Equal("codeWiki", wikis[1].Type);
            Assert.Equal("/docs", wikis[1].MappedPath);
            Assert.Equal(new[] { "main", "release" }, wikis[1].Versions);

            var request = Assert.Single(handler.Requests);
            Assert.StartsWith("https://dev.azure.com/contoso/Contoso/_apis/wiki/wikis?", request.Url);
        }

        [Fact]
        public async Task GetWikisAsync_NoProject_UsesOrganizationScope()
        {
            // Arrange
            var handler = new StubHttpMessageHandler().RespondTo("/_apis/wiki/wikis?", """{"count":0,"value":[]}""");
            var service = CreateService(handler);

            // Act
            var wikis = await service.GetWikisAsync("contoso", null);

            // Assert
            Assert.Empty(wikis);
            Assert.StartsWith("https://dev.azure.com/contoso/_apis/wiki/wikis?", Assert.Single(handler.Requests).Url);
        }

        [Fact]
        public async Task GetWikiPageAsync_ByPath_EscapesPathAndWikiAndRequestsContent()
        {
            // Arrange
            var handler = new StubHttpMessageHandler().RespondTo("/pages?", WikiPageJson);
            var service = CreateService(handler);

            // Act
            var page = await service.GetWikiPageAsync("contoso", "Contoso", "Team Docs", "/Engineering/On boarding", null);

            // Assert
            Assert.Equal(42, page.Id);
            Assert.Equal("/Engineering/Onboarding", page.Path);
            Assert.Equal("# Onboarding\n\nWelcome aboard.", page.Content);
            Assert.False(page.ContentTruncated);
            Assert.Equal("/Engineering/Onboarding.md", page.GitItemPath);
            Assert.True(page.IsParentPage);
            Assert.Equal("Team Docs", page.WikiName);
            Assert.Equal("Contoso", page.ProjectName);
            Assert.Equal("contoso", page.Organization);
            Assert.Equal("https://dev.azure.com/contoso/Contoso/_wiki/wikis/Contoso.wiki/42/Onboarding", page.Url);

            var url = Assert.Single(handler.Requests).Url;
            Assert.Contains("/_apis/wiki/wikis/Team%20Docs/pages?", url);
            Assert.Contains("path=%2FEngineering%2FOn%20boarding", url);
            Assert.Contains("includeContent=true", url);
            Assert.DoesNotContain("recursionLevel", url);
            Assert.DoesNotContain("versionDescriptor", url);
        }

        [Fact]
        public async Task GetWikiPageAsync_NoPath_RequestsRootPage()
        {
            // Arrange
            var handler = new StubHttpMessageHandler().RespondTo("/pages?", """{"id":1,"path":"/","content":"root"}""");
            var service = CreateService(handler);

            // Act
            var page = await service.GetWikiPageAsync("contoso", "Contoso", "Contoso.wiki", null, null);

            // Assert
            Assert.Equal("/", page.Path);
            Assert.Contains("path=%2F&", Assert.Single(handler.Requests).Url);
        }

        [Fact]
        public async Task GetWikiPageAsync_ByPageId_UsesIdEndpoint()
        {
            // Arrange
            var handler = new StubHttpMessageHandler().RespondTo("/pages/42?", WikiPageJson);
            var service = CreateService(handler);

            // Act
            var page = await service.GetWikiPageAsync("contoso", "Contoso", "Contoso.wiki", "/ignored", 42);

            // Assert
            Assert.Equal(42, page.Id);
            var url = Assert.Single(handler.Requests).Url;
            Assert.Contains("/_apis/wiki/wikis/Contoso.wiki/pages/42?includeContent=true", url);
            Assert.DoesNotContain("path=", url);
        }

        [Fact]
        public async Task GetWikiPageAsync_IncludeSubPages_RequestsOneLevelAndMapsChildren()
        {
            // Arrange
            var handler = new StubHttpMessageHandler().RespondTo("/pages?", WikiPageJson);
            var service = CreateService(handler);

            // Act
            var page = await service.GetWikiPageAsync(
                "contoso", "Contoso", "Contoso.wiki", "/Engineering/Onboarding", null, includeSubPages: true);

            // Assert
            Assert.Contains("recursionLevel=oneLevel", Assert.Single(handler.Requests).Url);
            Assert.Equal(2, page.SubPages.Count);
            Assert.Equal(43, page.SubPages[0].Id);
            Assert.Equal("/Engineering/Onboarding/Laptop Setup", page.SubPages[0].Path);
            Assert.False(page.SubPages[0].IsParentPage);
            Assert.Equal("https://dev.azure.com/contoso/Contoso/_wiki/wikis/Contoso.wiki/43/Laptop-Setup", page.SubPages[0].Url);
            Assert.True(page.SubPages[1].IsParentPage);
            Assert.Equal(string.Empty, page.SubPages[1].Url);
        }

        [Fact]
        public async Task GetWikiPageAsync_Version_AddsBranchVersionDescriptor()
        {
            // Arrange
            var handler = new StubHttpMessageHandler().RespondTo("/pages?", WikiPageJson);
            var service = CreateService(handler);

            // Act
            await service.GetWikiPageAsync(
                "contoso", "Contoso", "Api Docs", "/Overview", null, version: "feature/new docs");

            // Assert
            var url = Assert.Single(handler.Requests).Url;
            Assert.Contains("versionDescriptor.version=feature%2Fnew%20docs", url);
            Assert.Contains("versionDescriptor.versionType=branch", url);
        }

        [Fact]
        public async Task GetWikiPageAsync_ContentOverLimit_TruncatesOnCharacterBoundary()
        {
            // Arrange
            // "é" is two UTF-8 bytes, so a 5 byte limit must stop after "ab" + "é" rather than split the next one
            var handler = new StubHttpMessageHandler().RespondTo("/pages?", """{"id":1,"path":"/Big","content":"abééé"}""");
            var service = CreateService(handler, maxFileSize: 5);

            // Act
            var page = await service.GetWikiPageAsync("contoso", "Contoso", "Contoso.wiki", "/Big", null);

            // Assert
            Assert.True(page.ContentTruncated);
            Assert.Equal("abé", page.Content);
        }

        [Fact]
        public async Task GetWikiPageAsync_NotFound_ThrowsDescriptiveHttpRequestException()
        {
            // Arrange
            var handler = new StubHttpMessageHandler().RespondTo(
                "/pages?",
                """{"message":"The page '/Missing' specified in the add operation doesn't exist in the wiki."}""",
                HttpStatusCode.NotFound);
            var service = CreateService(handler);

            // Act
            var exception = await Assert.ThrowsAsync<HttpRequestException>(
                () => service.GetWikiPageAsync("contoso", "Contoso", "Contoso.wiki", "/Missing", null));

            // Assert
            Assert.Contains("could not find wiki page '/Missing'", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("doesn't exist in the wiki", exception.Message);
        }

        [Fact]
        public async Task GetWikiPageAsync_Unauthorized_MentionsWikiScope()
        {
            // Arrange
            var handler = new StubHttpMessageHandler().RespondTo(
                "/pages/42?",
                """{"message":"TF400813: The user is not authorized to access this resource."}""",
                HttpStatusCode.Unauthorized);
            var service = CreateService(handler);

            // Act
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.GetWikiPageAsync("contoso", "Contoso", "Contoso.wiki", null, 42));

            // Assert
            Assert.Contains("Wiki (Read)", exception.Message);
            Assert.DoesNotContain("Work Items", exception.Message);
        }

        [Fact]
        public async Task GetWikisAsync_MissingPat_ThrowsBeforeSendingRequest()
        {
            // Arrange
            var handler = new StubHttpMessageHandler().RespondTo("/wiki/wikis", """{"value":[]}""");
            var service = CreateService(handler, pat: null);

            // Act
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetWikisAsync("contoso", "Contoso"));

            // Assert
            Assert.Empty(handler.Requests);
        }

        private static AzureDevOpsService CreateService(
            StubHttpMessageHandler handler,
            string? pat = "test-pat",
            int? maxFileSize = null)
        {
            Environment.SetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsPat, pat);
            Environment.SetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsOrg, null);
            Environment.SetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsMaxFileSize, maxFileSize?.ToString());

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory
                .Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(() => new HttpClient(handler, disposeHandler: false));

            return new AzureDevOpsService(
                httpClientFactory.Object,
                new Mock<IDiffService>().Object,
                new Mock<ILogger<AzureDevOpsService>>().Object);
        }
    }
}
