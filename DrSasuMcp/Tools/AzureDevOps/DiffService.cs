using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using DrSasuMcp.Tools.AzureDevOps.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using DiffPlexChangeType = DiffPlex.DiffBuilder.Model.ChangeType;

namespace DrSasuMcp.Tools.AzureDevOps
{
    /// <summary>
    /// Implementation of diff service using DiffPlex library.
    /// </summary>
    public class DiffService : IDiffService
    {
        private readonly ILogger<DiffService> _logger;
        private readonly InlineDiffBuilder _inlineDiffBuilder;
        private readonly SideBySideDiffBuilder _sideBySideDiffBuilder;

        public DiffService(ILogger<DiffService> logger)
        {
            _logger = logger;
            var differ = new Differ();
            _inlineDiffBuilder = new InlineDiffBuilder(differ);
            _sideBySideDiffBuilder = new SideBySideDiffBuilder(differ);
        }

        /// <inheritdoc/>
        public DiffResultModel GenerateUnifiedDiff(string filePath, string? oldText, string? newText)
        {
            _logger.LogDebug("Generating unified diff for {FilePath}", filePath);

            var diffModel = _inlineDiffBuilder.BuildDiffModel(oldText ?? string.Empty, newText ?? string.Empty);
            var statistics = CalculateStatistics(oldText, newText);

            var diffContent = new StringBuilder();
            var diffLines = new List<DiffLine>();

            diffContent.AppendLine($"--- a/{filePath}");
            diffContent.AppendLine($"+++ b/{filePath}");

            int oldLineNum = 1;
            int newLineNum = 1;

            foreach (var line in diffModel.Lines)
            {
                var diffLineType = MapDiffLineType(line.Type);
                
                switch (line.Type)
                {
                    case DiffPlexChangeType.Unchanged:
                        diffContent.AppendLine($" {line.Text}");
                        diffLines.Add(new DiffLine
                        {
                            OldLineNumber = oldLineNum,
                            NewLineNumber = newLineNum,
                            Content = line.Text,
                            Type = DiffLineType.Unchanged
                        });
                        oldLineNum++;
                        newLineNum++;
                        break;

                    case DiffPlexChangeType.Deleted:
                        diffContent.AppendLine($"-{line.Text}");
                        diffLines.Add(new DiffLine
                        {
                            OldLineNumber = oldLineNum,
                            NewLineNumber = null,
                            Content = line.Text,
                            Type = DiffLineType.Deleted
                        });
                        oldLineNum++;
                        break;

                    case DiffPlexChangeType.Inserted:
                        diffContent.AppendLine($"+{line.Text}");
                        diffLines.Add(new DiffLine
                        {
                            OldLineNumber = null,
                            NewLineNumber = newLineNum,
                            Content = line.Text,
                            Type = DiffLineType.Added
                        });
                        newLineNum++;
                        break;

                    case DiffPlexChangeType.Modified:
                        diffContent.AppendLine($"-{line.Text}");
                        diffContent.AppendLine($"+{line.Text}");
                        diffLines.Add(new DiffLine
                        {
                            OldLineNumber = oldLineNum,
                            NewLineNumber = newLineNum,
                            Content = line.Text,
                            Type = DiffLineType.Modified
                        });
                        oldLineNum++;
                        newLineNum++;
                        break;
                }
            }

            return new DiffResultModel
            {
                FilePath = filePath,
                DiffFormat = "unified",
                DiffContent = diffContent.ToString(),
                Statistics = statistics,
                Lines = diffLines
            };
        }

        /// <inheritdoc/>
        public DiffResultModel GenerateSideBySideDiff(string filePath, string? oldText, string? newText)
        {
            _logger.LogDebug("Generating side-by-side diff for {FilePath}", filePath);

            var diffModel = _sideBySideDiffBuilder.BuildDiffModel(oldText ?? string.Empty, newText ?? string.Empty);
            var statistics = CalculateStatistics(oldText, newText);

            var diffContent = new StringBuilder();
            var diffLines = new List<DiffLine>();

            diffContent.AppendLine($"Side-by-Side Diff: {filePath}");
            diffContent.AppendLine(new string('=', 80));
            diffContent.AppendLine($"{"Old",-38} | {"New",-38}");
            diffContent.AppendLine(new string('-', 80));

            int maxLines = Math.Max(diffModel.OldText.Lines.Count, diffModel.NewText.Lines.Count);

            for (int i = 0; i < maxLines; i++)
            {
                var oldLine = i < diffModel.OldText.Lines.Count ? diffModel.OldText.Lines[i] : null;
                var newLine = i < diffModel.NewText.Lines.Count ? diffModel.NewText.Lines[i] : null;

                var oldText_Line = oldLine?.Text ?? string.Empty;
                var newText_Line = newLine?.Text ?? string.Empty;

                var oldPrefix = oldLine?.Type == DiffPlexChangeType.Deleted ? "-" :
                               oldLine?.Type == DiffPlexChangeType.Modified ? "~" : " ";
                var newPrefix = newLine?.Type == DiffPlexChangeType.Inserted ? "+" :
                               newLine?.Type == DiffPlexChangeType.Modified ? "~" : " ";

                diffContent.AppendLine($"{oldPrefix}{oldText_Line,-37} | {newPrefix}{newText_Line,-37}");

                // Add to structured diff lines
                if (oldLine != null)
                {
                    diffLines.Add(new DiffLine
                    {
                        OldLineNumber = i + 1,
                        NewLineNumber = newLine != null ? i + 1 : null,
                        Content = oldText_Line,
                        Type = MapDiffLineType(oldLine.Type)
                    });
                }
            }

            return new DiffResultModel
            {
                FilePath = filePath,
                DiffFormat = "sidebyside",
                DiffContent = diffContent.ToString(),
                Statistics = statistics,
                Lines = diffLines
            };
        }

        /// <inheritdoc/>
        public DiffResultModel GenerateInlineDiff(string filePath, string? oldText, string? newText)
        {
            _logger.LogDebug("Generating inline diff for {FilePath}", filePath);

            // For inline, we use the unified approach but with better formatting
            var diffModel = _inlineDiffBuilder.BuildDiffModel(oldText ?? string.Empty, newText ?? string.Empty);
            var statistics = CalculateStatistics(oldText, newText);

            var diffContent = new StringBuilder();
            var diffLines = new List<DiffLine>();

            diffContent.AppendLine($"Inline Diff: {filePath}");
            diffContent.AppendLine(new string('=', 80));

            int lineNum = 1;

            foreach (var line in diffModel.Lines)
            {
                string prefix = line.Type switch
                {
                    DiffPlexChangeType.Deleted => "- ",
                    DiffPlexChangeType.Inserted => "+ ",
                    DiffPlexChangeType.Modified => "~ ",
                    _ => "  "
                };

                diffContent.AppendLine($"{lineNum,4} {prefix}{line.Text}");

                diffLines.Add(new DiffLine
                {
                    OldLineNumber = line.Type != DiffPlexChangeType.Inserted ? lineNum : null,
                    NewLineNumber = line.Type != DiffPlexChangeType.Deleted ? lineNum : null,
                    Content = line.Text,
                    Type = MapDiffLineType(line.Type)
                });

                lineNum++;
            }

            return new DiffResultModel
            {
                FilePath = filePath,
                DiffFormat = "inline",
                DiffContent = diffContent.ToString(),
                Statistics = statistics,
                Lines = diffLines
            };
        }

        /// <inheritdoc/>
        public DiffStatistics CalculateStatistics(string? oldText, string? newText)
        {
            var oldLines = (oldText ?? string.Empty).Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var newLines = (newText ?? string.Empty).Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            var diffModel = _inlineDiffBuilder.BuildDiffModel(oldText ?? string.Empty, newText ?? string.Empty);

            var added = diffModel.Lines.Count(l => l.Type == DiffPlexChangeType.Inserted);
            var deleted = diffModel.Lines.Count(l => l.Type == DiffPlexChangeType.Deleted);
            var modified = diffModel.Lines.Count(l => l.Type == DiffPlexChangeType.Modified);
            var unchanged = diffModel.Lines.Count(l => l.Type == DiffPlexChangeType.Unchanged);
            var totalLines = Math.Max(oldLines.Length, newLines.Length);

            var changePercentage = totalLines > 0 ? ((added + deleted + modified) / (double)totalLines) * 100 : 0;

            return new DiffStatistics
            {
                TotalLines = totalLines,
                AddedLines = added,
                DeletedLines = deleted,
                ModifiedLines = modified,
                UnchangedLines = unchanged,
                ChangePercentage = Math.Round(changePercentage, 2)
            };
        }

        private static DiffLineType MapDiffLineType(DiffPlexChangeType changeType)
        {
            return changeType switch
            {
                DiffPlexChangeType.Deleted => DiffLineType.Deleted,
                DiffPlexChangeType.Inserted => DiffLineType.Added,
                DiffPlexChangeType.Modified => DiffLineType.Modified,
                _ => DiffLineType.Unchanged
            };
        }
    }
}

