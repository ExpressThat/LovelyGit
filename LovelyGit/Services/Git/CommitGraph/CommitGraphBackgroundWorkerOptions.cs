namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed record CommitGraphBackgroundWorkerOptions(
    bool EnableCommitGraphCacheWorker,
    bool EnableCommitDetailsPreloadWorker,
    bool EnableCommitFileDiffPreparationWorker);
