using System.Diagnostics;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Settings;

namespace LovelyGit.Tests.Settings;

[Collection(PerformanceTestCollection.Name)]
public sealed class SettingsManagerTests
{
    [Fact]
    public async Task SetSettingValues_PersistsEveryValue()
    {
        using var database = new SettingsDatabase();
        var manager = new SettingsManager(database.Context);

        await manager.SetSettingValues(ThemeValues());

        Assert.Equal("Morning", await manager.GetSetting(SettingsResolver.LightTheme));
        Assert.Equal("#112233", await manager.GetSetting(SettingsResolver.LightAccent));
        Assert.Equal("#F8F8F8", await manager.GetSetting(SettingsResolver.LightBackground));
        Assert.Equal("#202020", await manager.GetSetting(SettingsResolver.LightForeground));
    }

    [Fact]
    public async Task SetSettingValues_InvalidValueLeavesExistingValuesUnchanged()
    {
        using var database = new SettingsDatabase();
        var manager = new SettingsManager(database.Context);
        await manager.SetSettingValue(SettingsResolver.LightAccent, "\"#445566\"");
        var values = new Dictionary<ISettingDefinition, string>
        {
            [SettingsResolver.LightAccent] = "\"#112233\"",
            [SettingsResolver.SignCommitsByDefault] = "not-json",
        };

        await Assert.ThrowsAsync<JsonException>(() => manager.SetSettingValues(values));

        Assert.Equal("#445566", await manager.GetSetting(SettingsResolver.LightAccent));
        Assert.False(await manager.GetSetting(SettingsResolver.SignCommitsByDefault));
    }

    [Fact]
    public async Task SetSettingValues_EmptyPatchDoesNotCreateSettings()
    {
        using var database = new SettingsDatabase();
        var manager = new SettingsManager(database.Context);

        await manager.SetSettingValues(new Dictionary<ISettingDefinition, string>());

        Assert.Equal("Morning", await manager.GetSetting(SettingsResolver.LightTheme));
    }

    [Fact]
    public async Task SetSettingValues_KeepsBulkPersistenceBounded()
    {
        using var database = new SettingsDatabase();
        var manager = new SettingsManager(database.Context);
        var values = ThemeValues();
        for (var index = 0; index < 10; index++)
        {
            await manager.SetSettingValues(values);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var startedAt = Stopwatch.GetTimestamp();
        for (var index = 0; index < 100; index++)
        {
            await manager.SetSettingValues(values);
        }

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        Assert.True(elapsed < TimeSpan.FromMilliseconds(100));
        Assert.InRange(allocated, 0, 4_000_000);
    }

    private static Dictionary<ISettingDefinition, string> ThemeValues() => new()
    {
        [SettingsResolver.LightTheme] = "\"Morning\"",
        [SettingsResolver.LightAccent] = "\"#112233\"",
        [SettingsResolver.LightBackground] = "\"#F8F8F8\"",
        [SettingsResolver.LightForeground] = "\"#202020\"",
    };

    private sealed class SettingsDatabase : IDisposable
    {
        private readonly DirectoryInfo _directory =
            Directory.CreateTempSubdirectory("lovelygit-settings-");
        private readonly string? _previousDirectory;

        public SettingsDatabase()
        {
            _previousDirectory = Environment.GetEnvironmentVariable(
                LovelyGitDataDirectory.OverrideEnvironmentVariable);
            Environment.SetEnvironmentVariable(
                LovelyGitDataDirectory.OverrideEnvironmentVariable,
                _directory.FullName);
            AppDbContext.RegisterBsonKeys();
            Context = new AppDbContext();
        }

        public AppDbContext Context { get; }

        public void Dispose()
        {
            Context.Dispose();
            Environment.SetEnvironmentVariable(
                LovelyGitDataDirectory.OverrideEnvironmentVariable,
                _previousDirectory);
            _directory.Delete(recursive: true);
        }
    }
}
