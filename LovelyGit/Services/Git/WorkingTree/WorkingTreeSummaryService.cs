using System.Text;
using CliWrap;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class WorkingTreeSummaryService
{
    private readonly GitCliService _gitCliService;
    private readonly WorkingTreePreliminarySummaryService _preliminarySummaryService;

    public WorkingTreeSummaryService(
        GitCliService gitCliService,
        WorkingTreePreliminarySummaryService preliminarySummaryService)
    {
        _gitCliService = gitCliService;
        _preliminarySummaryService = preliminarySummaryService;
    }

    public async Task<WorkingTreeChangeSummaryResponse> GetSummaryAsync(
        string repositoryPath,
        CancellationToken cancellationToken,
        bool allowIncomplete = false)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        if (allowIncomplete)
        {
            return await _preliminarySummaryService
                .GetSummaryAsync(paths.WorkTreeDirectory, paths.WorktreeGitDirectory, cancellationToken)
                .ConfigureAwait(false);
        }

        var counter = new PorcelainStatusCounter();
        var standardError = new StringBuilder();
        var result = await _gitCliService
            .CreateCommand(
                ["--no-optional-locks", "status", "--porcelain=v1", "-z", "--untracked-files=all"],
                paths.WorkTreeDirectory,
                validateExitCode: false)
            .WithStandardOutputPipe(PipeTarget.ToStream(counter))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(standardError))
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(FirstNonEmptyLine(standardError.ToString())
                ?? "Git status summary failed.");
        }

        return new WorkingTreeChangeSummaryResponse
        {
            TotalCount = counter.Count,
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
