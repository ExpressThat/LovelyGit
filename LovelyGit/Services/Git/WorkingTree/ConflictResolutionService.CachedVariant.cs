using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class ConflictResolutionService
{
    internal static ConflictResolutionResponse BuildCachedVariant(
        ConflictResolutionResponse sibling,
        ConflictTexts? retainedTexts,
        bool ignoreWhitespace)
    {
        var texts = retainedTexts ?? ConflictTextPayloadBuilder.Expand(sibling);
        var hasCompactBundle = sibling.CompactTextBundleGzipBase64 is not null;
        var response = new ConflictResolutionResponse
        {
            Path = sibling.Path,
            WorktreeFingerprint = sibling.WorktreeFingerprint,
            CompactTextSchema = sibling.CompactTextSchema,
            CompactTextBundleGzipBase64 = sibling.CompactTextBundleGzipBase64,
            Base = CopyVersion(sibling.Base, hasCompactBundle ? null : texts.Base),
            Ours = CopyVersion(sibling.Ours, hasCompactBundle ? null : texts.Ours),
            Theirs = CopyVersion(sibling.Theirs, hasCompactBundle ? null : texts.Theirs),
            Result = CopyVersion(sibling.Result, hasCompactBundle ? null : texts.Result),
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
