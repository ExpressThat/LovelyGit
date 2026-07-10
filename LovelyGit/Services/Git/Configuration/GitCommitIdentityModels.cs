using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.Configuration;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<GitIdentityValueSource>))]
public enum GitIdentityValueSource
{
    Missing,
    System,
    Global,
    Repository,
    Worktree,
    Environment,
}

[TypeSharp]
public sealed record GitCommitIdentity
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public GitIdentityValueSource NameSource { get; set; }
    public GitIdentityValueSource EmailSource { get; set; }
    public bool HasRepositoryOverride { get; set; }
    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Email);
}

internal sealed class GitIdentityAccumulator
{
    public string? Name { get; private set; }
    public string? Email { get; private set; }
    public GitIdentityValueSource NameSource { get; private set; }
    public GitIdentityValueSource EmailSource { get; private set; }
    public bool HasRepositoryOverride { get; private set; }

    public void ApplyName(string value, GitIdentityValueSource source)
    {
        Name = value;
        NameSource = source;
        TrackRepositoryOverride(source);
    }

    public void ApplyEmail(string value, GitIdentityValueSource source)
    {
        Email = value;
        EmailSource = source;
        TrackRepositoryOverride(source);
    }

    public GitCommitIdentity Build() => new()
    {
        Name = Name,
        Email = Email,
        NameSource = NameSource,
        EmailSource = EmailSource,
        HasRepositoryOverride = HasRepositoryOverride,
    };

    private void TrackRepositoryOverride(GitIdentityValueSource source)
    {
        HasRepositoryOverride |= source is
            GitIdentityValueSource.Repository or GitIdentityValueSource.Worktree;
    }
}
