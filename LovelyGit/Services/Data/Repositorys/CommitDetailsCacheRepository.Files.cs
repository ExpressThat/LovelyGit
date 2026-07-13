using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

internal sealed partial class CommitDetailsCacheRepository
{
    private static List<CommitChangedFileCacheEntry> BuildChangedFileEntries(
        Guid repositoryId,
        string hash,
        IReadOnlyList<CommitChangedFile> files)
    {
        var entries = new List<CommitChangedFileCacheEntry>(files.Count);
        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];
            entries.Add(new CommitChangedFileCacheEntry
            {
                Id = CommitGraphCacheKeys.MakeRepositoryCommitFileId(repositoryId, hash, index),
                RepositoryId = repositoryId,
                Hash = hash,
                FileIndex = index,
                File = new CommitChangedFileCache
                {
                    Path = file.Path,
                    Status = file.Status,
                    Additions = file.Additions,
                    Deletions = file.Deletions,
                    IsBinary = file.IsBinary,
                },
            });
        }

        return entries;
    }
}
