using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Data.Models
{
    [TypeSharp]
    [Table("known_git_repositorys")]
    public sealed record KnownGitRepository
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [Column("name")]
        public string? Name { get; set; }

        [Required]
        [Column("path")]
        public string? Path { get; set; }
    }

    [TypeSharp]
    public sealed record KnownGitRepositoriesResponse
    {
        public List<KnownGitRepository> Repositories { get; init; } = [];

        public string? CompactRepositoriesGzipBase64 { get; init; }
    }
}
