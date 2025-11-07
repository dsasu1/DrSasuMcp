using DrSasuMcp.Tools.AzureDevOps.Models;

namespace DrSasuMcp.Tools.AzureDevOps.Analyzers
{
    /// <summary>
    /// Interface for code analyzers that review file changes.
    /// </summary>
    public interface ICodeAnalyzer
    {
        /// <summary>
        /// Gets the name of the analyzer.
        /// </summary>
        string AnalyzerName { get; }

        /// <summary>
        /// Analyzes a file change and returns review comments.
        /// </summary>
        /// <param name="fileChange">The file change to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of review comments.</returns>
        Task<List<ReviewComment>> AnalyzeFileChangeAsync(
            FileChange fileChange,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if this analyzer supports the given file type.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <returns>True if the file type is supported, false otherwise.</returns>
        bool SupportsFileType(string filePath);
    }
}

