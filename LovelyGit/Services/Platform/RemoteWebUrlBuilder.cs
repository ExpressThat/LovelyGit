using ExpressThat.LovelyGit.Services.TypeGeneration;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.Platform;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<RemoteWebResourceKind>))]
public enum RemoteWebResourceKind
{
    Repository,
    Commit,
    Branch,
    PullRequest,
}

internal static class RemoteWebUrlBuilder
{
    public static string Build(
        string remoteUrl,
        RemoteWebResourceKind kind,
        string? value,
        string? targetValue = null)
    {
        var repositoryUrl = NormalizeRepositoryUrl(remoteUrl);
        ValidateValue(kind, value, targetValue);

        return kind switch
        {
            RemoteWebResourceKind.Repository => repositoryUrl,
            RemoteWebResourceKind.Commit => BuildCommitUrl(repositoryUrl, value!),
            RemoteWebResourceKind.Branch => BuildBranchUrl(repositoryUrl, value!),
            RemoteWebResourceKind.PullRequest => BuildPullRequestUrl(repositoryUrl, value!, targetValue!),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    private static string NormalizeRepositoryUrl(string remoteUrl)
    {
        var trimmed = remoteUrl.Trim();
        if (trimmed.Length == 0)
        {
            throw new InvalidOperationException("The repository does not have a remote URL.");
        }

        string webUrl;
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            uri.Scheme is "http" or "https" or "ssh")
        {
            webUrl = uri.Scheme == "ssh"
                ? $"https://{uri.Host}{uri.AbsolutePath}"
                : $"https://{uri.Host}{uri.AbsolutePath}";
        }
        else if (TryParseScpRemote(trimmed, out var host, out var path))
        {
            webUrl = $"https://{host}/{path}";
        }
        else
        {
            throw new InvalidOperationException("This remote URL cannot be opened in a web browser.");
        }

        webUrl = webUrl.TrimEnd('/');
        webUrl = webUrl.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? webUrl[..^4]
            : webUrl;
        return NormalizeAzureSshUrl(webUrl);
    }

    private static bool TryParseScpRemote(string value, out string host, out string path)
    {
        host = string.Empty;
        path = string.Empty;
        var at = value.IndexOf('@');
        var colon = value.IndexOf(':', at + 1);
        if (at <= 0 || colon <= at + 1 || colon == value.Length - 1)
        {
            return false;
        }

        host = value[(at + 1)..colon];
        path = value[(colon + 1)..].TrimStart('/');
        return host.Length > 0 && path.Length > 0;
    }

    private static string BuildCommitUrl(string repositoryUrl, string hash)
    {
        var segment = Uri.EscapeDataString(hash);
        if (IsAzureDevOps(repositoryUrl))
        {
            return $"{repositoryUrl}/commit/{segment}";
        }

        return IsGitLab(repositoryUrl)
            ? $"{repositoryUrl}/-/commit/{segment}"
            : $"{repositoryUrl}/commit{(IsBitbucket(repositoryUrl) ? "s" : string.Empty)}/{segment}";
    }

    private static string BuildBranchUrl(string repositoryUrl, string branch)
    {
        var segment = string.Join('/', branch.Split('/').Select(Uri.EscapeDataString));
        if (IsAzureDevOps(repositoryUrl))
        {
            return $"{repositoryUrl}?version=GB{Uri.EscapeDataString(branch)}";
        }

        if (IsGitLab(repositoryUrl))
        {
            return $"{repositoryUrl}/-/tree/{segment}";
        }

        return IsBitbucket(repositoryUrl)
            ? $"{repositoryUrl}/branch/{segment}"
            : $"{repositoryUrl}/tree/{segment}";
    }

    private static string BuildPullRequestUrl(string repositoryUrl, string source, string target)
    {
        var sourceQuery = Uri.EscapeDataString(source);
        var targetQuery = Uri.EscapeDataString(target);
        if (IsAzureDevOps(repositoryUrl))
        {
            return $"{repositoryUrl}/pullrequestcreate?sourceRef={Uri.EscapeDataString($"refs/heads/{source}")}" +
                $"&targetRef={Uri.EscapeDataString($"refs/heads/{target}")}";
        }

        if (IsGitLab(repositoryUrl))
        {
            return $"{repositoryUrl}/-/merge_requests/new?merge_request%5Bsource_branch%5D={sourceQuery}" +
                $"&merge_request%5Btarget_branch%5D={targetQuery}";
        }

        if (IsBitbucket(repositoryUrl))
        {
            return $"{repositoryUrl}/pull-requests/new?source={sourceQuery}&dest={targetQuery}";
        }

        return $"{repositoryUrl}/compare/{EscapeBranchPath(target)}...{EscapeBranchPath(source)}?expand=1";
    }

    private static string EscapeBranchPath(string branch) =>
        string.Join('/', branch.Split('/').Select(Uri.EscapeDataString));

    private static void ValidateValue(
        RemoteWebResourceKind kind,
        string? value,
        string? targetValue)
    {
        if (kind != RemoteWebResourceKind.Repository && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"A {kind.ToString().ToLowerInvariant()} value is required.", nameof(value));
        }

        if (kind == RemoteWebResourceKind.PullRequest && string.IsNullOrWhiteSpace(targetValue))
        {
            throw new ArgumentException("A pull request target branch is required.", nameof(targetValue));
        }
    }

    private static bool IsGitLab(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        uri.Host.Contains("gitlab", StringComparison.OrdinalIgnoreCase);

    private static bool IsBitbucket(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        uri.Host.Contains("bitbucket", StringComparison.OrdinalIgnoreCase);

    private static bool IsAzureDevOps(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase) ||
         uri.Host.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase));

    private static string NormalizeAzureSshUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !uri.Host.Equals("ssh.dev.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        var segments = uri.AbsolutePath.Trim('/').Split('/');
        return segments.Length == 4 && segments[0].Equals("v3", StringComparison.Ordinal)
            ? $"https://dev.azure.com/{segments[1]}/{segments[2]}/_git/{segments[3]}"
            : url;
    }
}
