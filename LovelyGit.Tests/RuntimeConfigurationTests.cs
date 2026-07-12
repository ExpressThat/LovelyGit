using System.Text.Json;

namespace LovelyGit.Tests;

public sealed class RuntimeConfigurationTests
{
    [Fact]
    public void DesktopHost_UsesWorkstationGarbageCollection()
    {
        var runtimeConfigPath = Path.Combine(AppContext.BaseDirectory, "LovelyGit.runtimeconfig.json");
        Assert.True(File.Exists(runtimeConfigPath), $"Missing runtime config: {runtimeConfigPath}");

        using var document = JsonDocument.Parse(File.ReadAllBytes(runtimeConfigPath));
        var configProperties = document.RootElement
            .GetProperty("runtimeOptions")
            .GetProperty("configProperties");

        Assert.False(configProperties.GetProperty("System.GC.Server").GetBoolean());
    }
}
