using System.Text.Json;
using ExpressThat.LovelyGit.Services;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace LovelyGit.Tests.NativeMessaging;

public sealed class CommandResponderCatalogTests
{
    [Fact]
    public void AddLovelyGitServices_MapsEveryRequestCommandWithoutEagerResponderRegistrations()
    {
        var services = new ServiceCollection();

        services.AddLovelyGitServices();

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(ICommandResponder));
        var catalog = Assert.IsType<CommandResponderCatalog>(Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(CommandResponderCatalog))
            .ImplementationInstance);
        foreach (var commandType in Enum.GetValues<NativeMessageType>().Except(NotificationTypes))
        {
            Assert.True(
                catalog.TryGetResponderType(commandType, out _),
                $"{commandType} has no deferred responder mapping.");
        }
    }

    [Fact]
    public async Task ResolveCommand_ConstructsOnlyTheRequestedResponderAndReusesIt()
    {
        GetAllSettingsCommandResolver.Reset();
        KnownGitRepositorysCommandResolver.Reset();
        var services = new ServiceCollection();
        services.AddSingleton<ICommandResponder, GetAllSettingsCommandResolver>();
        services.AddSingleton<ICommandResponder, KnownGitRepositorysCommandResolver>();
        services.DeferCommandResponders();
        services.AddSingleton<CommandResolver>();
        await using var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<CommandResolver>();

        var first = await resolver.ResolveCommand(Command(NativeMessageType.GetAllSettings));
        var second = await resolver.ResolveCommand(Command(NativeMessageType.GetAllSettings));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, GetAllSettingsCommandResolver.CreatedCount);
        Assert.Equal(0, KnownGitRepositorysCommandResolver.CreatedCount);
    }

    [Fact]
    public async Task ResolveCommand_WhenCommandHasNoResponder_ReturnsFailureWithoutConstruction()
    {
        GetAllSettingsCommandResolver.Reset();
        var services = new ServiceCollection();
        services.AddSingleton<ICommandResponder, GetAllSettingsCommandResolver>();
        services.DeferCommandResponders();
        services.AddSingleton<CommandResolver>();
        await using var provider = services.BuildServiceProvider();

        var response = await provider.GetRequiredService<CommandResolver>()
            .ResolveCommand(Command(NativeMessageType.WorkingTreeChanged));

        Assert.False(response.IsSuccess);
        Assert.Contains("No responder", response.ErrorMessage!, StringComparison.Ordinal);
        Assert.Equal(0, GetAllSettingsCommandResolver.CreatedCount);
    }

    private static readonly NativeMessageType[] NotificationTypes =
    [
        NativeMessageType.WorkingTreeChanged,
        NativeMessageType.CommitGraphChanged,
        NativeMessageType.CloneRepositoryProgress,
    ];

    private static NativeCommand<JsonElement> Command(NativeMessageType commandType) => new()
    {
        CommandType = commandType,
        CommandUniqueId = Guid.NewGuid().ToString("N"),
    };

    private sealed class GetAllSettingsCommandResolver : TestResponder
    {
        public static int CreatedCount { get; private set; }

        public GetAllSettingsCommandResolver() => CreatedCount++;

        protected override NativeMessageType CommandType => NativeMessageType.GetAllSettings;

        public static void Reset() => CreatedCount = 0;
    }

    private sealed class KnownGitRepositorysCommandResolver : TestResponder
    {
        public static int CreatedCount { get; private set; }

        public KnownGitRepositorysCommandResolver() => CreatedCount++;

        protected override NativeMessageType CommandType => NativeMessageType.KnownGitRepositorys;

        public static void Reset() => CreatedCount = 0;
    }

    private abstract class TestResponder : ICommandResponder
    {
        protected abstract NativeMessageType CommandType { get; }

        public bool CanRespondTo(NativeCommand<JsonElement> command) =>
            command.CommandType == CommandType;

        public Task<CommandResponseBase> Resolve(NativeCommand<JsonElement> command) =>
            Task.FromResult<CommandResponseBase>(new()
            {
                CommandType = command.CommandType,
                CommandUniqueId = command.CommandUniqueId,
                IsSuccess = true,
            });
    }
}
