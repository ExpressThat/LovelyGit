using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;

namespace LovelyGit.Tests.Data;

[Collection(PerformanceTestCollection.Name)]
public sealed class KnownRepositoryPathIndexTests
{
    [Fact]
    public async Task AddedRepository_IsFoundThroughNormalizedPathKey()
    {
        using var fixture = new DataFixture();
        var storedPath = CaseVariant(fixture.RepositoryPath, upper: false);
        var lookupPath = CaseVariant(fixture.RepositoryPath, upper: true);
        var added = await fixture.Repositories.AddAsync(Repository(storedPath));

        var found = await fixture.Repositories.FindByPathAsync(lookupPath);

        Assert.Equal(added, found);
        Assert.NotNull(await fixture.Context.KnownGitRepositoryPaths.FindByIdAsync(
            PathKey(lookupPath)));
    }

    [Fact]
    public async Task FindById_PreservesExistingAndMissingContracts()
    {
        using var fixture = new DataFixture();
        var expected = await fixture.Repositories.AddAsync(Repository(fixture.RepositoryPath));

        var found = await fixture.Repositories.FindByIdAsync(expected.Id);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await fixture.Repositories.FindByIdAsync(Guid.NewGuid()));

        Assert.Equal(expected, found);
        Assert.Equal("Sequence contains no elements.", error.Message);
    }

    [Fact]
    public async Task LegacyRepositories_AreIndexedOnceAndConcurrentLookupsRemainExact()
    {
        using var fixture = new DataFixture();
        var first = Repository(fixture.RepositoryPath);
        var second = Repository(Path.Combine(fixture.RootPath, "second"));
        Directory.CreateDirectory(second.Path!);
        await InsertLegacyAsync(fixture.Context, first, second);

        var lookups = await Task.WhenAll(
            fixture.Repositories.FindByPathAsync(CaseVariant(first.Path!, upper: true)),
            fixture.Repositories.FindByPathAsync(CaseVariant(second.Path!, upper: false)));

        Assert.Equal(first.Id, lookups[0]?.Id);
        Assert.Equal(second.Id, lookups[1]?.Id);
        Assert.NotNull(await fixture.Context.KnownGitRepositoryPaths.FindByIdAsync(
            PathKey(first.Path!)));
        Assert.NotNull(await fixture.Context.KnownGitRepositoryPaths.FindByIdAsync(
            PathKey(second.Path!)));
    }

    [Fact]
    public async Task RemovedRepository_DoesNotLeaveAPathMatch()
    {
        using var fixture = new DataFixture();
        var repository = await fixture.Repositories.AddAsync(Repository(fixture.RepositoryPath));

        await fixture.Repositories.RemoveAsync(repository.Id);

        Assert.Null(await fixture.Repositories.FindByPathAsync(repository.Path!));
        Assert.Null(await fixture.Context.KnownGitRepositoryPaths.FindByIdAsync(
            PathKey(repository.Path!)));
    }

    [Fact]
    public async Task StartupPreparation_BackfillsLegacyPathsBeforeTheAppOpens()
    {
        var root = Directory.CreateTempSubdirectory("lovelygit-startup-path-index-");
        var previous = Environment.GetEnvironmentVariable(
            LovelyGitDataDirectory.OverrideEnvironmentVariable);
        var repository = Repository(Directory.CreateDirectory(
            Path.Combine(root.FullName, "repository")).FullName);
        try
        {
            Environment.SetEnvironmentVariable(
                LovelyGitDataDirectory.OverrideEnvironmentVariable,
                Path.Combine(root.FullName, "data"));
            AppDbContext.RegisterBsonKeys();
            using (var seedContext = new AppDbContext())
            {
                await InsertLegacyAsync(seedContext, repository);
            }

            await StartupDatabaseInitialization.Start();

            using var preparedContext = new AppDbContext();
            Assert.NotNull(await preparedContext.KnownGitRepositoryPaths.FindByIdAsync(
                PathKey(repository.Path!)));
            var preparedRepositories = new KnownGitRepositorysRepository(
                preparedContext,
                new KnownGitRepositoryOrderRepository(preparedContext));
            Assert.Equal(0, await preparedRepositories.EnsurePathIndexAsync());
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                LovelyGitDataDirectory.OverrideEnvironmentVariable,
                previous);
            root.Delete(recursive: true);
        }
    }

    private static async Task InsertLegacyAsync(
        AppDbContext context,
        params KnownGitRepository[] repositories)
    {
        using var transaction = context.BeginTransaction();
        using var retention = BLiteTransactionRetention.Track(transaction);
        foreach (var repository in repositories)
        {
            await context.KnownGitRepositorys.InsertAsync(repository, transaction);
        }

        await context.SaveChangesAsync(transaction);
    }

    private static KnownGitRepository Repository(string path) => new()
    {
        Id = Guid.NewGuid(),
        Name = Path.GetFileName(path),
        Path = path,
    };

    private static string CaseVariant(string path, bool upper) =>
        OperatingSystem.IsWindows()
            ? upper ? path.ToUpperInvariant() : path.ToLowerInvariant()
            : path;

    private static string PathKey(string path)
    {
        var normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        return OperatingSystem.IsWindows() ? normalized.ToUpperInvariant() : normalized;
    }

    private sealed class DataFixture : IDisposable
    {
        private readonly string? _previousDataDirectory;
        private readonly DirectoryInfo _root;

        public DataFixture()
        {
            _root = Directory.CreateTempSubdirectory("lovelygit-path-index-");
            RepositoryPath = Directory.CreateDirectory(Path.Combine(_root.FullName, "Repository")).FullName;
            _previousDataDirectory = Environment.GetEnvironmentVariable(
                LovelyGitDataDirectory.OverrideEnvironmentVariable);
            Environment.SetEnvironmentVariable(
                LovelyGitDataDirectory.OverrideEnvironmentVariable,
                Path.Combine(_root.FullName, "data"));
            AppDbContext.RegisterBsonKeys();
            Context = new AppDbContext();
            Repositories = new KnownGitRepositorysRepository(
                Context,
                new KnownGitRepositoryOrderRepository(Context));
        }

        public AppDbContext Context { get; }
        public string RepositoryPath { get; }
        public string RootPath => _root.FullName;
        public KnownGitRepositorysRepository Repositories { get; }

        public void Dispose()
        {
            Context.Dispose();
            Environment.SetEnvironmentVariable(
                LovelyGitDataDirectory.OverrideEnvironmentVariable,
                _previousDataDirectory);
            _root.Delete(recursive: true);
        }
    }
}
