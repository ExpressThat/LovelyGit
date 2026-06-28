using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed record GitIndexStatusScan(
    WorkingTreeChangesResponse Response,
    GitObjectId? RootTreeId,
    HashSet<string> RootTrackedFiles,
    HashSet<string> RootTrackedDirectories);

internal readonly record struct GitIndexStatusEntry(
    string Path,
    int Stage,
    uint FileSize,
    DateTimeOffset ModifiedTime,
    bool SkipWorkTree,
    bool IntentToAdd);
