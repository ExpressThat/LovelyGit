using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Submodules;

namespace LovelyGit.Tests.Git.Submodules;

public sealed class SubmoduleServicesTests
{
    [Fact]
    public async Task NativeReader_TracksInitializedAndDeinitializedState()
    {
        using var fixture = SubmoduleFixture.Create();
        var reader = new NativeSubmoduleReader();
        var service = new SubmoduleCommandService(fixture.Git);

        var initialized = Assert.Single(await reader.ReadAsync(
            fixture.ParentPath,
            CancellationToken.None));
        Assert.Equal("library", initialized.Name);
        Assert.Equal("deps/library", initialized.Path);
        Assert.Equal(SubmoduleState.Current, initialized.State);
        Assert.Equal(initialized.ExpectedCommit, initialized.CurrentCommit);

        await service.ExecuteAsync(
            fixture.ParentPath,
            "deps/library",
            SubmoduleAction.Deinitialize,
            CancellationToken.None);
        var deinitialized = Assert.Single(await reader.ReadAsync(
            fixture.ParentPath,
            CancellationToken.None));
        Assert.Equal(SubmoduleState.Uninitialized, deinitialized.State);

        await service.ExecuteAsync(
            fixture.ParentPath,
            "deps/library",
            SubmoduleAction.Initialize,
            CancellationToken.None);
        var reinitialized = Assert.Single(await reader.ReadAsync(
            fixture.ParentPath,
            CancellationToken.None));
        Assert.Equal(SubmoduleState.Current, reinitialized.State);
    }

    [Fact]
    public async Task CommandService_RejectsPathsOutsideRepository()
    {
        using var fixture = SubmoduleFixture.Create();
        var service = new SubmoduleCommandService(fixture.Git);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.ExecuteAsync(
            fixture.ParentPath,
            "../outside",
            SubmoduleAction.Update,
            CancellationToken.None));

        Assert.Contains("escapes", exception.Message);
    }

    [Fact]
    public async Task NativeReader_DoesNotOpenSubmodulePathsOutsideRepository()
    {
        var root = Directory.CreateTempSubdirectory("lovelygit-submodule-boundary-");
        try
        {
            var parentPath = Path.Combine(root.FullName, "parent");
            var outsidePath = Path.Combine(root.FullName, "outside");
            Directory.CreateDirectory(parentPath);
            Directory.CreateDirectory(outsidePath);
            var git = new GitCliService();
            InitializeRepository(git, parentPath);
            InitializeRepository(git, outsidePath);
            File.WriteAllText(
                Path.Combine(parentPath, ".gitmodules"),
                "[submodule \"outside\"]\n\tpath = ../outside\n\turl = ../outside\n");
            RunGit(git, parentPath, ["add", ".gitmodules"]);
            RunGit(git, parentPath, ["commit", "-m", "Add unsafe submodule definition"]);

            var submodule = Assert.Single(await new NativeSubmoduleReader().ReadAsync(
                parentPath,
                CancellationToken.None));

            Assert.Null(submodule.CurrentCommit);
            Assert.Equal(SubmoduleState.MissingFromHead, submodule.State);
        }
        finally
        {
            foreach (var file in root.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            root.Delete(recursive: true);
        }
    }

    private static void InitializeRepository(GitCliService git, string path)
    {
        RunGit(git, path, ["init"]);
        RunGit(git, path, ["config", "user.name", "LovelyGit Test"]);
        RunGit(git, path, ["config", "user.email", "test@example.invalid"]);
    }

    private static CliWrap.Buffered.BufferedCommandResult RunGit(
        GitCliService git,
        string path,
        IReadOnlyList<string> arguments) =>
        git.ExecuteBufferedAsync(arguments, path).GetAwaiter().GetResult();

    private sealed class SubmoduleFixture : IDisposable
    {
        private readonly DirectoryInfo _root;

        private SubmoduleFixture(DirectoryInfo root)
        {
            _root = root;
            ParentPath = Path.Combine(root.FullName, "parent");
            Git = new GitCliService();
        }

        public GitCliService Git { get; }
        public string ParentPath { get; }

        public static SubmoduleFixture Create()
        {
            var fixture = new SubmoduleFixture(
                Directory.CreateTempSubdirectory("lovelygit-submodules-"));
            var childPath = Path.Combine(fixture._root.FullName, "child");
            Directory.CreateDirectory(childPath);
            Directory.CreateDirectory(fixture.ParentPath);
            fixture.InitializeRepository(childPath);
            File.WriteAllText(Path.Combine(childPath, "library.txt"), "library\n");
            fixture.RunGit(childPath, ["add", "."]);
            fixture.RunGit(childPath, ["commit", "-m", "Library"]);
            fixture.InitializeRepository(fixture.ParentPath);
            fixture.RunGit(
                fixture.ParentPath,
                ["-c", "protocol.file.allow=always", "submodule", "add", "--name", "library", childPath, "deps/library"]);
            fixture.RunGit(fixture.ParentPath, ["commit", "-am", "Add submodule"]);
            return fixture;
        }

        public void Dispose()
        {
            foreach (var file in _root.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _root.Delete(recursive: true);
        }

        private void InitializeRepository(string path)
        {
            RunGit(path, ["init"]);
            RunGit(path, ["config", "user.name", "LovelyGit Test"]);
            RunGit(path, ["config", "user.email", "test@example.invalid"]);
            RunGit(path, ["config", "protocol.file.allow", "always"]);
        }

        private CliWrap.Buffered.BufferedCommandResult RunGit(
            string path,
            IReadOnlyList<string> arguments) =>
            Git.ExecuteBufferedAsync(arguments, path).GetAwaiter().GetResult();
    }
}
