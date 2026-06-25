using ExpressThat.LovelyGit.Services.Settings;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Settings
{
    [TypeSharp]
    public record GetSettingsCommandArguments
    {
        public Setting? Setting { get; set; } = null;
    }

    [TypeSharp]
    public record SetSettingsCommandArguments
    {
        public Setting? Setting { get; set; } = null;

        public string? ValueJson { get; set; } = null;
    }

    [TypeSharp]
    public record SetMultipleSettingsCommandArguments
    {
        public Dictionary<Setting, string> SettingValueJsons { get; set; } = new();
    }
}
