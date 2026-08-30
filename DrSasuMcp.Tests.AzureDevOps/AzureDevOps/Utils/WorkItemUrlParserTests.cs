using DrSasuMcp.AzureDevOps.AzureDevOps.Utils;
using Xunit;

namespace DrSasuMcp.Tests.AzureDevOps.Utils
{
    public class WorkItemUrlParserTests
    {
        [Fact]
        public void ParseWorkItemUrl_ValidUrl_ReturnsComponents()
        {
            // Arrange
            var url = "https://dev.azure.com/myorg/myproject/_workitems/edit/1234";

            // Act
            var result = WorkItemUrlParser.ParseWorkItemUrl(url);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("myorg", result.Value.organization);
            Assert.Equal("myproject", result.Value.project);
            Assert.Equal(1234, result.Value.workItemId);
        }

        [Fact]
        public void ParseWorkItemUrl_UrlWithoutProject_ReturnsNullProject()
        {
            // Arrange
            var url = "https://dev.azure.com/myorg/_workitems/edit/42";

            // Act
            var result = WorkItemUrlParser.ParseWorkItemUrl(url);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("myorg", result.Value.organization);
            Assert.Null(result.Value.project);
            Assert.Equal(42, result.Value.workItemId);
        }

        [Fact]
        public void ParseWorkItemUrl_LegacyVisualStudioHost_ReturnsComponents()
        {
            // Arrange
            var url = "https://contoso.visualstudio.com/MyProject/_workitems/edit/987";

            // Act
            var result = WorkItemUrlParser.ParseWorkItemUrl(url);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("contoso", result.Value.organization);
            Assert.Equal("MyProject", result.Value.project);
            Assert.Equal(987, result.Value.workItemId);
        }

        [Theory]
        [InlineData("https://dev.azure.com/org/project/_workitems?id=555", 555)]
        [InlineData("https://dev.azure.com/org/project/_workitems/?id=555&_a=edit", 555)]
        [InlineData("https://contoso.visualstudio.com/project/_workitems?_a=edit&id=555", 555)]
        public void ParseWorkItemUrl_QueryStringForm_ReturnsId(string url, int expectedId)
        {
            // Act
            var result = WorkItemUrlParser.ParseWorkItemUrl(url);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedId, result.Value.workItemId);
        }

        [Fact]
        public void ParseWorkItemUrl_UrlWithTrailingSegments_ReturnsComponents()
        {
            // Arrange
            var url = "https://dev.azure.com/myorg/my%20project/_workitems/edit/77/?fullScreen=true";

            // Act
            var result = WorkItemUrlParser.ParseWorkItemUrl(url);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("my%20project", result.Value.project);
            Assert.Equal(77, result.Value.workItemId);
        }

        [Theory]
        [InlineData("https://dev.azure.com/org/project/_git/repo/pullrequest/123")]
        [InlineData("https://github.com/owner/repo/issues/12")]
        [InlineData("https://dev.azure.com/org/project/_workitems/edit/abc")]
        [InlineData("https://dev.azure.com/org/project/_workitems")]
        [InlineData("https://dev.azure.com/org/project/_workitems?myid=5")]
        [InlineData("invalid-url")]
        [InlineData("")]
        [InlineData(null)]
        public void ParseWorkItemUrl_InvalidUrl_ReturnsNull(string? url)
        {
            // Act
            var result = WorkItemUrlParser.ParseWorkItemUrl(url!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void IsValidWorkItemUrl_ValidUrl_ReturnsTrue()
        {
            // Act & Assert
            Assert.True(WorkItemUrlParser.IsValidWorkItemUrl("https://dev.azure.com/org/project/_workitems/edit/1"));
        }

        [Theory]
        [InlineData("https://dev.azure.com/org/project/_git/repo/pullrequest/1")]
        [InlineData("nope")]
        [InlineData("")]
        public void IsValidWorkItemUrl_InvalidUrl_ReturnsFalse(string url)
        {
            // Act & Assert
            Assert.False(WorkItemUrlParser.IsValidWorkItemUrl(url));
        }

        [Theory]
        [InlineData("1234", 1234)]
        [InlineData(" 1234 ", 1234)]
        [InlineData("#1234", 1234)]
        public void ParseWorkItemReference_BareId_ReturnsIdWithoutOrganization(string reference, int expectedId)
        {
            // Act
            var result = WorkItemUrlParser.ParseWorkItemReference(reference);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Value.organization);
            Assert.Null(result.Value.project);
            Assert.Equal(expectedId, result.Value.workItemId);
        }

        [Fact]
        public void ParseWorkItemReference_Url_ReturnsOrganizationAndProject()
        {
            // Act
            var result = WorkItemUrlParser.ParseWorkItemReference("https://dev.azure.com/myorg/myproject/_workitems/edit/9");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("myorg", result.Value.organization);
            Assert.Equal("myproject", result.Value.project);
            Assert.Equal(9, result.Value.workItemId);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-5")]
        [InlineData("12abc")]
        [InlineData("")]
        [InlineData(null)]
        public void ParseWorkItemReference_InvalidReference_ReturnsNull(string? reference)
        {
            // Act
            var result = WorkItemUrlParser.ParseWorkItemReference(reference!);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("https://dev.azure.com/org/_apis/wit/workItems/301", 301)]
        [InlineData("https://dev.azure.com/org/project/_apis/wit/workitems/44/", 44)]
        public void ParseWorkItemIdFromApiUrl_WorkItemUrl_ReturnsId(string url, int expectedId)
        {
            // Act
            var result = WorkItemUrlParser.ParseWorkItemIdFromApiUrl(url);

            // Assert
            Assert.Equal(expectedId, result);
        }

        [Theory]
        [InlineData("vstfs:///Git/PullRequestId/1234%2f5678%2f90")]
        [InlineData("https://dev.azure.com/org/_apis/wit/attachments/guid")]
        [InlineData("https://example.com/docs")]
        [InlineData("")]
        [InlineData(null)]
        public void ParseWorkItemIdFromApiUrl_NonWorkItemUrl_ReturnsNull(string? url)
        {
            // Act
            var result = WorkItemUrlParser.ParseWorkItemIdFromApiUrl(url);

            // Assert
            Assert.Null(result);
        }
    }
}
