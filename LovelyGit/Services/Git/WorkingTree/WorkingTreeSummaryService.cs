using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class WorkingTreeSummaryService
{
    private readonly GitCliService _gitCliService;

    public WorkingTreeSummaryService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task<WorkingTreeChangeSummaryResponse> GetSummaryAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var result = await _gitCliService
            .ExecuteBufferedAsync(
                ["--no-optional-locks", "status", "--porcelain=v1", "-z", "--untracked-files=all"],
                repositoryPaths.WorkTreeDirectory,
                validateExitCode: false,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            var message = FirstNonEmptyLine(result.StandardError)
                ?? FirstNonEmptyLine(result.StandardOutput)
                ?? "Git status failed.";
            throw new InvalidOperationException(message);
        }

        return new WorkingTreeChangeSummaryResponse
        {
            TotalCount = CountPorcelainRecords(result.StandardOutput.AsSpan()),
        };
    }

    internal static int CountPorcelainRecords(ReadOnlySpan<char> output)
    {
        var count = 0;
        var offset = 0;
        while (offset < output.Length)
        {
            if (output.Length - offset < 4)
            {
                break;
            }

            var x = output[offset];
            var y = output[offset + 1];
            offset += 3;
            offset = AdvancePastNul(output, offset);
            if (x is 'R' or 'C' || y is 'R' or 'C')
            {
                offset = AdvancePastNul(output, offset);
            }

            count++;
        }

        return count;
    }

    private static int AdvancePastNul(ReadOnlySpan<char> output, int offset)
    {
        var remaining = output[offset..];
        var nulIndex = remaining.IndexOf('\0');
        return nulIndex < 0 ? output.Length : offset + nulIndex + 1;
    }

    private static string? FirstNonEmptyLine(string text)
    {
        foreach (var line in text.AsSpan().EnumerateLines())
        {
            var trimmed = line.Trim();
            if (!trimmed.IsEmpty)
            {
                return trimmed.ToString();
            }
        }

        return null;
    }
}
