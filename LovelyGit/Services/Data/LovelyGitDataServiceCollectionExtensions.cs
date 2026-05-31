using ExpressThat.LovelyGit.Services.Data.Repositorys;

namespace ExpressThat.LovelyGit.Services.Data;

internal static class LovelyGitDataServiceCollectionExtensions
{
    public static IServiceCollection AddLovelyGitData(this IServiceCollection services)
    {
        services.AddSingleton<AppDbContext>();
        services.AddSingleton<GitRepoCacheDbContext>();
        services.AddSingleton<KnownGitRepositoryOrderRepository>();
        services.AddSingleton<KnownGitRepositorysRepository>();
        services.AddSingleton<CommitGraphRepository>();

        return services;
    }
}
