using InfiniFrame;
using Microsoft.Extensions.DependencyInjection;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

internal static class NativeMessagingRegistration
{
    public static IServiceCollection AddNativeMessaging(
        this IServiceCollection services)
    {
        services.AddSingleton<NativeMessaging>();
        services.AddSingleton<INativeMessaging>(services =>
            services.GetRequiredService<NativeMessaging>());

        return services;
    }

    public static IInfiniFrameWindowBuilder UseNativeMessaging(
        this IInfiniFrameWindowBuilder windowBuilder)
    {
        windowBuilder.RegisterWindowCreatedHandler(window =>
        {
            GetNativeMessaging(window).AttachWindow(window);
        });

        foreach (var messageType in Enum.GetValues<NativeMessageType>())
        {
            windowBuilder.RegisterWebMessagePostHandler(messageType.ToMessageId(), (window, payload) =>
            {
                var serviceProvider = window.ServiceProvider
                    ?? throw new InvalidOperationException("InfiniFrame window service provider is unavailable.");

                GetNativeMessaging(window).Handle(messageType, serviceProvider, payload);
            });
        }

        return windowBuilder;
    }

    private static NativeMessaging GetNativeMessaging(IInfiniFrameWindow window)
    {
        var serviceProvider = window.ServiceProvider
            ?? throw new InvalidOperationException("InfiniFrame window service provider is unavailable.");

        return serviceProvider.GetRequiredService<NativeMessaging>();
    }
}
