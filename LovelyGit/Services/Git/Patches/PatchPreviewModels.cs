using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.Patches;

[TypeSharp]
public record PatchFilePreview
{
    public string Path { get; set; } = string.Empty;
    public int Additions { get; set; }
    public int Deletions { get; set; }
}

[TypeSharp]
public record PatchPreviewResponse
{
    public bool Selected { get; set; }
    public string? Path { get; set; }
    public string? FileName { get; set; }
    public List<PatchFilePreview> Files { get; set; } = new();
    public int TotalAdditions { get; set; }
    public int TotalDeletions { get; set; }
    public bool IsTruncated { get; set; }
}
