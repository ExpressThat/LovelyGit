using ExpressThat.LovelyGit.Services.TypeGeneration;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models
{
    [TypeSharp]
    [Union]
    [JsonConverter(typeof(JsonStringEnumConverter<CommitSignatureKind>))]
    public enum CommitSignatureKind
    {
        None,
        OpenPgp,
        Ssh,
        X509,
        Unknown,
    }

    [TypeSharp]
    [Union]
    [JsonConverter(typeof(JsonStringEnumConverter<CommitRefKind>))]
    public enum CommitRefKind
    {
        Local,
        Remote,
        Tag,
        Stash,
    }

    [TypeSharp]
    public record CommitRefInfo
    {
        public string Name { get; set; } = string.Empty;
        public CommitRefKind Kind { get; set; }
        public string? RemoteUrl { get; set; }
    }

    [TypeSharp]
    public record CommitInfo
    {
        public string Hash { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public long Date { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<CommitRefInfo> Refs { get; set; } = new();
        public CommitStats? Stats { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public CommitSignatureKind SignatureKind { get; set; }
    }
}
