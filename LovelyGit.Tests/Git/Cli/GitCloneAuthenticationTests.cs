using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Cli;

[Collection(GitShellIntegrationCollection.Name)]
public sealed class GitCloneAuthenticationTests
{
    [Fact]
    public async Task CloneAsync_UsesConfiguredCredentialHelperWithoutTerminalPrompt()
    {
        using var parent = TemporaryDirectory.Create("lovelygit-clone-auth-");
        await using var server = new BasicChallengeServer();
        var credentials = Path.Combine(parent.Path, "credentials.txt");
        await File.WriteAllTextAsync(
            credentials,
            $"http://lovely:secret@127.0.0.1:{server.Port}/{Environment.NewLine}");
        var environment = new Dictionary<string, string?>
        {
            ["GIT_CONFIG_NOSYSTEM"] = "1",
            ["GIT_CONFIG_COUNT"] = "1",
            ["GIT_CONFIG_KEY_0"] = "credential.helper",
            ["GIT_CONFIG_VALUE_0"] = $"store --file={credentials.Replace('\\', '/')}",
        };
        var destination = Path.Combine(parent.Path, "copy");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new GitCloneService(new GitCliService(environment)).CloneAsync(
                Guid.NewGuid(),
                $"http://127.0.0.1:{server.Port}/repository.git",
                parent.Path,
                "copy",
                false,
                false,
                _ => { },
                timeout.Token));

        Assert.Contains("Authentication failed", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("lovely:secret")),
            server.AuthorizationHeaders);
        Assert.False(Directory.Exists(destination));
    }
}

internal sealed class BasicChallengeServer : IAsyncDisposable
{
    private readonly CancellationTokenSource _stopping = new();
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly Task _serverTask;

    public BasicChallengeServer()
    {
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _serverTask = ServeAsync();
    }

    public ConcurrentBag<string> AuthorizationHeaders { get; } = [];

    public int Port { get; }

    public async ValueTask DisposeAsync()
    {
        await _stopping.CancelAsync();
        _listener.Stop();
        try
        {
            await _serverTask.ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is OperationCanceledException or SocketException)
        {
        }
        _stopping.Dispose();
    }

    private async Task ServeAsync()
    {
        while (!_stopping.IsCancellationRequested)
        {
            using var client = await _listener.AcceptTcpClientAsync(_stopping.Token)
                .ConfigureAwait(false);
            await RespondAsync(client, _stopping.Token).ConfigureAwait(false);
        }
    }

    private async Task RespondAsync(TcpClient client, CancellationToken cancellationToken)
    {
        await using var stream = client.GetStream();
        using var reader = new StreamReader(
            stream,
            Encoding.ASCII,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line &&
               line.Length > 0)
        {
            const string authorization = "Authorization: ";
            if (line.StartsWith(authorization, StringComparison.OrdinalIgnoreCase))
            {
                AuthorizationHeaders.Add(line[authorization.Length..]);
            }
        }

        var response = Encoding.ASCII.GetBytes(
            "HTTP/1.1 401 Unauthorized\r\n" +
            "WWW-Authenticate: Basic realm=\"LovelyGit Test\"\r\n" +
            "Content-Length: 0\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(response, cancellationToken).ConfigureAwait(false);
    }
}
