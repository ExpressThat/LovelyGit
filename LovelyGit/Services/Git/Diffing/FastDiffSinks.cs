using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal interface IFastDiffSink
{
    void Unchanged(int oldLine, int newLine, string text);
    void Deleted(int oldLine, string text);
    void Inserted(int newLine, string text);
    void Modified(int oldLine, int newLine, string oldText, string newText);
}

internal readonly struct SideBySideSink(List<CommitFileDiffLine> lines) : IFastDiffSink
{
    public void Unchanged(int oldLine, int newLine, string text) =>
        Add(new CommitFileDiffLine { OldLineNumber = oldLine, NewLineNumber = newLine, OldText = text, NewText = text, ChangeType = "Unchanged" });

    public void Deleted(int oldLine, string text) =>
        Add(new CommitFileDiffLine { OldLineNumber = oldLine, OldText = text, ChangeType = "Deleted" });

    public void Inserted(int newLine, string text) =>
        Add(new CommitFileDiffLine { NewLineNumber = newLine, NewText = text, ChangeType = "Inserted" });

    public void Modified(int oldLine, int newLine, string oldText, string newText) =>
        Add(new CommitFileDiffLine { OldLineNumber = oldLine, NewLineNumber = newLine, OldText = oldText, NewText = newText, ChangeType = "Modified" });

    private void Add(CommitFileDiffLine line)
    {
        line.Text = null!;
        lines.Add(FastDiffLineDefaults.TrimEmptyPayloads(line));
    }
}

internal readonly struct CombinedSink(List<CommitFileDiffLine> lines) : IFastDiffSink
{
    public void Unchanged(int oldLine, int newLine, string text) =>
        Add(new CommitFileDiffLine { OldLineNumber = oldLine, NewLineNumber = newLine, Text = text, ChangeType = "Unchanged" });

    public void Deleted(int oldLine, string text) =>
        Add(new CommitFileDiffLine { OldLineNumber = oldLine, Text = text, ChangeType = "Deleted" });

    public void Inserted(int newLine, string text) =>
        Add(new CommitFileDiffLine { NewLineNumber = newLine, Text = text, ChangeType = "Inserted" });

    public void Modified(int oldLine, int newLine, string oldText, string newText)
    {
        Deleted(oldLine, oldText);
        Inserted(newLine, newText);
    }

    private void Add(CommitFileDiffLine line)
    {
        line.OldText = null!;
        line.NewText = null!;
        lines.Add(FastDiffLineDefaults.TrimEmptyPayloads(line));
    }
}
