using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeIndexService
{
    public async Task CommitStagedChangesAsync(
        string repositoryPath,
        string title,
        string body,
        bool amend,
        bool sign,
        CancellationToken cancellationToken)
    {
        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length == 0)
        {
            throw new InvalidOperationException("Commit title is required.");
        }

        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var arguments = BuildCommitArguments(trimmedTitle, body.Trim(), amend, sign);
        var result = await _gitCliService
            .ExecuteBufferedAsync(
                arguments,
                repositoryPaths.WorkTreeDirectory,
                validateExitCode: false,
                cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode == 0)
        {
            return;
        }

        var message = FirstNonEmptyLine(result.StandardError)
            ?? FirstNonEmptyLine(result.StandardOutput)
            ?? "Git could not create the commit.";
        throw new InvalidOperationException(sign
            ? $"Git could not sign the commit. {message} Configure user.signingKey and gpg.format, then try again."
            : message);
    }

    private static string[] BuildCommitArguments(string title, string body, bool amend, bool sign)
    {
        var arguments = new List<string>(amend ? 8 : 6) { "commit" };
        if (amend)
        {
            arguments.Add("--amend");
            arguments.Add("--allow-empty");
        }
        if (sign)
        {
            arguments.Add("--gpg-sign");
        }

        arguments.Add("-m");
        arguments.Add(title);
        if (body.Length > 0)
        {
            arguments.Add("-m");
            arguments.Add(body);
        }

        return arguments.ToArray();
    }
}
