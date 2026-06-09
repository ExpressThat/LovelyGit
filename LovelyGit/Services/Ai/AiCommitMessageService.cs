using System.Text;
using ExpressThat.LovelyGit.Services.Ai.Models;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Settings;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;

namespace ExpressThat.LovelyGit.Services.Ai;

internal sealed class AiCommitMessageService
{
    private const int ApproximateCharactersPerToken = 4;
    private const int DefaultContextSize = 8192;
    private const int MinContextSize = 1024;
    private const int MaxContextSize = 250_000;
    private const int MinPromptBudgetPercent = 5;
    private const int MaxPromptBudgetPercent = 80;
    private readonly AiModelDownloadService _modelDownloadService;
    private readonly GitCliService _gitCliService;
    private readonly SettingsManager _settingsManager;

    public AiCommitMessageService(
        AiModelDownloadService modelDownloadService,
        GitCliService gitCliService,
        SettingsManager settingsManager)
    {
        _modelDownloadService = modelDownloadService;
        _gitCliService = gitCliService;
        _settingsManager = settingsManager;
    }

    public async Task<GenerateCommitMessageResponse> GenerateCommitMessageAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        if (!await _settingsManager.GetSetting(SettingsResolver.AiFeaturesEnabled).ConfigureAwait(false))
        {
            throw new InvalidOperationException("AI features are disabled.");
        }

        var selectedModel = await _settingsManager.GetSetting(SettingsResolver.AiModel).ConfigureAwait(false);
        var selectedDevice = await _settingsManager.GetSetting(SettingsResolver.AiComputeDevice).ConfigureAwait(false);
        var contextSize = NormalizeContextSize(await _settingsManager.GetSetting(SettingsResolver.AiContextSize).ConfigureAwait(false));
        var rawDiffContextPercent = await GetRawDiffContextPercentAsync(selectedModel).ConfigureAwait(false);
        var summaryContextPercent = NormalizePromptBudgetPercent(
            await _settingsManager.GetSetting(SettingsResolver.AiSummaryContextPercent).ConfigureAwait(false));
        var rawDiffCharacterBudget = CalculateCharacterBudget(contextSize, rawDiffContextPercent);
        var summaryCharacterBudget = CalculateCharacterBudget(contextSize, summaryContextPercent);
        var modelSpec = AiModelCatalog.Get(selectedModel);
        var modelPath = await _modelDownloadService.EnsureModelAsync(modelSpec, cancellationToken).ConfigureAwait(false);
        var diff = await GetStagedDiffAsync(repositoryPath, rawDiffCharacterBudget, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(diff))
        {
            throw new InvalidOperationException("There are no staged changes to describe.");
        }

        var summary = await GetStagedDiffSummaryAsync(repositoryPath, summaryCharacterBudget, cancellationToken).ConfigureAwait(false);
        var response = await InferCommitMessageAsync(
                modelPath,
                selectedDevice,
                contextSize,
                summary,
                diff,
                cancellationToken)
            .ConfigureAwait(false);
        response.ComputeDevice = selectedDevice;
        response.ContextSize = contextSize;
        response.GpuLayerCount = selectedDevice == AiComputeDevice.Gpu ? -1 : 0;
        return response;
    }

    private async Task<int> GetRawDiffContextPercentAsync(AiModel selectedModel)
    {
        var rawDiffContextPercent = IsGemmaModel(selectedModel)
            ? await _settingsManager.GetSetting(SettingsResolver.AiGemmaRawDiffContextPercent).ConfigureAwait(false)
            : await _settingsManager.GetSetting(SettingsResolver.AiLlamaRawDiffContextPercent).ConfigureAwait(false);
        return NormalizePromptBudgetPercent(rawDiffContextPercent);
    }

    private async Task<string> GetStagedDiffAsync(
        string repositoryPath,
        int maxCharacters,
        CancellationToken cancellationToken)
    {
        var result = await _gitCliService
            .ExecuteBufferedAsync(
                ["--no-optional-locks", "diff", "--cached", "--no-ext-diff", "--unified=3", "--no-color"],
                repositoryPath,
                validateExitCode: false,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(FirstNonEmptyLine(result.StandardError) ?? "Failed to read staged diff.");
        }

        return TruncatePromptText(result.StandardOutput, maxCharacters, "\n\n[Diff truncated]\n");
    }

    private async Task<string> GetStagedDiffSummaryAsync(
        string repositoryPath,
        int maxCharacters,
        CancellationToken cancellationToken)
    {
        var result = await _gitCliService
            .ExecuteBufferedAsync(
                ["--no-optional-locks", "diff", "--cached", "--stat", "--summary", "--no-color"],
                repositoryPath,
                validateExitCode: false,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            return string.Empty;
        }

        return TruncatePromptText(result.StandardOutput, maxCharacters, "\n[Summary truncated]\n");
    }

    private static string TruncatePromptText(string text, int maxCharacters, string suffix)
    {
        if (text.Length <= maxCharacters)
        {
            return text;
        }

        return string.Concat(text.AsSpan(0, maxCharacters), suffix);
    }

    private static int CalculateCharacterBudget(int contextSize, int contextPercent)
    {
        var promptTokens = contextSize * (long)contextPercent / 100;
        return (int)Math.Clamp(
            promptTokens * ApproximateCharactersPerToken,
            ApproximateCharactersPerToken,
            int.MaxValue);
    }

    private static int NormalizePromptBudgetPercent(int contextPercent)
    {
        return Math.Clamp(contextPercent, MinPromptBudgetPercent, MaxPromptBudgetPercent);
    }

    private static bool IsGemmaModel(AiModel model)
    {
        return model is AiModel.Gemma4_E2B or AiModel.Gemma4_E4B;
    }

    private static async Task<GenerateCommitMessageResponse> InferCommitMessageAsync(
        string modelPath,
        AiComputeDevice selectedDevice,
        int contextSize,
        string summary,
        string diff,
        CancellationToken cancellationToken)
    {
        var parameters = BuildModelParams(modelPath, selectedDevice, contextSize);

        using var model = await LLamaWeights.LoadFromFileAsync(parameters, cancellationToken).ConfigureAwait(false);
        var titleOutput = await InferTextAsync(
                model,
                parameters,
                BuildTitlePrompt(summary, diff),
                maxTokens: 48,
                cancellationToken)
            .ConfigureAwait(false);
        if (!TryParseTitle(titleOutput, out var title))
        {
            var titleRetryOutput = await InferTextAsync(
                    model,
                    parameters,
                    BuildTitleRetryPrompt(summary),
                    maxTokens: 64,
                    cancellationToken)
                .ConfigureAwait(false);
            title = ParseTitle(titleRetryOutput);
        }

        var body = await InferBodyAsync(
                model,
                parameters,
                summary,
                title,
                cancellationToken)
            .ConfigureAwait(false);

        return new GenerateCommitMessageResponse
        {
            Title = title,
            Body = body,
        };
    }

    private static async Task<string> InferTextAsync(
        LLamaWeights model,
        ModelParams parameters,
        string prompt,
        int maxTokens,
        CancellationToken cancellationToken)
    {
        using var context = model.CreateContext(parameters);
        var executor = new InteractiveExecutor(context);
        var session = new ChatSession(executor);
        var inference = new InferenceParams
        {
            MaxTokens = maxTokens,
            AntiPrompts = ["<|eot_id|>", "<|end_of_text|>", "\nUser:", "\n\nUser:"],
            OverflowStrategy = ContextOverflowStrategy.TruncateAndReprefill,
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = 0.2f,
                TopP = 0.9f,
            },
        };

        var builder = new StringBuilder(512);
        await foreach (var token in session.ChatAsync(new ChatHistory.Message(AuthorRole.User, prompt), inference, cancellationToken).ConfigureAwait(false))
        {
            builder.Append(token);
        }

        return builder.ToString();
    }

    private static async Task<string> InferBodyAsync(
        LLamaWeights model,
        ModelParams parameters,
        string summary,
        string title,
        CancellationToken cancellationToken)
    {
        var bodyOutput = await InferTextAsync(
                model,
                parameters,
                BuildBodyPrompt(summary, title),
                maxTokens: 160,
                cancellationToken)
            .ConfigureAwait(false);

        if (TryParseBody(bodyOutput, out var body))
        {
            return body;
        }

        var retryOutput = await InferTextAsync(
                model,
                parameters,
                BuildBodyRetryPrompt(summary, title),
                maxTokens: 120,
                cancellationToken)
            .ConfigureAwait(false);

        if (TryParseBody(retryOutput, out body))
        {
            return body;
        }

        throw new InvalidOperationException("AI did not generate a usable commit body.");
    }

    private static ModelParams BuildModelParams(
        string modelPath,
        AiComputeDevice selectedDevice,
        int contextSize)
    {
        var parameters = new ModelParams(modelPath)
        {
            ContextSize = (uint)contextSize,
            Threads = Math.Max(1, Environment.ProcessorCount - 1),
            BatchThreads = Math.Max(1, Environment.ProcessorCount - 1),
            BatchSize = 256,
            UBatchSize = 256,
            UseMemorymap = true,
            UseMemoryLock = false,
            FlashAttention = true,
        };

        if (selectedDevice == AiComputeDevice.Cpu)
        {
            parameters.GpuLayerCount = 0;
            parameters.SplitMode = GPUSplitMode.None;
            parameters.NoKqvOffload = true;
            parameters.OpOffload = false;
            return parameters;
        }

        parameters.GpuLayerCount = int.MaxValue;
        parameters.MainGpu = 0;
        parameters.SplitMode = GPUSplitMode.Layer;
        parameters.NoKqvOffload = false;
        parameters.OpOffload = true;
        return parameters;
    }

    private static int NormalizeContextSize(int contextSize)
    {
        if (contextSize <= 0)
        {
            return DefaultContextSize;
        }

        return Math.Clamp(contextSize, MinContextSize, MaxContextSize);
    }

    private static string BuildTitlePrompt(string summary, string diff)
    {
        return $$"""
            You are LovelyGit's commit message assistant.
            Write only the commit title for the staged diff.
            Return plain text only.
            Do not include labels, markdown, quotes, file paths, diff headers, or patch lines.
            Use imperative mood. Maximum 72 characters.
            If you cannot infer intent, summarize the dominant implementation change.

            Diff summary:
            {{summary}}

            Staged diff:
            {{diff}}
            """;
    }

    private static string BuildTitleRetryPrompt(string summary)
    {
        return $$"""
            You are LovelyGit's commit message assistant.
            Write only one Git commit title for these staged changes.
            Return plain text only.
            Do not include labels, markdown, quotes, file paths, diff headers, patch lines, or instructions.
            Use imperative mood. Maximum 72 characters.

            Change summary:
            {{summary}}
            """;
    }

    private static string BuildBodyPrompt(string summary, string title)
    {
        return $$"""
            You are LovelyGit's commit message assistant.
            Write the commit body only for this commit title:
            {{title}}

            Return 1 to 4 concise plain-text lines.
            Do not include a title, labels, markdown, quotes, file paths, diff headers, patch lines, or instructions.
            Explain what changed and why at a useful engineering level.
            Start immediately with the body text.

            Diff summary:
            {{summary}}
            """;
    }

    private static string BuildBodyRetryPrompt(string summary, string title)
    {
        return $$"""
            The previous answer did not contain a commit body.
            Generate only the body text for this commit title:
            {{title}}

            Requirements:
            - 1 to 4 concise plain-text lines
            - no labels
            - no markdown
            - no file paths
            - no instructions
            - start with the body text

            Change summary:
            {{summary}}
            """;
    }

    private static string ParseTitle(string output)
    {
        if (TryParseTitle(output, out var title))
        {
            return title;
        }

        throw new InvalidOperationException("AI did not generate a usable commit title.");
    }

    private static bool TryParseTitle(string output, out string title)
    {
        title = string.Empty;

        foreach (var rawLine in StripMarkdownFences(output).AsSpan().EnumerateLines())
        {
            var line = rawLine.Trim();
            if (line.IsEmpty || IsDiffEcho(line) || IsInstructionEcho(line) || IsControlTokenEcho(line))
            {
                continue;
            }

            if (line.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
            {
                title = line["Title:".Length..].Trim().ToString();
                break;
            }

            title = line.ToString();
            break;
        }

        title = CleanGeneratedLine(title);
        if (title.Length > 72)
        {
            title = title[..72].TrimEnd();
        }

        if (string.IsNullOrWhiteSpace(title) || title == "`" || IsDiffEcho(title) || IsControlTokenEcho(title))
        {
            title = string.Empty;
            return false;
        }

        return true;
    }

    private static bool TryParseBody(string output, out string body)
    {
        var builder = new StringBuilder(output.Length);
        foreach (var rawLine in StripMarkdownFences(output).AsSpan().EnumerateLines())
        {
            var line = rawLine.Trim();
            if (line.IsEmpty)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                continue;
            }

            if (line.StartsWith("Body:", StringComparison.OrdinalIgnoreCase))
            {
                line = line["Body:".Length..].Trim();
            }

            if (line.IsEmpty || IsDiffEcho(line) || IsInstructionEcho(line) || IsControlTokenEcho(line) || line.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            builder.AppendLine(line.ToString());
        }

        var generatedBody = CleanGeneratedBody(builder.ToString());
        if (string.IsNullOrWhiteSpace(generatedBody))
        {
            body = string.Empty;
            return false;
        }

        body = generatedBody;
        return true;
    }

    private static string StripMarkdownFences(string output)
    {
        var trimmed = output.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return output;
        }

        var firstLineEnd = trimmed.IndexOf('\n');
        if (firstLineEnd < 0)
        {
            return string.Empty;
        }

        var withoutOpeningFence = trimmed[(firstLineEnd + 1)..];
        var closingFence = withoutOpeningFence.LastIndexOf("```", StringComparison.Ordinal);
        return closingFence >= 0 ? withoutOpeningFence[..closingFence] : withoutOpeningFence;
    }

    private static string CleanGeneratedLine(string text)
    {
        return text
            .Trim()
            .Trim('"')
            .Trim('`')
            .Trim();
    }

    private static bool IsDiffEcho(ReadOnlySpan<char> line)
    {
        var trimmed = line.Trim();
        return trimmed.StartsWith("diff --git", StringComparison.Ordinal)
            || trimmed.StartsWith("index ", StringComparison.Ordinal)
            || trimmed.StartsWith("--- ", StringComparison.Ordinal)
            || trimmed.StartsWith("+++ ", StringComparison.Ordinal)
            || trimmed.StartsWith("@@", StringComparison.Ordinal)
            || trimmed.StartsWith("new file mode ", StringComparison.Ordinal)
            || trimmed.StartsWith("deleted file mode ", StringComparison.Ordinal);
    }

    private static bool IsInstructionEcho(ReadOnlySpan<char> line)
    {
        var trimmed = line.Trim();
        return trimmed.StartsWith("Please provide", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("Please write", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("Write a concise Git commit message", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("Return plain text only", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("The Body line is mandatory", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsControlTokenEcho(ReadOnlySpan<char> line)
    {
        var trimmed = line.Trim();
        return trimmed.SequenceEqual("<|eot_id|>".AsSpan())
            || trimmed.SequenceEqual("<|end_of_text|>".AsSpan())
            || trimmed.SequenceEqual("<turn|>".AsSpan())
            || trimmed.SequenceEqual("<eos>".AsSpan())
            || trimmed.SequenceEqual("</s>".AsSpan());
    }


    private static string CleanGeneratedBody(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var rawLine in text.AsSpan().EnumerateLines())
        {
            var line = CleanGeneratedLine(rawLine.ToString());
            if (IsInstructionEcho(line))
            {
                continue;
            }

            if (line.Length == 0)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                continue;
            }

            builder.AppendLine(line);
        }

        return builder.ToString().Trim();
    }

    private static string? FirstNonEmptyLine(string text)
    {
        foreach (var line in text.AsSpan().EnumerateLines())
        {
            var trimmed = line.Trim();
            if (!trimmed.IsEmpty)
            {
                return trimmed.ToString();
            }
        }

        return null;
    }
}
