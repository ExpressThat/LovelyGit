using Tapper;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

[TranspilationSource]
public record CommitDetailsResponse
{
    public string Hash { get; set; } = string.Empty;
    public List<string> Parents { get; set; } = new();
    public string Author { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public long Date { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> Branches { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public CommitStats Stats { get; set; } = new();
    public List<CommitChangedFile> ChangedFiles { get; set; } = new();
}
