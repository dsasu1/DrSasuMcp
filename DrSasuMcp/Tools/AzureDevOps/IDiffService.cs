using DrSasuMcp.Tools.AzureDevOps.Models;

namespace DrSasuMcp.Tools.AzureDevOps
{
    /// <summary>
    /// Service interface for generating file diffs using DiffPlex.
    /// </summary>
    public interface IDiffService
    {
        /// <summary>
        /// Generates a unified diff (traditional patch format).
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="oldText">The original text.</param>
        /// <param name="newText">The modified text.</param>
        /// <returns>A diff result with unified format.</returns>
        DiffResultModel GenerateUnifiedDiff(string filePath, string? oldText, string? newText);

        /// <summary>
        /// Generates a side-by-side diff for visual comparison.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="oldText">The original text.</param>
        /// <param name="newText">The modified text.</param>
        /// <returns>A diff result with side-by-side format.</returns>
        DiffResultModel GenerateSideBySideDiff(string filePath, string? oldText, string? newText);

        /// <summary>
        /// Generates an inline diff with embedded changes.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="oldText">The original text.</param>
        /// <param name="newText">The modified text.</param>
        /// <returns>A diff result with inline format.</returns>
        DiffResultModel GenerateInlineDiff(string filePath, string? oldText, string? newText);

        /// <summary>
        /// Calculates statistics about the changes between two texts.
        /// </summary>
        /// <param name="oldText">The original text.</param>
        /// <param name="newText">The modified text.</param>
        /// <returns>Statistics about the diff.</returns>
        DiffStatistics CalculateStatistics(string? oldText, string? newText);
    }
}

