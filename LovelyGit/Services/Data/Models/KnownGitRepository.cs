using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpressThat.LovelyGit.Services.Data.Models
{
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
}
