using DrSasuMcp.AzureDevOps.AzureDevOps;
using DrSasuMcp.Tests.AzureDevOps.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;

namespace DrSasuMcp.Tests.AzureDevOps
{
    public class AzureDevOpsServiceWorkItemTests : IDisposable
    {
        private const string SingleWorkItemJson = """
        {
          "id": 1234,
          "rev": 7,
          "fields": {
            "System.AreaPath": "Contoso\\Web",
            "System.TeamProject": "Contoso",
            "System.IterationPath": "Contoso\\Sprint 12",
            "System.WorkItemType": "User Story",
            "System.State": "Active",
            "System.Reason": "Implementation started",
            "System.AssignedTo": { "displayName": "Ada Lovelace", "uniqueName": "ada@contoso.com" },
            "System.CreatedDate": "2026-02-03T09:15:22.13Z",
            "System.CreatedBy": { "displayName": "Grace Hopper" },
            "System.ChangedDate": "2026-02-10T11:00:00Z",
            "System.ChangedBy": { "displayName": "Ada Lovelace" },
            "System.Title": "Support work item lookups",
            "System.Description": "<div>Expose work items through MCP</div>",
            "System.Tags": "mcp; azure-devops ; api",
            "System.Parent": 1200,
            "Microsoft.VSTS.Common.Priority": 2,
            "Microsoft.VSTS.Scheduling.StoryPoints": 5.5,
            "Microsoft.VSTS.Common.AcceptanceCriteria": "<div>Given a work item id</div>",
            "Custom.Team": "Platform"
          },
          "_links": { "html": { "href": "https://dev.azure.com/contoso/Contoso/_workitems/edit/1234" } },
          "url": "https://dev.azure.com/contoso/_apis/wit/workItems/1234"
        }
        """;

        private readonly string? _originalPat;
        private readonly string? _originalOrg;
        private readonly string? _originalMaxWorkItems;

        public AzureDevOpsServiceWorkItemTests()
        {
            _originalPat = Environment.GetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsPat);
            _originalOrg = Environment.GetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsOrg);
            _originalMaxWorkItems = Environment.GetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsMaxWorkItems);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsPat, _originalPat);
            Environment.SetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsOrg, _originalOrg);
            Environment.SetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsMaxWorkItems, _originalMaxWorkItems);
        }

        [Fact]
        public async Task GetWorkItemAsync_ValidResponse_MapsFields()
        {
            // Arrange
            var handler = new StubHttpMessageHandler()
                .RespondTo("/wit/workitems/1234", SingleWorkItemJson);
            var service = CreateService(handler);

            // Act
            var workItem = await service.GetWorkItemAsync("contoso", "Contoso", 1234);

            // Assert
            Assert.Equal(1234, workItem.Id);
            Assert.Equal(7, workItem.Revision);
            Assert.Equal("User Story", workItem.WorkItemType);
            Assert.Equal("Support work item lookups", workItem.Title);
            Assert.Equal("Active", workItem.State);
            Assert.Equal("Implementation started", workItem.Reason);
            Assert.Equal("Ada Lovelace", workItem.AssignedTo);
            Assert.Equal("Grace Hopper", workItem.CreatedBy);
            Assert.Equal("Ada Lovelace", workItem.ChangedBy);
            Assert.Equal(new DateTime(2026, 2, 3, 9, 15, 22, 130, DateTimeKind.Utc), workItem.CreatedDate!.Value.ToUniversalTime());
            Assert.Equal(@"Contoso\Web", workItem.AreaPath);
            Assert.Equal(@"Contoso\Sprint 12", workItem.IterationPath);
            Assert.Equal("Contoso", workItem.ProjectName);
            Assert.Equal("contoso", workItem.Organization);
            Assert.Equal(new[] { "mcp", "azure-devops", "api" }, workItem.Tags);
            Assert.Equal(2, workItem.Priority);
            Assert.Equal(5.5, workItem.StoryPoints);
            Assert.Equal(1200, workItem.ParentId);
            Assert.Equal("<div>Expose work items through MCP</div>", workItem.Description);
            Assert.Equal("<div>Given a work item id</div>", workItem.AcceptanceCriteria);
            Assert.Equal("https://dev.azure.com/contoso/Contoso/_workitems/edit/1234", workItem.Url);
            Assert.Empty(workItem.Relations);
        }

        [Fact]
        public async Task GetWorkItemAsync_ProjectScoped_UsesProjectInRequestUrl()
        {
            // Arrange
            var handler = new StubHttpMessageHandler()
                .RespondTo("/wit/workitems/1234", SingleWorkItemJson);
            var service = CreateService(handler);

            // Act
            await service.GetWorkItemAsync("contoso", "Contoso", 1234);

            // Assert
            Assert.Single(handler.Requests);
            Assert.Contains("https://dev.azure.com/contoso/Contoso/_apis/wit/workitems/1234", handler.Requests[0].Url);
            Assert.Contains("$expand=fields", handler.Requests[0].Url);
        }

        [Fact]
        public async Task GetWorkItemAsync_NoProject_UsesOrganizationScopedUrl()
        {
            // Arrange
            var handler = new StubHttpMessageHandler()
                .RespondTo("/wit/workitems/1234", SingleWorkItemJson);
            var service = CreateService(handler);

            // Act
            await service.GetWorkItemAsync("contoso", null, 1234);

            // Assert
            Assert.Contains("https://dev.azure.com/contoso/_apis/wit/workitems/1234", handler.Requests[0].Url);
        }

        [Fact]
        public async Task GetWorkItemAsync_LegacyStringIdentityFields_MapsDisplayName()
        {
            // Arrange
            var json = """
            {
              "id": 5,
              "rev": 1,
              "fields": {
                "System.Title": "Older API shape",
                "System.AssignedTo": "Ada Lovelace <ada@contoso.com>",
                "System.CreatedBy": "Grace Hopper <grace@contoso.com>"
              }
            }
            """;
            var handler = new StubHttpMessageHandler().RespondTo("/wit/workitems/5", json);
            var service = CreateService(handler);

            // Act
            var workItem = await service.GetWorkItemAsync("contoso", "Contoso", 5);

            // Assert
            Assert.Equal("Ada Lovelace <ada@contoso.com>", workItem.AssignedTo);
            Assert.Equal("Grace Hopper <grace@contoso.com>", workItem.CreatedBy);
        }

        [Fact]
        public async Task GetWorkItemAsync_MissingOptionalFields_ReturnsEmptyDefaults()
        {
            // Arrange
            var json = """
            { "id": 8, "rev": 1, "fields": { "System.Title": "Bare work item" } }
            """;
            var handler = new StubHttpMessageHandler().RespondTo("/wit/workitems/8", json);
            var service = CreateService(handler);

            // Act
            var workItem = await service.GetWorkItemAsync("contoso", "Contoso", 8);

            // Assert
            Assert.Equal("Bare work item", workItem.Title);
            Assert.Equal(string.Empty, workItem.AssignedTo);
            Assert.Null(workItem.Priority);
            Assert.Null(workItem.ParentId);
            Assert.Null(workItem.CreatedDate);
            Assert.Empty(workItem.Tags);
            // Falls back to a constructed browser URL when the response has no _links section
            Assert.Equal("https://dev.azure.com/contoso/Contoso/_workitems/edit/8", workItem.Url);
        }

        [Fact]
        public async Task GetWorkItemAsync_WithRelations_RequestsExpandAllAndMapsLinks()
        {
            // Arrange
            var json = """
            {
              "id": 1234,
              "rev": 7,
              "fields": { "System.Title": "Parent linked work item", "System.TeamProject": "Contoso" },
              "relations": [
                {
                  "rel": "System.LinkTypes.Hierarchy-Reverse",
                  "url": "https://dev.azure.com/contoso/_apis/wit/workItems/1200",
                  "attributes": { "isLocked": false, "name": "Parent" }
                },
                {
                  "rel": "ArtifactLink",
                  "url": "vstfs:///Git/PullRequestId/abc%2Fdef%2F42",
                  "attributes": { "name": "Pull Request" }
                }
              ]
            }
            """;
            var handler = new StubHttpMessageHandler().RespondTo("/wit/workitems/1234", json);
            var service = CreateService(handler);

            // Act
            var workItem = await service.GetWorkItemAsync("contoso", "Contoso", 1234, includeRelations: true);

            // Assert
            Assert.Contains("$expand=all", handler.Requests[0].Url);
            Assert.Equal(2, workItem.Relations.Count);
            Assert.Equal("Parent", workItem.Relations[0].Name);
            Assert.Equal("System.LinkTypes.Hierarchy-Reverse", workItem.Relations[0].RelationType);
            Assert.Equal(1200, workItem.Relations[0].TargetWorkItemId);
            Assert.Equal("Pull Request", workItem.Relations[1].Name);
            Assert.Null(workItem.Relations[1].TargetWorkItemId);
        }

        [Fact]
        public async Task GetWorkItemAsync_WithAdditionalFields_ReturnsOnlyRequestedExtras()
        {
            // Arrange
            var handler = new StubHttpMessageHandler().RespondTo("/wit/workitems/1234", SingleWorkItemJson);
            var service = CreateService(handler);

            // Act
            var workItem = await service.GetWorkItemAsync(
                "contoso", "Contoso", 1234, additionalFields: new[] { "Custom.Team", "Custom.NotPresent" });

            // Assert
            Assert.Single(workItem.AdditionalFields);
            Assert.Equal("Platform", workItem.AdditionalFields["Custom.Team"]);
        }

        [Fact]
        public async Task GetWorkItemAsync_NotFound_ThrowsWithHelpfulMessage()
        {
            // Arrange
            var handler = new StubHttpMessageHandler().RespondTo(
                "/wit/workitems/99",
                """{"message":"TF401232: Work item 99 does not exist."}""",
                HttpStatusCode.NotFound);
            var service = CreateService(handler);

            // Act
            var exception = await Assert.ThrowsAsync<HttpRequestException>(
                () => service.GetWorkItemAsync("contoso", "Contoso", 99));

            // Assert
            Assert.Contains("could not find work item 99", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("TF401232", exception.Message);
        }

        [Fact]
        public async Task GetWorkItemAsync_Unauthorized_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var handler = new StubHttpMessageHandler().RespondTo(
                "/wit/workitems/1234",
                """{"message":"TF400813: The user is not authorized to access this resource."}""",
                HttpStatusCode.Unauthorized);
            var service = CreateService(handler);

            // Act
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.GetWorkItemAsync("contoso", "Contoso", 1234));

            // Assert
            Assert.Contains("Work Items (Read)", exception.Message);
        }

        [Fact]
        public async Task GetWorkItemAsync_MissingPat_ThrowsBeforeSendingRequest()
        {
            // Arrange
            var handler = new StubHttpMessageHandler().RespondTo("/wit/workitems/1234", SingleWorkItemJson);
            var service = CreateService(handler, pat: null);

            // Act
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.GetWorkItemAsync("contoso", "Contoso", 1234));

            // Assert
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task GetWorkItemsAsync_BatchResponse_PreservesRequestedOrderAndSkipsOmitted()
        {
            // Arrange
            var json = """
            {
              "count": 2,
              "value": [
                { "id": 5, "rev": 1, "fields": { "System.Title": "Second" } },
                null,
                { "id": 3, "rev": 1, "fields": { "System.Title": "First" } }
              ]
            }
            """;
            var handler = new StubHttpMessageHandler().RespondTo("/wit/workitems?ids=", json);
            var service = CreateService(handler);

            // Act
            var workItems = await service.GetWorkItemsAsync("contoso", "Contoso", new[] { 3, 5, 7 });

            // Assert
            Assert.Equal(new[] { 3, 5 }, workItems.Select(w => w.Id));
            Assert.Equal(new[] { "First", "Second" }, workItems.Select(w => w.Title));
            Assert.Contains("errorPolicy=omit", handler.Requests[0].Url);
        }

        [Fact]
        public async Task GetWorkItemsAsync_NoIds_MakesNoRequest()
        {
            // Arrange
            var handler = new StubHttpMessageHandler();
            var service = CreateService(handler);

            // Act
            var workItems = await service.GetWorkItemsAsync("contoso", "Contoso", Array.Empty<int>());

            // Assert
            Assert.Empty(workItems);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task GetWorkItemsAsync_MoreThanBatchLimit_SplitsIntoMultipleRequests()
        {
            // Arrange
            var ids = Enumerable.Range(1, AzureDevOpsToolConstants.WorkItemBatchSize + 1).ToList();
            var handler = new StubHttpMessageHandler()
                .RespondTo("/wit/workitems?ids=", """{"count":0,"value":[]}""");
            var service = CreateService(handler);

            // Act
            await service.GetWorkItemsAsync("contoso", "Contoso", ids);

            // Assert
            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal(AzureDevOpsToolConstants.WorkItemBatchSize, CountRequestedIds(handler.Requests[0].Url));
            Assert.Equal(1, CountRequestedIds(handler.Requests[1].Url));
        }

        [Fact]
        public async Task QueryWorkItemsAsync_FlatQuery_PostsWiqlAndHydratesResults()
        {
            // Arrange
            var wiqlJson = """
            {
              "queryType": "flat",
              "asOf": "2026-02-11T10:00:00Z",
              "workItems": [ { "id": 3 }, { "id": 5 } ]
            }
            """;
            var batchJson = """
            {
              "count": 2,
              "value": [
                { "id": 5, "rev": 1, "fields": { "System.Title": "Second" } },
                { "id": 3, "rev": 1, "fields": { "System.Title": "First" } }
              ]
            }
            """;
            var handler = new StubHttpMessageHandler()
                .RespondTo("/wit/wiql", wiqlJson)
                .RespondTo("/wit/workitems?ids=", batchJson);
            var service = CreateService(handler);
            var wiql = "SELECT [System.Id] FROM WorkItems WHERE [System.State] = 'Active'";

            // Act
            var result = await service.QueryWorkItemsAsync("contoso", "Contoso", wiql, top: 2);

            // Assert
            Assert.Equal("flat", result.QueryType);
            Assert.Equal(new DateTime(2026, 2, 11, 10, 0, 0, DateTimeKind.Utc), result.AsOf!.Value.ToUniversalTime());
            Assert.Equal(2, result.MatchedCount);
            Assert.False(result.Truncated);
            Assert.Equal(new[] { 3, 5 }, result.WorkItems.Select(w => w.Id));

            var wiqlRequest = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, wiqlRequest.Method);
            Assert.Contains("https://dev.azure.com/contoso/Contoso/_apis/wit/wiql", wiqlRequest.Url);
            // One extra result is requested so truncation can be detected
            Assert.Contains("$top=3", wiqlRequest.Url);
            Assert.Contains("[System.State]", wiqlRequest.Body);
        }

        [Fact]
        public async Task QueryWorkItemsAsync_MoreMatchesThanLimit_ReportsTruncated()
        {
            // Arrange
            var wiqlJson = """{ "queryType": "flat", "workItems": [ { "id": 3 }, { "id": 5 } ] }""";
            var batchJson = """{ "count": 1, "value": [ { "id": 3, "rev": 1, "fields": { "System.Title": "First" } } ] }""";
            var handler = new StubHttpMessageHandler()
                .RespondTo("/wit/wiql", wiqlJson)
                .RespondTo("/wit/workitems?ids=", batchJson);
            var service = CreateService(handler);

            // Act
            var result = await service.QueryWorkItemsAsync(
                "contoso", "Contoso", "SELECT [System.Id] FROM WorkItems", top: 1);

            // Assert
            Assert.True(result.Truncated);
            Assert.Equal(1, result.MatchedCount);
            Assert.Single(result.WorkItems);
            Assert.Contains("ids=3", handler.Requests[1].Url);
        }

        [Fact]
        public async Task QueryWorkItemsAsync_TreeQuery_CollectsIdsFromRelations()
        {
            // Arrange
            var wiqlJson = """
            {
              "queryType": "tree",
              "workItemRelations": [
                { "rel": null, "source": null, "target": { "id": 10 } },
                { "rel": "System.LinkTypes.Hierarchy-Forward", "source": { "id": 10 }, "target": { "id": 11 } }
              ]
            }
            """;
            var batchJson = """
            {
              "count": 2,
              "value": [
                { "id": 10, "rev": 1, "fields": { "System.Title": "Parent" } },
                { "id": 11, "rev": 1, "fields": { "System.Title": "Child" } }
              ]
            }
            """;
            var handler = new StubHttpMessageHandler()
                .RespondTo("/wit/wiql", wiqlJson)
                .RespondTo("/wit/workitems?ids=", batchJson);
            var service = CreateService(handler);

            // Act
            var result = await service.QueryWorkItemsAsync(
                "contoso", "Contoso", "SELECT [System.Id] FROM WorkItemLinks", top: 10);

            // Assert
            Assert.Equal("tree", result.QueryType);
            Assert.Equal(new[] { 10, 11 }, result.WorkItems.Select(w => w.Id));
        }

        [Fact]
        public async Task QueryWorkItemsAsync_TopAboveConfiguredMaximum_ClampsToMaximum()
        {
            // Arrange
            var handler = new StubHttpMessageHandler()
                .RespondTo("/wit/wiql", """{ "queryType": "flat", "workItems": [] }""");
            var service = CreateService(handler, maxWorkItems: 5);

            // Act
            await service.QueryWorkItemsAsync("contoso", "Contoso", "SELECT [System.Id] FROM WorkItems", top: 500);

            // Assert
            Assert.Contains("$top=6", handler.Requests[0].Url);
        }

        [Fact]
        public async Task QueryWorkItemsAsync_NoProject_UsesOrganizationScopedUrl()
        {
            // Arrange
            var handler = new StubHttpMessageHandler()
                .RespondTo("/wit/wiql", """{ "queryType": "flat", "workItems": [] }""");
            var service = CreateService(handler);

            // Act
            var result = await service.QueryWorkItemsAsync("contoso", null, "SELECT [System.Id] FROM WorkItems", top: 10);

            // Assert
            Assert.Contains("https://dev.azure.com/contoso/_apis/wit/wiql", handler.Requests[0].Url);
            Assert.Empty(result.WorkItems);
        }

        [Fact]
        public async Task GetPullRequestWorkItemIdsAsync_StringIds_ReturnsParsedIds()
        {
            // Arrange
            var json = """
            {
              "count": 2,
              "value": [
                { "id": "297", "url": "https://dev.azure.com/contoso/_apis/wit/workItems/297" },
                { "id": "299", "url": "https://dev.azure.com/contoso/_apis/wit/workItems/299" }
              ]
            }
            """;
            var handler = new StubHttpMessageHandler().RespondTo("/pullRequests/42/workitems", json);
            var service = CreateService(handler);

            // Act
            var ids = await service.GetPullRequestWorkItemIdsAsync("contoso", "Contoso", "MyRepo", 42);

            // Assert
            Assert.Equal(new[] { 297, 299 }, ids);
            Assert.Contains("https://dev.azure.com/contoso/Contoso/_apis/git/repositories/MyRepo/pullRequests/42/workitems", handler.Requests[0].Url);
        }

        [Fact]
        public async Task GetPullRequestWorkItemIdsAsync_NoLinkedWorkItems_ReturnsEmptyList()
        {
            // Arrange
            var handler = new StubHttpMessageHandler()
                .RespondTo("/pullRequests/42/workitems", """{ "count": 0, "value": [] }""");
            var service = CreateService(handler);

            // Act
            var ids = await service.GetPullRequestWorkItemIdsAsync("contoso", "Contoso", "MyRepo", 42);

            // Assert
            Assert.Empty(ids);
        }

        [Fact]
        public void GetDefaultOrganization_EnvironmentVariableSet_ReturnsValue()
        {
            // Arrange
            var service = CreateService(new StubHttpMessageHandler(), organization: "contoso");

            // Act & Assert
            Assert.Equal("contoso", service.GetDefaultOrganization());
        }

        [Fact]
        public void GetDefaultOrganization_EnvironmentVariableMissing_ReturnsNull()
        {
            // Arrange
            var service = CreateService(new StubHttpMessageHandler());

            // Act & Assert
            Assert.Null(service.GetDefaultOrganization());
        }

        [Fact]
        public void GetMaxWorkItems_NoOverride_ReturnsDefault()
        {
            // Arrange
            var service = CreateService(new StubHttpMessageHandler());

            // Act & Assert
            Assert.Equal(AzureDevOpsToolConstants.DefaultMaxWorkItems, service.GetMaxWorkItems());
        }

        private static int CountRequestedIds(string url)
        {
            var start = url.IndexOf("ids=", StringComparison.OrdinalIgnoreCase) + "ids=".Length;
            var end = url.IndexOf('&', start);
            var ids = end < 0 ? url[start..] : url[start..end];

            return ids.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static AzureDevOpsService CreateService(
            StubHttpMessageHandler handler,
            string? pat = "test-pat",
            string? organization = null,
            int? maxWorkItems = null)
        {
            Environment.SetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsPat, pat);
            Environment.SetEnvironmentVariable(AzureDevOpsToolConstants.EnvAzureDevOpsOrg, organization);
            Environment.SetEnvironmentVariable(
                AzureDevOpsToolConstants.EnvAzureDevOpsMaxWorkItems,
                maxWorkItems?.ToString());

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
