using ExpressThat.LovelyGit.Services.Settings;
using System.Diagnostics.Contracts;
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
}
