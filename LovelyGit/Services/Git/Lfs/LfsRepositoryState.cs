using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.Lfs;

[TypeSharp]
public sealed record LfsRepositoryState
{
    public bool IsAvailable { get; set; }
    public bool IsInitialized { get; set; }
    public bool HasTrackedPatterns { get; set; }
    public List<string> TrackedPatterns { get; set; } = [];
}
