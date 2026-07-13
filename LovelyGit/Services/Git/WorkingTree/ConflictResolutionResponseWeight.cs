using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class ConflictResolutionResponseWeight
{
    public static long Estimate(
        ConflictResolutionResponse response,
        ConflictTexts? retainedTexts)
    {
        var strings = new HashSet<string>(ReferenceEqualityComparer.Instance);
        Add(strings, response.Path);
        Add(strings, response.WorktreeFingerprint);
        Add(strings, response.CompactTextSchema);
        Add(strings, response.CompactTextBundleGzipBase64);
        Add(strings, response.Base);
        Add(strings, response.Ours);
        Add(strings, response.Theirs);
        Add(strings, response.Result);
        Add(strings, response.CurrentSource);
        Add(strings, response.IncomingSource);
        Add(strings, response.CurrentComparison);
        Add(strings, response.IncomingComparison);
        if (retainedTexts is { } texts)
        {
            Add(strings, texts.Base);
            Add(strings, texts.Ours);
            Add(strings, texts.Theirs);
            Add(strings, texts.Result);
        }

        long characters = 0;
        foreach (var value in strings) characters += value.Length;
        return characters;
    }

    private static void Add(HashSet<string> strings, ConflictFileVersion version)
    {
        Add(strings, version.Text);
        Add(strings, version.TextGzipBase64);
        Add(strings, version.TextEncoding);
    }

    private static void Add(HashSet<string> strings, ConflictSourceMetadata source)
    {
        Add(strings, source.Label);
        Add(strings, source.RefName);
        Add(strings, source.ObjectId);
    }

    private static void Add(HashSet<string> strings, CommitFileDiffResponse? comparison)
    {
        if (comparison is null) return;
        Add(strings, comparison.CommitHash);
        Add(strings, comparison.Path);
        Add(strings, comparison.Status);
        Add(strings, comparison.TruncationMessage);
        Add(strings, comparison.VirtualText);
        Add(strings, comparison.VirtualTextGzipBase64);
        Add(strings, comparison.VirtualTextEncoding);
        Add(strings, comparison.VirtualChangeType);
        Add(strings, comparison.CompactLineSchema);
        Add(strings, comparison.CompactLinesGzipBase64);
        Add(strings, comparison.CompactSourceSchema);
        Add(strings, comparison.CompactSourceBundleGzipBase64);
        foreach (var line in comparison.Lines)
        {
            Add(strings, line.OldText);
            Add(strings, line.NewText);
            Add(strings, line.Text);
            Add(strings, line.ChangeType);
            foreach (var span in line.OldSyntaxSpans) Add(strings, span.Scope);
            foreach (var span in line.NewSyntaxSpans) Add(strings, span.Scope);
            foreach (var span in line.SyntaxSpans) Add(strings, span.Scope);
            foreach (var span in line.OldChangeSpans) Add(strings, span.ChangeType);
            foreach (var span in line.NewChangeSpans) Add(strings, span.ChangeType);
            foreach (var span in line.ChangeSpans) Add(strings, span.ChangeType);
        }
    }

    private static void Add(HashSet<string> strings, string? value)
    {
        if (value is not null) strings.Add(value);
    }
}
