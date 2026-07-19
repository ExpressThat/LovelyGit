using System.Text;
using CliWrap;
using CliWrap.Buffered;
using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git;

internal static class GitFastImportFixtureSeeder
{
    private const string Identity =
        "LovelyGit Test <lovelygit@example.invalid> 1704067200 +0000";

    public static Task SeedOctopusAsync(
        string repositoryPath,
        string baseHash,
        CancellationToken cancellationToken = default)
    {
        var import = new StringBuilder();
        for (var index = 1; index <= 3; index++)
        {
            AppendCommit(
                import,
                $"refs/heads/{ToBranchName(index)}",
                index,
                ToBranchName(index),
                baseHash);
        }

        AppendCommit(
            import,
            "refs/heads/master",
            4,
            "Octopus merge",
            baseHash,
            mergeMarkCount: 3);
        import.Append("done\n");
        return RunAsync(repositoryPath, import, cancellationToken);
    }

    public static Task SeedIndependentTagTipsAsync(
        string repositoryPath,
        int count,
        CancellationToken cancellationToken = default)
    {
        var import = new StringBuilder();
        for (var index = 0; index < count; index++)
        {
            var mark = index + 1;
            AppendCommit(
                import,
                $"refs/heads/tag-source-{index}",
                mark,
                $"tag history {index}");
            import.Append("reset refs/tags/history-tag-")
                .Append(index)
                .Append("\nfrom :")
                .Append(mark)
                .Append("\n\n");
        }
        import.Append("done\n");
        return RunAsync(repositoryPath, import, cancellationToken);
    }

    public static async Task<string> AppendPackedGenerationAsync(
        string repositoryPath,
        string parentHash,
        int generation,
        CancellationToken cancellationToken = default)
    {
        var message = $"pack generation {generation}";
        var content = $"generation {generation}";
        var path = generation == 1 ? "packed.txt" : $"packed-{generation}.txt";
        var import = new StringBuilder()
            .Append("commit refs/heads/master\nmark :1\nauthor ")
            .Append(Identity)
            .Append("\ncommitter ")
            .Append(Identity)
            .Append("\ndata ")
            .Append(Encoding.UTF8.GetByteCount(message))
            .Append('\n').Append(message)
            .Append("\nfrom ").Append(parentHash)
            .Append("\nM 100644 inline ").Append(path)
            .Append("\ndata ").Append(Encoding.UTF8.GetByteCount(content))
            .Append('\n').Append(content)
            .Append("\n\nget-mark :1\ndone\n");
        var result = await new GitCliService()
            .CreateCommand(["fast-import", "--quiet"], repositoryPath)
            .WithStandardInputPipe(PipeSource.FromString(import.ToString(), Encoding.UTF8))
            .ExecuteBufferedAsync(cancellationToken)
            .ConfigureAwait(false);
        return result.StandardOutput.Trim();
    }

    private static void AppendCommit(
        StringBuilder import,
        string reference,
        int mark,
        string message,
        string? parent = null,
        int mergeMarkCount = 0)
    {
        import.Append("commit ").Append(reference)
            .Append("\nmark :").Append(mark)
            .Append("\nauthor ").Append(Identity)
            .Append("\ncommitter ").Append(Identity)
            .Append("\ndata ").Append(Encoding.UTF8.GetByteCount(message))
            .Append('\n').Append(message).Append('\n');
        if (parent != null)
        {
            import.Append("from ").Append(parent).Append('\n');
        }
        for (var mergeMark = 1; mergeMark <= mergeMarkCount; mergeMark++)
        {
            import.Append("merge :").Append(mergeMark).Append('\n');
        }
        import.Append('\n');
    }

    private static async Task RunAsync(
        string repositoryPath,
        StringBuilder import,
        CancellationToken cancellationToken)
    {
        await new GitCliService()
            .CreateCommand(["fast-import", "--quiet"], repositoryPath)
            .WithStandardInputPipe(PipeSource.FromString(import.ToString(), Encoding.UTF8))
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static string ToBranchName(int index) => index switch
    {
        1 => "one",
        2 => "two",
        _ => "three",
    };
}
