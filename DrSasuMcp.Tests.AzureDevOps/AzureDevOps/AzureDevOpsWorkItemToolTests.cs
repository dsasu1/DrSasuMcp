using DrSasuMcp.AzureDevOps.AzureDevOps;
using DrSasuMcp.AzureDevOps.AzureDevOps.Analyzers;
using DrSasuMcp.AzureDevOps.AzureDevOps.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DrSasuMcp.Tests.AzureDevOps
{
    public class AzureDevOpsWorkItemToolTests
    {
        private readonly Mock<IAzureDevOpsService> _serviceMock = new();
        private readonly AzureDevOpsTool _tool;

        public AzureDevOpsWorkItemToolTests()
        {
            _tool = new AzureDevOpsTool(
                _serviceMock.Object,
                new Mock<IDiffService>().Object,
                Array.Empty<ICodeAnalyzer>(),
                new Mock<ILogger<AzureDevOpsTool>>().Object);
        }

        [Fact]
        public async Task AzureGetWorkItem_UrlReference_UsesOrganizationAndProjectFromUrl()
        {
            // Arrange
            SetupGetWorkItem(new WorkItemInfo { Id = 1234, Title = "From URL" });

            // Act
            var result = await _tool.AzureGetWorkItem("https://dev.azure.com/myorg/myproject/_workitems/edit/1234");

            // Assert
            Assert.True(result.Success);
            _serviceMock.Verify(s => s.GetWorkItemAsync(
                "myorg", "myproject", 1234, false, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AzureGetWorkItem_UrlReference_TakesPrecedenceOverArguments()
        {
            // Arrange
            SetupGetWorkItem(new WorkItemInfo { Id = 1234 });

            // Act
            var result = await _tool.AzureGetWorkItem(
                "https://dev.azure.com/urlorg/urlproject/_workitems/edit/1234",
                project: "argproject",
                organization: "argorg");

            // Assert
            Assert.True(result.Success);
            _serviceMock.Verify(s => s.GetWorkItemAsync(
                "urlorg", "urlproject", 1234, false, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AzureGetWorkItem_BareIdWithArguments_PassesArgumentsThrough()
        {
            // Arrange
            SetupGetWorkItem(new WorkItemInfo { Id = 77 });

            // Act
            var result = await _tool.AzureGetWorkItem("77", project: "Contoso", organization: "contoso", includeRelations: true);

            // Assert
            Assert.True(result.Success);
            _serviceMock.Verify(s => s.GetWorkItemAsync(
                "contoso", "Contoso", 77, true, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AzureGetWorkItem_BareIdWithConfiguredOrganization_UsesDefaultOrganization()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetDefaultOrganization()).Returns("envorg");
            SetupGetWorkItem(new WorkItemInfo { Id = 77 });

            // Act
            var result = await _tool.AzureGetWorkItem("77", project: "Contoso");

            // Assert
            Assert.True(result.Success);
            _serviceMock.Verify(s => s.GetWorkItemAsync(
                "envorg", "Contoso", 77, false, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AzureGetWorkItem_NoOrganizationAvailable_ReturnsError()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetDefaultOrganization()).Returns((string?)null);

            // Act
            var result = await _tool.AzureGetWorkItem("77");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("AZURE_DEVOPS_ORG", result.Error);
            _serviceMock.Verify(s => s.GetWorkItemAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData("not-a-work-item")]
        [InlineData("https://dev.azure.com/org/project/_git/repo/pullrequest/5")]
        [InlineData("")]
        public async Task AzureGetWorkItem_InvalidReference_ReturnsError(string reference)
        {
            // Act
            var result = await _tool.AzureGetWorkItem(reference);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Invalid work item reference", result.Error);
        }

        [Fact]
        public async Task AzureGetWorkItem_AdditionalFields_PassesParsedFieldNames()
        {
            // Arrange
            IEnumerable<string>? capturedFields = null;
            _serviceMock
                .Setup(s => s.GetWorkItemAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<bool>(),
                    It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
                .Callback((string _, string? _, int _, bool _, IEnumerable<string>? fields, CancellationToken _) => capturedFields = fields)
                .ReturnsAsync(new WorkItemInfo { Id = 1 });

            // Act
            var result = await _tool.AzureGetWorkItem(
                "1", organization: "contoso", additionalFields: " Custom.Team , System.BoardColumn ,, Custom.Team ");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(capturedFields);
            Assert.Equal(new[] { "Custom.Team", "System.BoardColumn" }, capturedFields);
        }

        [Fact]
        public async Task AzureGetWorkItem_ServiceUnauthorized_ReturnsAuthenticationError()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetWorkItemAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<bool>(),
                    It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException("PAT is missing"));

            // Act
            var result = await _tool.AzureGetWorkItem("1", organization: "contoso");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Authentication failed", result.Error);
            Assert.Contains("PAT is missing", result.Error);
        }

        [Fact]
        public async Task AzureGetWorkItem_ServiceHttpFailure_ReturnsError()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetWorkItemAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<bool>(),
                    It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("work item 9 was not found"));

            // Act
            var result = await _tool.AzureGetWorkItem("9", organization: "contoso");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("work item 9 was not found", result.Error);
        }

        [Fact]
        public async Task AzureGetWorkItem_SuccessfulCall_ReturnsWorkItemData()
        {
            // Arrange
            var workItem = new WorkItemInfo { Id = 1234, Title = "Support work item lookups", State = "Active" };
            SetupGetWorkItem(workItem);

            // Act
            var result = await _tool.AzureGetWorkItem("1234", organization: "contoso");

            // Assert
            Assert.True(result.Success);
            Assert.Same(workItem, result.Data);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AzureQueryWorkItems_EmptyQuery_ReturnsError(string wiql)
        {
            // Act
            var result = await _tool.AzureQueryWorkItems(wiql, organization: "contoso");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("WIQL query is required", result.Error);
        }

        [Theory]
        [InlineData("DELETE FROM WorkItems")]
        [InlineData("UPDATE WorkItems SET [System.State] = 'Closed'")]
        public async Task AzureQueryWorkItems_NonSelectQuery_ReturnsError(string wiql)
        {
            // Act
            var result = await _tool.AzureQueryWorkItems(wiql, organization: "contoso");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Only WIQL SELECT queries are supported", result.Error);
            _serviceMock.Verify(s => s.QueryWorkItemsAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AzureQueryWorkItems_ValidQuery_ReturnsResultAndRowCount()
        {
            // Arrange
            var queryResult = new WorkItemQueryResult
            {
                QueryType = "flat",
                MatchedCount = 2,
                WorkItems = new List<WorkItemInfo>
                {
                    new() { Id = 3, Title = "First" },
                    new() { Id = 5, Title = "Second" }
                }
            };
            _serviceMock
                .Setup(s => s.QueryWorkItemsAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            var result = await _tool.AzureQueryWorkItems(
                "  select [System.Id] from WorkItems where [System.State] = 'Active'",
                project: "Contoso",
                organization: "contoso",
                top: 25);

            // Assert
            Assert.True(result.Success);
            Assert.Same(queryResult, result.Data);
            Assert.Equal(2, result.RowsAffected);
            _serviceMock.Verify(s => s.QueryWorkItemsAsync(
                "contoso", "Contoso", It.IsAny<string>(), 25, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public async Task AzureQueryWorkItems_NonPositiveTop_FallsBackToDefault(int top)
        {
            // Arrange
            _serviceMock
                .Setup(s => s.QueryWorkItemsAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WorkItemQueryResult());

            // Act
            var result = await _tool.AzureQueryWorkItems(
                "SELECT [System.Id] FROM WorkItems", organization: "contoso", top: top);

            // Assert
            Assert.True(result.Success);
            _serviceMock.Verify(s => s.QueryWorkItemsAsync(
                "contoso", null, It.IsAny<string>(), 50, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AzureQueryWorkItems_NoOrganizationAvailable_ReturnsError()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetDefaultOrganization()).Returns((string?)null);

            // Act
            var result = await _tool.AzureQueryWorkItems("SELECT [System.Id] FROM WorkItems");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("AZURE_DEVOPS_ORG", result.Error);
        }

        [Fact]
        public async Task AzureQueryWorkItems_ServiceFailure_ReturnsError()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.QueryWorkItemsAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("VS403474: WIQL syntax error"));

            // Act
            var result = await _tool.AzureQueryWorkItems("SELECT [System.Id] FROM WorkItems", organization: "contoso");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("VS403474", result.Error);
        }

        [Theory]
        [InlineData("https://dev.azure.com/org/project/_workitems/edit/1")]
        [InlineData("not-a-url")]
        public async Task AzureGetPullRequestWorkItems_InvalidUrl_ReturnsError(string prUrl)
        {
            // Act
            var result = await _tool.AzureGetPullRequestWorkItems(prUrl);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Invalid Azure DevOps PR URL format", result.Error);
        }

        [Fact]
        public async Task AzureGetPullRequestWorkItems_LinkedWorkItems_ReturnsWorkItemDetails()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetPullRequestWorkItemIdsAsync(
                    "myorg", "myproject", "myrepo", 123, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<int> { 297, 299 });
            _serviceMock
                .Setup(s => s.GetWorkItemsAsync(
                    "myorg", "myproject", It.IsAny<IEnumerable<int>>(),
                    It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<WorkItemInfo>
                {
                    new() { Id = 297, Title = "Bug fix", WorkItemType = "Bug" },
                    new() { Id = 299, Title = "Follow up", WorkItemType = "Task" }
                });

            // Act
            var result = await _tool.AzureGetPullRequestWorkItems(
                "https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.RowsAffected);

            var data = Assert.IsType<PullRequestWorkItems>(result.Data);
            Assert.Equal(123, data.PullRequestId);
            Assert.Equal("myorg", data.Organization);
            Assert.Equal("myproject", data.ProjectName);
            Assert.Equal("myrepo", data.RepositoryName);
            Assert.Equal(2, data.LinkedCount);
            Assert.Equal(new[] { 297, 299 }, data.WorkItems.Select(w => w.Id));
        }

        [Fact]
        public async Task AzureGetPullRequestWorkItems_NoLinkedWorkItems_ReturnsEmptyResult()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetPullRequestWorkItemIdsAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<int>());
            _serviceMock
                .Setup(s => s.GetWorkItemsAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IEnumerable<int>>(),
                    It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<WorkItemInfo>());

            // Act
            var result = await _tool.AzureGetPullRequestWorkItems(
                "https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123");

            // Assert
            Assert.True(result.Success);
            var data = Assert.IsType<PullRequestWorkItems>(result.Data);
            Assert.Equal(0, data.LinkedCount);
            Assert.Empty(data.WorkItems);
        }

        [Fact]
        public async Task AzureGetPullRequestWorkItems_ServiceUnauthorized_ReturnsAuthenticationError()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetPullRequestWorkItemIdsAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException("PAT lacks Work Items (Read)"));

            // Act
            var result = await _tool.AzureGetPullRequestWorkItems(
                "https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Authentication failed", result.Error);
        }

        private void SetupGetWorkItem(WorkItemInfo workItem)
        {
            _serviceMock
                .Setup(s => s.GetWorkItemAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<bool>(),
                    It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(workItem);
        }
    }
}
