using ExpressThat.LovelyGit.Services.Settings;

namespace LovelyGit.Tests.Settings;

public sealed class SettingsResolverTests
{
    [Fact]
    public void GetAllDefinitions_IncludesConflictFileViewModeDefault()
    {
        var definitions = SettingsResolver.GetAllDefinitions();

        var definition = Assert.IsType<SettingDefinition<ConflictFileViewMode>>(
            definitions[Setting.ConflictFileViewMode]);
        Assert.Equal(ConflictFileViewMode.Path, definition.DefaultValue);
        Assert.Equal(typeof(ConflictFileViewMode), definition.JsonTypeInfo.Type);
    }
}
