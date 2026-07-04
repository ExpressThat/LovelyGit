using ExpressThat.LovelyGit.Services.NativeMessaging;
using ExpressThat.LovelyGit.Services.Settings;
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
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<NativeMessageType>());
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<Setting>());
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

        return services;
    }
}
