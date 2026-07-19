using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpressThat.LovelyGit.Services.Data.Models;

[Table("known_git_repository_paths")]
public sealed record KnownGitRepositoryPath
{
    [Key]
    public string Id { get; set; } = string.Empty;

    public Guid RepositoryId { get; set; }
}
