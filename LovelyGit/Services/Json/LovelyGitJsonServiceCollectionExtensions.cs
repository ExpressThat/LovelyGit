using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Settings;
using Microsoft.AspNetCore.SignalR;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Json;

internal static class LovelyGitJsonServiceCollectionExtensions
{
    public static IServiceCollection AddLovelyGitJsonDefaults(this IServiceCollection services)
    {
        services.Configure<HttpJsonOptions>(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<CommsHubCommandType>());
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<Setting>());
        });

        services.Configure<JsonHubProtocolOptions>(options =>
        {
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter<CommsHubCommandType>());
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter<Setting>());
        });

        return services;
    }

    public static IServiceCollection AddLovelyGitJsonTypeInfoResolver(
        this IServiceCollection services,
        IJsonTypeInfoResolver resolver)
    {
        services.Configure<HttpJsonOptions>(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Add(resolver);
        });

        services.Configure<JsonHubProtocolOptions>(options =>
        {
            options.PayloadSerializerOptions.TypeInfoResolverChain.Add(resolver);
        });

        return services;
    }
}
