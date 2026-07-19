using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

internal sealed class CommandResponderCatalog(
    IReadOnlyDictionary<NativeMessageType, Type> responderTypes)
{
    private static readonly IReadOnlyDictionary<string, NativeMessageType[]> NameOverrides =
        new Dictionary<string, NativeMessageType[]>(StringComparer.Ordinal)
        {
            ["ChooseCloneDestination"] =
            [
                NativeMessageType.ChooseCloneDestination,
                NativeMessageType.ChooseRepositoryDestination,
            ],
            ["GetSettings"] = [NativeMessageType.GetSetting],
            ["SetSettings"] = [NativeMessageType.SetSetting],
            ["Stash"] = [NativeMessageType.ManageStash],
            ["WorkingTreeHunk"] =
            [
                NativeMessageType.StageWorkingTreeHunk,
                NativeMessageType.UnstageWorkingTreeHunk,
            ],
        };

    private readonly IReadOnlyDictionary<NativeMessageType, Type> _responderTypes =
        responderTypes;

    public bool TryGetResponderType(NativeMessageType commandType, out Type responderType) =>
        _responderTypes.TryGetValue(commandType, out responderType!);

    internal static NativeMessageType[] GetCommandTypes(Type responderType)
    {
        const string suffix = "CommandResolver";
        var name = responderType.Name;
        if (!name.EndsWith(suffix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Command responder '{responderType.FullName}' must end with '{suffix}'.");
        }

        var commandName = name[..^suffix.Length];
        if (NameOverrides.TryGetValue(commandName, out var commandTypes))
        {
            return commandTypes;
        }

        if (Enum.TryParse<NativeMessageType>(commandName, out var commandType))
        {
            return [commandType];
        }

        throw new InvalidOperationException(
            $"Command responder '{responderType.FullName}' has no NativeMessageType mapping.");
    }
}

internal static class DeferredCommandResponderServiceCollectionExtensions
{
    public static IServiceCollection DeferCommandResponders(this IServiceCollection services)
    {
        var descriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(ICommandResponder))
            .ToArray();
        var responderTypes = new Dictionary<NativeMessageType, Type>();

        foreach (var descriptor in descriptors)
        {
            var implementationType = descriptor.ImplementationType
                ?? throw new InvalidOperationException(
                    "Command responders must use implementation-type registrations.");
            services.Remove(descriptor);
            services.TryAdd(ServiceDescriptor.Describe(
                implementationType,
                implementationType,
                descriptor.Lifetime));

            foreach (var commandType in CommandResponderCatalog.GetCommandTypes(implementationType))
            {
                if (!responderTypes.TryAdd(commandType, implementationType))
                {
                    throw new InvalidOperationException(
                        $"Multiple responders are registered for '{commandType}'.");
                }
            }
        }

        services.AddSingleton(new CommandResponderCatalog(responderTypes));
        return services;
    }
}
