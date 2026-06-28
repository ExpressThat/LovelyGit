using ExpressThat.LovelyGit.Services.TypeGeneration;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models
{
    [TypeSharp]
    [Union]
    [JsonConverter(typeof(JsonStringEnumConverter<CommitRefKind>))]
    public enum CommitRefKind
    {
        Local,
        Remote,
        Tag,
    }

    [TypeSharp]
    public record CommitRefInfo
    {
        public string Name { get; set; } = string.Empty;
        public CommitRefKind Kind { get; set; }
    }

    [TypeSharp]
    public record CommitInfo
    {
        public string Hash { get; set; } = string.Empty;
        public List<string> Parents { get; set; } = new();
        public string Author { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public long Date { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Branches { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public List<CommitRefInfo> Refs { get; set; } = new();
        public string? RemoteUrl { get; set; }
        public string? RemoteRepositoryUrl { get; set; }
        public CommitStats? Stats { get; set; }
    }
}
