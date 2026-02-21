using DrSasuMcp.AzureDevOps.AzureDevOps.Analyzers;
using DrSasuMcp.AzureDevOps.AzureDevOps.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace DrSasuMcp.Tests.AzureDevOps.Analyzers
{
    public class BestPracticesAnalyzerTests
    {
        private readonly BestPracticesAnalyzer _analyzer;

        public BestPracticesAnalyzerTests()
        {
            var loggerMock = new Mock<ILogger<BestPracticesAnalyzer>>();
            _analyzer = new BestPracticesAnalyzer(loggerMock.Object);
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_DeletedFile_ReturnsEmpty()
        {
            var fileChange = new FileChange
            {
                FilePath = "test.cs",
                ChangeType = ChangeType.Deleted,
                OriginalContent = "var x = new HttpClient();"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.Empty(comments);
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_BlockingResult_ReturnsBP003()
        {
            var fileChange = new FileChange
            {
                FilePath = "service.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = "var data = GetDataAsync().Result;"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.Contains(comments, c => c.Code == "BP003");
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_BlockingWait_ReturnsBP004()
        {
            var fileChange = new FileChange
            {
                FilePath = "service.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = "task.Wait();"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.Contains(comments, c => c.Code == "BP004");
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_NewHttpClient_ReturnsBP009()
        {
            var fileChange = new FileChange
            {
                FilePath = "repo.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = "var client = new HttpClient();"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.Contains(comments, c => c.Code == "BP009");
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_ConsoleWriteLine_ReturnsBP010()
        {
            var fileChange = new FileChange
            {
                FilePath = "app.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = "Console.WriteLine(\"Hello world\");"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.Contains(comments, c => c.Code == "BP010");
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_NullCheckEquality_ReturnsBP006()
        {
            var fileChange = new FileChange
            {
                FilePath = "app.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = "if (obj == null) throw new ArgumentNullException();"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.Contains(comments, c => c.Code == "BP006");
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_StringConcatInLoop_ReturnsBP005()
        {
            // BP005: string initialized to empty, then += inside a loop
            var fileChange = new FileChange
            {
                FilePath = "builder.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = @"
string result = """";
foreach (var item in items)
{
    result += item.Name;
}"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.Contains(comments, c => c.Code == "BP005" && c.Level == IssueLevel.Warning);
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_StringConcatNotInLoop_NoBP005()
        {
            // string += outside any loop should not trigger BP005
            var fileChange = new FileChange
            {
                FilePath = "builder.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = @"
string result = """";
result += "" world"";"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.DoesNotContain(comments, c => c.Code == "BP005");
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_DisposableWithoutUsing_ReturnsBP012()
        {
            var fileChange = new FileChange
            {
                FilePath = "repo.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = "var stream = new FileStream(path, FileMode.Open);"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.Contains(comments, c => c.Code == "BP012");
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_DisposableInsideUsing_NoBP012()
        {
            var fileChange = new FileChange
            {
                FilePath = "repo.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = "using var stream = new FileStream(path, FileMode.Open);"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.DoesNotContain(comments, c => c.Code == "BP012");
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_GenericException_ReturnsBP013()
        {
            var fileChange = new FileChange
            {
                FilePath = "app.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = "catch (Exception ex) { _logger.LogError(ex, \"Error\"); }"
            };

            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            Assert.Contains(comments, c => c.Code == "BP013");
        }

        [Theory]
        [InlineData(".cs", true)]
        [InlineData(".js", true)]
        [InlineData(".ts", true)]
        [InlineData(".java", true)]
        [InlineData(".py", true)]
        [InlineData(".md", false)]
        [InlineData(".json", false)]
        public void SupportsFileType_VariousExtensions_ReturnsExpected(string extension, bool expected)
        {
            var result = _analyzer.SupportsFileType($"file{extension}");
            Assert.Equal(expected, result);
        }
    }
}
