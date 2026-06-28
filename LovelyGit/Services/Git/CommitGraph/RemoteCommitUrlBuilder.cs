namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static class RemoteCommitUrlBuilder
{
    public static string? Build(string? remoteUrl, string commitHash)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl) || string.IsNullOrWhiteSpace(commitHash))
        {
            return null;
        }

        var webUrl = BuildRepository(remoteUrl);
        if (webUrl == null)
        {
            return null;
        }

        var separator = webUrl.Contains("gitlab", StringComparison.OrdinalIgnoreCase)
            ? "/-/commit/"
            : webUrl.Contains("bitbucket", StringComparison.OrdinalIgnoreCase)
                ? "/commits/"
                : "/commit/";
        return webUrl + separator + Uri.EscapeDataString(commitHash);
    }

    public static string? BuildRepository(string? remoteUrl)
    {
        return string.IsNullOrWhiteSpace(remoteUrl)
            ? null
            : NormalizeRemoteUrl(remoteUrl.Trim());
    }

    public static string? BuildTag(string? remoteUrl, string tagName)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl) || string.IsNullOrWhiteSpace(tagName))
        {
            return null;
        }

        var webUrl = BuildRepository(remoteUrl);
        if (webUrl == null)
        {
            return null;
        }

        var escapedTag = Uri.EscapeDataString(tagName);
        if (webUrl.Contains("github", StringComparison.OrdinalIgnoreCase))
        {
            return webUrl + "/releases/tag/" + escapedTag;
        }

        if (webUrl.Contains("gitlab", StringComparison.OrdinalIgnoreCase))
        {
            return webUrl + "/-/tags/" + escapedTag;
        }

        if (webUrl.Contains("bitbucket", StringComparison.OrdinalIgnoreCase))
        {
            return webUrl + "/src/" + escapedTag;
        }

        return webUrl + "/tree/" + escapedTag;
    }

    private static string? NormalizeRemoteUrl(string remoteUrl)
    {
        if (remoteUrl.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            remoteUrl = remoteUrl[..^4];
        }

        if (Uri.TryCreate(remoteUrl, UriKind.Absolute, out var uri))
        {
            return uri.Scheme is "http" or "https"
                ? remoteUrl.TrimEnd('/')
                : NormalizeSshUri(uri);
        }

        var colonIndex = remoteUrl.IndexOf(':');
        if (colonIndex <= 0 || remoteUrl[..colonIndex].Contains('/'))
        {
            return null;
        }

        var host = StripUser(remoteUrl[..colonIndex]);
        var path = remoteUrl[(colonIndex + 1)..].TrimStart('/');
        return string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(path)
            ? null
            : $"https://{host}/{path.TrimEnd('/')}";
    }

    private static string? NormalizeSshUri(Uri uri)
    {
        if (!uri.Scheme.Equals("ssh", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            return null;
        }

        var path = uri.AbsolutePath.Trim('/');
        return string.IsNullOrWhiteSpace(path) ? null : $"https://{uri.Host}/{path}";
    }

    private static string StripUser(string host)
    {
        var atIndex = host.LastIndexOf('@');
        return atIndex >= 0 ? host[(atIndex + 1)..] : host;
    }
}
