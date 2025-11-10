using DrSasuMcp.Tools.Datadog.Models;

namespace DrSasuMcp.Tools.Datadog.Troubleshooters
{
    /// <summary>
    /// Interface for troubleshooters that analyze issues and provide recommendations.
    /// </summary>
    public interface ITroubleshooter
    {
        /// <summary>
        /// Gets the name of the troubleshooter.
        /// </summary>
        string TroubleshooterName { get; }

        /// <summary>
        /// Checks if this troubleshooter supports the given issue type.
        /// </summary>
        /// <param name="issueType">The issue type to check.</param>
        /// <returns>True if the issue type is supported, false otherwise.</returns>
        bool SupportsIssueType(string issueType);

        /// <summary>
        /// Analyzes a troubleshooting context and returns recommendations.
        /// </summary>
        /// <param name="context">The troubleshooting context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of troubleshooting recommendations.</returns>
        Task<List<TroubleshootingRecommendation>> AnalyzeAsync(
            TroubleshootingContext context,
            CancellationToken cancellationToken = default);
    }
}

