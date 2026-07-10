namespace ExpressThat.LovelyGit.Services.Dialogs;

internal static class DialogServiceCollectionExtensions
{
    public static IServiceCollection AddLovelyGitDialogs(this IServiceCollection services)
    {
        services.AddSingleton<InfiniFrameWindowProvider>();
        services.AddSingleton<IFolderPicker, InfiniFrameFolderPicker>();
        services.AddSingleton<ISaveFilePicker, InfiniFrameSaveFilePicker>();

        return services;
    }
}
