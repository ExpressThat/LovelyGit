using System.ComponentModel.DataAnnotations;

namespace ExpressThat.LovelyGit.Services.Data.Models
{
    public sealed record SettingModel
    {
        [Key]
        public string SettingName { get; set; } = string.Empty;

        public string? ValueJson { get; set; }
    }
}
