using DrSasuMcp.AzureDevOps.AzureDevOps.Analyzers;
using DrSasuMcp.AzureDevOps.AzureDevOps.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace DrSasuMcp.Tests.AzureDevOps.Analyzers
{
    public class CodeQualityAnalyzerTests
    {
        private readonly CodeQualityAnalyzer _analyzer;

        public CodeQualityAnalyzerTests()
        {
            var loggerMock = new Mock<ILogger<CodeQualityAnalyzer>>();
            _analyzer = new CodeQualityAnalyzer(loggerMock.Object);
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_DeletedFile_ReturnsEmpty()
        {
            var fileChange = new FileChange
            {
                FilePath = "test.cs",
                ChangeType = ChangeType.Deleted,
                OriginalContent = "var x = 1;"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.Empty(comments);
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_TodoComment_ReturnsQual002()
        {
            var fileChange = new FileChange
            {
                FilePath = "service.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = "// TODO: fix this later\nvar x = 1;"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.Contains(comments, c => c.Code == "QUAL002");
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_LongLine_ReturnsQual003()
        {
            var longLine = new string('a', 130);
            var fileChange = new FileChange
            {
                FilePath = "file.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = longLine
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.Contains(comments, c => c.Code == "QUAL003");
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_UnsupportedFileType_ReturnsEmpty()
        {
            var fileChange = new FileChange
            {
                FilePath = "readme.md",
                ChangeType = ChangeType.Modified,
                ModifiedContent = "// TODO: lots of magic numbers 9999"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.Empty(comments);
        }

        [Theory]
        [InlineData(".cs", true)]
        [InlineData(".js", true)]
        [InlineData(".ts", true)]
        [InlineData(".java", true)]
        [InlineData(".py", true)]
        [InlineData(".go", true)]
        [InlineData(".md", false)]
        [InlineData(".txt", false)]
        [InlineData(".json", false)]
        public void SupportsFileType_VariousExtensions_ReturnsExpected(string extension, bool expected)
        {
            var result = _analyzer.SupportsFileType($"file{extension}");
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_ComplexityKeywords_PrecompiledPatternsWork()
        {
            // Verify pre-compiled complexity patterns fire correctly
            var fileChange = new FileChange
            {
                FilePath = "complex.cs",
                ChangeType = ChangeType.Modified,
                // A single line with 5+ complexity keywords triggers QUAL006
                ModifiedContent = "if (a && b || c) { for (int i = 0; i < n; i++) { foreach (var x in y) {} } }"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.Contains(comments, c => c.Code == "QUAL006");
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_CleanCode_NoQualityIssues()
        {
            var fileChange = new FileChange
            {
                FilePath = "clean.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = @"
public class OrderService
{
    private readonly IOrderRepository _repo;

    public OrderService(IOrderRepository repo)
    {
        _repo = repo;
    }

    public async Task<Order> GetOrderAsync(int id)
    {
        return await _repo.GetByIdAsync(id);
    }
}"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            // Clean code should have no errors; warnings/info ok but no critical
            Assert.DoesNotContain(comments, c => c.Level == IssueLevel.Critical);
        }
    }
}
