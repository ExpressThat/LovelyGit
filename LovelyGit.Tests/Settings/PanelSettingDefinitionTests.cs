using ExpressThat.LovelyGit.Services.Settings;

namespace LovelyGit.Tests.Settings;

public sealed class PanelSettingDefinitionTests
{
    [Fact]
    public void PanelWidthsHaveSafeDefaultsAndAreRegistered()
    {
        var definitions = SettingsResolver.GetAllDefinitions();

        Assert.Equal(256, SettingsResolver.CommitGraphRefsPanelWidth.DefaultValue);
        Assert.Equal(440, SettingsResolver.DetailsPanelWidth.DefaultValue);
        Assert.Same(
            SettingsResolver.CommitGraphRefsPanelWidth,
            definitions[Setting.CommitGraphRefsPanelWidth]);
        Assert.Same(
            SettingsResolver.DetailsPanelWidth,
            definitions[Setting.DetailsPanelWidth]);
    }
}
