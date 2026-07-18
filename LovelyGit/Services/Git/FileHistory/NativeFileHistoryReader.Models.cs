using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.FileHistory;

internal static partial class NativeFileHistoryReader
{
    private readonly record struct HistoryWorkItem(
        GitObjectId Hash,
        string Path,
        ReadOnlyMemory<byte> EncodedPath,
        GitCommitAncestryHeader Header,
        GitTreeFile? Current);

    private readonly record struct HistoryKey(GitObjectId Hash, string Path);

    private readonly record struct FileChange(
        FileHistoryChangeKind Kind,
        string? PreviousPath);

    private readonly record struct EdgeResult(
        FileChange? Change,
        string? PreviousPath,
        GitTreeFile? PreviousFile);
}
