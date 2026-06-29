using System.Text.Json.Serialization;

namespace LovelyGit.DiffBenchmarks;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CommitFileDiffResponse))]
[JsonSerializable(typeof(BenchmarkResult))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(ReportRow[]))]
internal sealed partial class BenchmarkJsonContext : JsonSerializerContext
{
}
