using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class ConflictResolutionService
{
    private static ConflictDiffModels PrepareDiffModels(
        string baseText,
        string currentText,
        string incomingText,
        bool ignoreWhitespace)
    {
        var baseLines = ConflictHunkBuilder.PrepareLineText(baseText);
        var currentLines = ConflictHunkBuilder.PrepareLineText(currentText, baseLines);
        var incomingLines = ConflictHunkBuilder.PrepareLineText(incomingText, baseLines);
        var currentHunk = ConflictHunkBuilder.BuildLineModel(baseLines, currentLines);
        var incomingHunk = ConflictHunkBuilder.BuildLineModel(baseLines, incomingLines);
        return new(
            currentHunk,
            incomingHunk,
            ignoreWhitespace
                ? ConflictHunkBuilder.BuildLineModel(baseLines, currentLines, ignoreWhitespace: true)
                : currentHunk,
            ignoreWhitespace
                ? ConflictHunkBuilder.BuildLineModel(baseLines, incomingLines, ignoreWhitespace: true)
                : incomingHunk);
    }

    private sealed record ConflictDiffModels(
        LineDiffModel CurrentHunk,
        LineDiffModel IncomingHunk,
        LineDiffModel CurrentComparison,
        LineDiffModel IncomingComparison);
}
