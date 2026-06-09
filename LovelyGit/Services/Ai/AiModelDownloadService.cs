using System.Buffers;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ExpressThat.LovelyGit.Services.Ai.Models;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ExpressThat.LovelyGit.Services.Ai;

internal sealed class AiModelDownloadService
{
    private const int BufferSize = 1024 * 128;
    private const int MaxLicenseCharacters = 80_000;
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(200);
    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private readonly HttpClient _httpClient;
    private readonly IHubContext<CommsHub> _hubContext;
    private readonly SemaphoreSlim _downloadGate = new(1, 1);

    public AiModelDownloadService(HttpClient httpClient, IHubContext<CommsHub> hubContext)
    {
        _httpClient = httpClient;
        _hubContext = hubContext;
    }

    public async Task<string> EnsureModelAsync(AiModelSpec model, CancellationToken cancellationToken)
    {
        var modelDirectory = GetModelDirectory();
        Directory.CreateDirectory(modelDirectory);
        await EnsureLicenseAsync(model, cancellationToken).ConfigureAwait(false);

        var modelPath = Path.Combine(modelDirectory, model.FileName);
        var existingLength = GetExistingModelLength(modelPath);
        if (existingLength > 0)
        {
            return modelPath;
        }

        await _downloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (GetExistingModelLength(modelPath) > 0)
            {
                return modelPath;
            }

            await DownloadModelAsync(model, modelPath, cancellationToken).ConfigureAwait(false);
            return modelPath;
        }
        finally
        {
            _downloadGate.Release();
        }
    }

    public async Task<GetAiModelLicensesResponse> GetLicensesAsync(CancellationToken cancellationToken)
    {
        var licenses = new List<AiModelLicenseInfo>();
        foreach (var model in AiModelCatalog.GetAll())
        {
            licenses.Add(await EnsureLicenseAsync(model, cancellationToken).ConfigureAwait(false));
        }

        return new GetAiModelLicensesResponse
        {
            Licenses = licenses,
        };
    }

    private async Task<AiModelLicenseInfo> EnsureLicenseAsync(AiModelSpec model, CancellationToken cancellationToken)
    {
        var modelDirectory = GetModelDirectory();
        Directory.CreateDirectory(modelDirectory);

        var licensePath = GetLicensePath(modelDirectory, model);
        if (TryReadCachedLicense(model, licensePath, out var cachedLicense))
        {
            return cachedLicense;
        }

        var license = await FetchLicenseAsync(model, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(licensePath, BuildLicenseCacheContent(license), Encoding.UTF8, cancellationToken)
            .ConfigureAwait(false);

        license.IsCached = true;
        return license;
    }

    private async Task<AiModelLicenseInfo> FetchLicenseAsync(AiModelSpec model, CancellationToken cancellationToken)
    {
        var cardText = await GetStringAsync(BuildHuggingFaceRawUri(model.RepositoryId, "README.md"), cancellationToken)
            .ConfigureAwait(false);
        var frontMatter = ParseFrontMatter(cardText);
        if (!frontMatter.TryGetValue("license", out var licenseName))
        {
            licenseName = string.Empty;
        }

        frontMatter.TryGetValue("license_link", out var licenseUrl);
        frontMatter.TryGetValue("extra_gated_prompt", out var licenseText);

        if (string.IsNullOrWhiteSpace(licenseText)
            && frontMatter.TryGetValue("base_model", out var baseModel)
            && !string.IsNullOrWhiteSpace(baseModel))
        {
            var baseLicense = await TryFetchBaseModelLicenseAsync(baseModel, cancellationToken).ConfigureAwait(false);
            if (baseLicense != null)
            {
                licenseName = string.IsNullOrWhiteSpace(baseLicense.LicenseName) ? licenseName : baseLicense.LicenseName;
                licenseUrl = string.IsNullOrWhiteSpace(baseLicense.LicenseUrl) ? licenseUrl : baseLicense.LicenseUrl;
                licenseText = baseLicense.LicenseText;
            }
        }

        if (string.IsNullOrWhiteSpace(licenseText) && !string.IsNullOrWhiteSpace(licenseUrl))
        {
            licenseText = await TryFetchLicenseLinkAsync(licenseUrl, cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(licenseText))
        {
            licenseText = ExtractReadableCardText(cardText);
        }

        return new AiModelLicenseInfo
        {
            Model = model.Id,
            DisplayName = model.DisplayName,
            RepositoryId = model.RepositoryId,
            LicenseName = licenseName,
            LicenseUrl = licenseUrl ?? string.Empty,
            LicenseText = TruncateLicenseText(licenseText),
            IsCached = false,
        };
    }

    private async Task<AiModelLicenseInfo?> TryFetchBaseModelLicenseAsync(
        string baseModel,
        CancellationToken cancellationToken)
    {
        try
        {
            var cardText = await GetStringAsync(BuildHuggingFaceRawUri(baseModel, "README.md"), cancellationToken)
                .ConfigureAwait(false);
            var frontMatter = ParseFrontMatter(cardText);
            frontMatter.TryGetValue("license", out var licenseName);
            frontMatter.TryGetValue("license_link", out var licenseUrl);
            frontMatter.TryGetValue("extra_gated_prompt", out var licenseText);
            if (string.IsNullOrWhiteSpace(licenseText) && !string.IsNullOrWhiteSpace(licenseUrl))
            {
                licenseText = await TryFetchLicenseLinkAsync(licenseUrl, cancellationToken).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(licenseText))
            {
                licenseText = ExtractReadableCardText(cardText);
            }

            return new AiModelLicenseInfo
            {
                LicenseName = licenseName ?? string.Empty,
                LicenseUrl = licenseUrl ?? string.Empty,
                LicenseText = TruncateLicenseText(licenseText),
            };
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
    }

    private async Task<string> TryFetchLicenseLinkAsync(string licenseUrl, CancellationToken cancellationToken)
    {
        try
        {
            var text = await GetStringAsync(new Uri(licenseUrl), cancellationToken).ConfigureAwait(false);
            return ExtractReadableCardText(text);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or UriFormatException)
        {
            return string.Empty;
        }
    }

    private async Task DownloadModelAsync(AiModelSpec model, string modelPath, CancellationToken cancellationToken)
    {
        var temporaryPath = $"{modelPath}.download";
        long bytesReceived = 0;
        long? totalBytesForComplete = null;
        try
        {
            await using (var output = File.Open(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using var response = await _httpClient
                    .GetAsync(model.DownloadUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength;
                totalBytesForComplete = totalBytes;
                await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                var lastProgress = DateTimeOffset.MinValue;
                try
                {
                    while (true)
                    {
                        var bytesRead = await input.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false);
                        if (bytesRead == 0)
                        {
                            break;
                        }

                        await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                        bytesReceived += bytesRead;

                        var now = DateTimeOffset.UtcNow;
                        if (now - lastProgress >= ProgressInterval)
                        {
                            lastProgress = now;
                            await SendProgressAsync(model, bytesReceived, totalBytes, false, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, modelPath, true);
            await SendProgressAsync(model, bytesReceived, totalBytesForComplete, true, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            TryDeleteTemporaryFile(temporaryPath);
            throw;
        }
    }

    private async Task SendProgressAsync(
        AiModelSpec model,
        long bytesReceived,
        long? totalBytes,
        bool isComplete,
        CancellationToken cancellationToken)
    {
        var percent = totalBytes is > 0 ? bytesReceived * 100d / totalBytes.Value : (double?)null;
        await _hubContext.Clients.All
            .SendAsync(
                "AiModelDownloadProgress",
                new AiModelDownloadProgressNotification
                {
                    Model = model.Id.ToString(),
                    BytesReceived = bytesReceived,
                    TotalBytes = totalBytes,
                    Percent = percent,
                    IsComplete = isComplete,
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static string GetModelDirectory()
    {
        var databasePath = AppDbContext.GetBasePath();
        var dataDirectory = Path.GetDirectoryName(databasePath)
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LovelyGit");
        return Path.Combine(dataDirectory, "Models");
    }

    private static Uri BuildHuggingFaceRawUri(string repositoryId, string path)
    {
        return new Uri($"https://huggingface.co/{repositoryId}/raw/main/{path}");
    }

    private static string GetLicensePath(string modelDirectory, AiModelSpec model)
    {
        return Path.Combine(modelDirectory, $"{model.FileName}.license.md");
    }

    private static bool TryReadCachedLicense(
        AiModelSpec model,
        string licensePath,
        out AiModelLicenseInfo license)
    {
        license = new AiModelLicenseInfo();
        if (!File.Exists(licensePath))
        {
            return false;
        }

        try
        {
            var text = File.ReadAllText(licensePath, Encoding.UTF8);
            var frontMatter = ParseFrontMatter(text);
            license = new AiModelLicenseInfo
            {
                Model = model.Id,
                DisplayName = model.DisplayName,
                RepositoryId = model.RepositoryId,
                LicenseName = frontMatter.TryGetValue("license", out var licenseName) ? licenseName : string.Empty,
                LicenseUrl = frontMatter.TryGetValue("license_link", out var licenseUrl) ? licenseUrl : string.Empty,
                LicenseText = ExtractReadableCardText(text),
                IsCached = true,
            };
            return !string.IsNullOrWhiteSpace(license.LicenseText);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string BuildLicenseCacheContent(AiModelLicenseInfo license)
    {
        return $$"""
            ---
            model: {{license.Model}}
            repository: {{license.RepositoryId}}
            license: {{license.LicenseName}}
            license_link: {{license.LicenseUrl}}
            ---

            {{license.LicenseText}}
            """;
    }

    private static Dictionary<string, string> ParseFrontMatter(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!text.StartsWith("---", StringComparison.Ordinal))
        {
            return values;
        }

        var end = text.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0)
        {
            return values;
        }

        var frontMatter = text.AsSpan(3, end - 3);
        string? currentKey = null;
        var currentValue = new StringBuilder();
        foreach (var rawLine in frontMatter.EnumerateLines())
        {
            var line = rawLine.TrimEnd();
            if (line.IsEmpty)
            {
                continue;
            }

            if (!char.IsWhiteSpace(line[0]) && line.IndexOf(':') is var separator && separator > 0)
            {
                FlushFrontMatterValue(values, currentKey, currentValue);
                currentKey = line[..separator].Trim().ToString();
                currentValue.Clear();
                currentValue.Append(UnquoteYamlValue(line[(separator + 1)..].Trim()));
                continue;
            }

            if (currentKey != null)
            {
                if (currentValue.Length > 0)
                {
                    currentValue.AppendLine();
                }

                currentValue.Append(UnquoteYamlValue(line.Trim()));
            }
        }

        FlushFrontMatterValue(values, currentKey, currentValue);
        return values;
    }

    private static void FlushFrontMatterValue(
        Dictionary<string, string> values,
        string? currentKey,
        StringBuilder currentValue)
    {
        if (currentKey == null)
        {
            return;
        }

        var value = currentValue.ToString().Trim();
        if (value.StartsWith("- ", StringComparison.Ordinal))
        {
            value = value[2..].Trim();
        }

        values[currentKey] = value.Replace("\\n", "\n", StringComparison.Ordinal).Replace("\\\"", "\"", StringComparison.Ordinal);
    }

    private static string UnquoteYamlValue(ReadOnlySpan<char> value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length >= 2
            && ((trimmed[0] == '"' && trimmed[^1] == '"') || (trimmed[0] == '\'' && trimmed[^1] == '\'')))
        {
            trimmed = trimmed[1..^1];
        }

        return trimmed.ToString();
    }

    private static string ExtractReadableCardText(string text)
    {
        var withoutFrontMatter = StripFrontMatter(text);
        if (withoutFrontMatter.Contains('<', StringComparison.Ordinal) && withoutFrontMatter.Contains('>', StringComparison.Ordinal))
        {
            withoutFrontMatter = HtmlTagRegex.Replace(withoutFrontMatter, string.Empty);
            withoutFrontMatter = WebUtility.HtmlDecode(withoutFrontMatter);
        }

        return TruncateLicenseText(withoutFrontMatter.Trim());
    }

    private static string StripFrontMatter(string text)
    {
        if (!text.StartsWith("---", StringComparison.Ordinal))
        {
            return text;
        }

        var end = text.IndexOf("\n---", 3, StringComparison.Ordinal);
        return end >= 0 ? text[(end + 4)..] : text;
    }

    private static string TruncateLicenseText(string text)
    {
        return text.Length <= MaxLicenseCharacters
            ? text
            : string.Concat(text.AsSpan(0, MaxLicenseCharacters), "\n\n[License text truncated]\n");
    }

    private async Task<string> GetStringAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    private static long GetExistingModelLength(string modelPath)
    {
        if (!File.Exists(modelPath))
        {
            return 0;
        }

        try
        {
            return new FileInfo(modelPath).Length;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return 0;
        }
    }

    private static void TryDeleteTemporaryFile(string temporaryPath)
    {
        try
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
