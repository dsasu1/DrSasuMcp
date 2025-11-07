using DrSasuMcp.Tools.AzureDevOps.Analyzers;
using DrSasuMcp.Tools.AzureDevOps.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DrSasuMcp.Tests.AzureDevOps.Analyzers
{
    public class SecurityAnalyzerTests
    {
        private readonly SecurityAnalyzer _analyzer;

        public SecurityAnalyzerTests()
        {
            var loggerMock = new Mock<ILogger<SecurityAnalyzer>>();
            _analyzer = new SecurityAnalyzer(loggerMock.Object);
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_HardcodedPassword_ReturnsComment()
        {
            // Arrange
            var fileChange = new FileChange
            {
                FilePath = "test.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = @"
                    var password = ""MySecretPassword123"";
                    var connection = Connect(password);
                "
            };

            // Act
            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            // Assert
            Assert.NotEmpty(comments);
            var comment = comments.First(c => c.Code == "SEC001");
            Assert.Equal(IssueLevel.Critical, comment.Level);
            Assert.Contains("hardcoded password", comment.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_HardcodedApiKey_ReturnsComment()
        {
            // Arrange
            var fileChange = new FileChange
            {
                FilePath = "config.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = @"
                    var apiKey = ""sk-1234567890abcdef"";
                    var api_key = ""another-key"";
                "
            };

            // Act
            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            // Assert
            Assert.NotEmpty(comments);
            Assert.Contains(comments, c => c.Code == "SEC002");
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_SqlInjection_ReturnsComment()
        {
            // Arrange
            var fileChange = new FileChange
            {
                FilePath = "repository.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = @"
                    var query = ""SELECT * FROM Users WHERE name = "" + userName;
                    ExecuteQuery(query);
                "
            };

            // Act
            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            // Assert
            Assert.NotEmpty(comments);
            Assert.Contains(comments, c => c.Code == "SEC004" && c.Message.Contains("SQL injection"));
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_WeakCrypto_ReturnsComment()
        {
            // Arrange
            var fileChange = new FileChange
            {
                FilePath = "crypto.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = @"
                    var hasher = new MD5CryptoServiceProvider();
                    var hash = hasher.ComputeHash(data);
                "
            };

            // Act
            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            // Assert
            Assert.NotEmpty(comments);
            Assert.Contains(comments, c => c.Code == "SEC005" && c.Level == IssueLevel.Warning);
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_DeletedFile_ReturnsEmpty()
        {
            // Arrange
            var fileChange = new FileChange
            {
                FilePath = "test.cs",
                ChangeType = ChangeType.Deleted,
                OriginalContent = "var password = \"secret\";"
            };

            // Act
            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            // Assert
            Assert.Empty(comments);
        }

        [Theory]
        [InlineData(".cs", true)]
        [InlineData(".js", true)]
        [InlineData(".ts", true)]
        [InlineData(".java", true)]
        [InlineData(".py", true)]
        [InlineData(".txt", false)]
        [InlineData(".md", false)]
        [InlineData(".json", false)]
        public void SupportsFileType_VariousExtensions_ReturnsExpected(string extension, bool expected)
        {
            // Arrange
            var filePath = $"test{extension}";

            // Act
            var result = _analyzer.SupportsFileType(filePath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task AnalyzeFileChangeAsync_CleanCode_ReturnsEmpty()
        {
            // Arrange
            var fileChange = new FileChange
            {
                FilePath = "clean.cs",
                ChangeType = ChangeType.Modified,
                ModifiedContent = @"
                    public class UserService
                    {
                        private readonly ILogger _logger;
                        
                        public UserService(ILogger logger)
                        {
                            _logger = logger;
                        }
                        
                        public async Task<User> GetUserAsync(int id)
                        {
                            _logger.LogInformation(""Getting user {Id}"", id);
                            return await _repository.GetByIdAsync(id);
                        }
                    }
                "
            };

            // Act
            var comments = await _analyzer.AnalyzeFileChangeAsync(fileChange);

            // Assert
            Assert.Empty(comments);
        }
    }
}

