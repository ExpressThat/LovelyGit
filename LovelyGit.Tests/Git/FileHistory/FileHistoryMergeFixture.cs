using System.Text;
using CliWrap;
using ExpressThat.LovelyGit.Services.Git.Cli;
using LovelyGit.Tests.Git.WorkingTree;

namespace LovelyGit.Tests.Git.FileHistory;

internal sealed class FileHistoryMergeFixture : IDisposable
{
    private readonly TemporaryDirectory _directory;

    private FileHistoryMergeFixture(TemporaryDirectory directory, string head)
    {
        _directory = directory;
        Head = head;
    }

    public string Head { get; }
    public string Path => _directory.Path;

    public static FileHistoryMergeFixture Create()
    {
        var directory = TemporaryDirectory.Create("lovelygit-history-merge-");
        InitializedRepositoryTemplate.CopyInto(new DirectoryInfo(directory.Path), "master");
        var git = new GitCliService();
        git.CreateCommand(["fast-import", "--quiet"], directory.Path)
            .WithStandardInputPipe(PipeSource.FromString(BuildImport(), Encoding.UTF8))
            .ExecuteAsync().GetAwaiter().GetResult();
        var head = File.ReadAllText(
            System.IO.Path.Combine(directory.Path, ".git", "refs", "heads", "history-test"))
            .Trim();
        return new FileHistoryMergeFixture(directory, head);
    }

    public void Dispose() => _directory.Dispose();

    private static string BuildImport()
    {
        var builder = new StringBuilder();
        AppendCommit(builder, "base file", 1, null, null, "tracked.txt", "base");
        AppendCommit(builder, "main work", 2, 1, null, "main.txt", "main");
        AppendCommit(builder, "topic edit", 3, 1, null, "tracked.txt", "topic");
        AppendCommit(builder, "merge topic", 4, 2, 3, "tracked.txt", "topic");
        return builder.AppendLine("done").ToString().ReplaceLineEndings("\n");
    }

    private static void AppendCommit(
        StringBuilder builder,
        string subject,
        int mark,
        int? parent,
        int? mergeParent,
        string path,
        string content)
    {
        builder.AppendLine("commit refs/heads/history-test")
            .Append("mark :").AppendLine(mark.ToString())
            .AppendLine("author LovelyGit Test <test@example.invalid> 1700000000 +0000")
            .AppendLine("committer LovelyGit Test <test@example.invalid> 1700000000 +0000")
            .Append("data ").AppendLine(subject.Length.ToString()).AppendLine(subject);
        if (parent.HasValue) builder.Append("from :").AppendLine(parent.Value.ToString());
        if (mergeParent.HasValue) builder.Append("merge :").AppendLine(mergeParent.Value.ToString());
        builder.Append("M 100644 inline ").AppendLine(path)
            .Append("data ").AppendLine(content.Length.ToString()).AppendLine(content)
            .AppendLine();
    }
}
