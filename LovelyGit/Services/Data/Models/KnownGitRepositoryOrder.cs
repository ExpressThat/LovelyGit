using System.ComponentModel.DataAnnotations;

namespace ExpressThat.LovelyGit.Services.Data.Models
{
    public sealed record KnownGitRepositoryOrder
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        public List<Guid> RepositoryIds { get; set; } = [];
    }
}
