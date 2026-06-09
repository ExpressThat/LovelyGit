# Git CLI Service Usage

`GitCliService` resolves the packaged Git executable and creates configured CliWrap commands.
It should be injected anywhere backend code needs to run Git as a process.

```csharp
using ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed class ExampleGitCommand
{
    private readonly GitCliService _gitCliService;

    public ExampleGitCommand(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public async Task<string> ReadStatusAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        var result = await _gitCliService.ExecuteBufferedAsync(
            ["status", "--short", "--branch"],
            workingDirectory: repositoryPath,
            cancellationToken: cancellationToken);

        return result.StandardOutput;
    }
}
```

For commands where Git may return a meaningful non-zero exit code, disable validation and inspect
the result yourself.

```csharp
var result = await _gitCliService.ExecuteBufferedAsync(
    ["diff", "--quiet"],
    workingDirectory: repositoryPath,
    validateExitCode: false,
    cancellationToken: cancellationToken);

var hasDifferences = result.ExitCode == 1;
```

For large output, create a CliWrap command directly and use streaming pipes instead of buffering.

```csharp
using CliWrap;

var command = _gitCliService.CreateCommand(
    ["log", "--format=%H"],
    workingDirectory: repositoryPath);

await command
    .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
    {
        // Process each output chunk without keeping the whole command output in memory.
    }))
    .ExecuteAsync(cancellationToken);
```

The service automatically:

- Chooses the packaged Git binary for Windows, Linux, or macOS.
- Prepends packaged Git helper directories to `PATH`.
- Sets `GIT_TERMINAL_PROMPT=0` so background commands do not block waiting for credentials.
- Falls back to PATH only for development if packaged Git is unavailable.
