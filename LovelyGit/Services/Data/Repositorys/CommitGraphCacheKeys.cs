namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

internal static class CommitGraphCacheKeys
{
    public static string MakeRepositoryHashId(Guid repositoryId, string hash)
    {
        return string.Concat(repositoryId, ":", hash);
    }

    public static string MakeRepositoryRowId(Guid repositoryId, int rowIndex)
    {
        return string.Concat(repositoryId, ":", rowIndex);
    }

    public static string MakeRepositoryCommitFileId(Guid repositoryId, string hash, int fileIndex)
    {
        return string.Concat(repositoryId, ":", hash, ":", fileIndex);
    }
}
