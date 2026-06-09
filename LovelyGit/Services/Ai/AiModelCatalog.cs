using ExpressThat.LovelyGit.Services.Settings;

namespace ExpressThat.LovelyGit.Services.Ai;

internal sealed record AiModelSpec(
    AiModel Id,
    string DisplayName,
    string FileName,
    Uri DownloadUri,
    string RepositoryId);

internal static class AiModelCatalog
{
    private static readonly IReadOnlyDictionary<AiModel, AiModelSpec> Models =
        new Dictionary<AiModel, AiModelSpec>
        {
            [AiModel.Llama32_1B] = new(
                AiModel.Llama32_1B,
                "llama3.2 (1B)",
                "Llama-3.2-1B-Instruct-Q4_K_M.gguf",
                new Uri("https://huggingface.co/bartowski/Llama-3.2-1B-Instruct-GGUF/resolve/main/Llama-3.2-1B-Instruct-Q4_K_M.gguf?download=true"),
                "bartowski/Llama-3.2-1B-Instruct-GGUF"),
            [AiModel.Llama32_3B] = new(
                AiModel.Llama32_3B,
                "llama3.2 (3B)",
                "Llama-3.2-3B-Instruct-Q4_K_M.gguf",
                new Uri("https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q4_K_M.gguf?download=true"),
                "bartowski/Llama-3.2-3B-Instruct-GGUF"),
            [AiModel.Gemma4_E2B] = new(
                AiModel.Gemma4_E2B,
                "Gemma 4 E2B IT (Q8)",
                "gemma-4-E2B-it-Q8_0.gguf",
                new Uri("https://huggingface.co/ggml-org/gemma-4-E2B-it-GGUF/resolve/main/gemma-4-E2B-it-Q8_0.gguf?download=true"),
                "ggml-org/gemma-4-E2B-it-GGUF"),
            [AiModel.Gemma4_E4B] = new(
                AiModel.Gemma4_E4B,
                "Gemma 4 E4B IT (Q4_K_M)",
                "gemma-4-E4B-it-Q4_K_M.gguf",
                new Uri("https://huggingface.co/ggml-org/gemma-4-E4B-it-GGUF/resolve/main/gemma-4-E4B-it-Q4_K_M.gguf?download=true"),
                "ggml-org/gemma-4-E4B-it-GGUF"),
        };

    public static AiModelSpec Get(AiModel model)
    {
        return Models.TryGetValue(model, out var spec) ? spec : Models[AiModel.Llama32_3B];
    }

    public static IReadOnlyCollection<AiModelSpec> GetAll()
    {
        return Models.Values.ToArray();
    }
}
