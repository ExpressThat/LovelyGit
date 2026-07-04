using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal static class FastDiffLineDefaults
{
    public static CommitFileDiffLine TrimEmptyPayloads(CommitFileDiffLine line)
    {
        line.OldSyntaxSpans = null!;
        line.NewSyntaxSpans = null!;
        line.SyntaxSpans = null!;
        line.OldChangeSpans = null!;
        line.NewChangeSpans = null!;
        line.ChangeSpans = null!;
        return line;
    }
}
