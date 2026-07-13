namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed partial class GitRepositoryOperationService
{
    private static string[] BuildCommitOperationArguments(
        string operation,
        IReadOnlyList<string> commitHashes)
    {
        if (commitHashes.Count is < 1 or > 100)
        {
            throw new ArgumentException(
                "Select between one and 100 commits.", nameof(commitHashes));
        }

        var arguments = new string[commitHashes.Count + 3];
        arguments[0] = operation;
        arguments[1] = "--no-edit";
        arguments[2] = "--";
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < commitHashes.Count; index++)
        {
            var hash = NormalizeCommitHash(commitHashes[index]);
            if (!seen.Add(hash))
            {
                throw new ArgumentException(
                    "A commit can only be selected once.", nameof(commitHashes));
            }
            arguments[index + 3] = hash;
        }
        return arguments;
    }

    private static string NormalizeCommitHash(string commitHash)
    {
        var normalized = commitHash.Trim();
        if ((normalized.Length is not 40 and not 64)
            || normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("Commit hash is not valid.", nameof(commitHash));
        }

        return normalized;
    }
}
