using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class ConflictResolutionService
{
    private static ConflictResolutionResponse BuildCachedVariant(
        ConflictResolutionResponse sibling,
        bool ignoreWhitespace)
    {
        var texts = ConflictTextPayloadBuilder.Expand(sibling);
        var response = new ConflictResolutionResponse
        {
            Path = sibling.Path,
            WorktreeFingerprint = sibling.WorktreeFingerprint,
            Base = CopyVersion(sibling.Base, texts.Base),
            Ours = CopyVersion(sibling.Ours, texts.Ours),
            Theirs = CopyVersion(sibling.Theirs, texts.Theirs),
            Result = CopyVersion(sibling.Result, texts.Result),
            CurrentSource = sibling.CurrentSource,
            IncomingSource = sibling.IncomingSource,
            Hunks = sibling.Hunks,
        };
        if (texts.Base is not null && texts.Ours is not null && texts.Theirs is not null)
        {
            var current = ConflictHunkBuilder.BuildLineModel(texts.Base, texts.Ours, ignoreWhitespace);
            var incoming = ConflictHunkBuilder.BuildLineModel(texts.Base, texts.Theirs, ignoreWhitespace);
            response.CurrentComparison = BuildBaseComparison(sibling.Path, texts.Base, texts.Ours, current);
            response.IncomingComparison = BuildBaseComparison(sibling.Path, texts.Base, texts.Theirs, incoming);
        }

        ConflictComparisonPayloadBuilder.Compact(response.CurrentComparison);
        ConflictComparisonPayloadBuilder.Compact(response.IncomingComparison);
        ConflictTextPayloadBuilder.Compact(response);
        return response;
    }

    private static ConflictFileVersion CopyVersion(ConflictFileVersion source, string? text) => new()
    {
        Exists = source.Exists,
        IsBinary = source.IsBinary,
        IsTooLarge = source.IsTooLarge,
        SizeBytes = source.SizeBytes,
        Text = text,
    };
}
