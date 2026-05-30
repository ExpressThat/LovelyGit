using ExpressThat.LovelyGit.Services.Settings;
using Tapper;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Settings
{
    [TranspilationSource]
    public record GetSettingsCommandArguments
    {
        public Setting? Setting { get; set; } = null;
    }

    [TranspilationSource]
    public record SetSettingsCommandArguments
    {
        public Setting? Setting { get; set; } = null;

        public string? ValueJson { get; set; } = null;
    }

    [TranspilationSource]
    public record SetMultipleSettingsCommandArguments
    {
        public Dictionary<Setting, string> SettingValueJsons { get; set; } = new();
    }
}
