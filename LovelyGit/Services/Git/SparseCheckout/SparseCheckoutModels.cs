using ExpressThat.LovelyGit.Services.TypeGeneration;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.Git.SparseCheckout;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<SparseCheckoutAction>))]
public enum SparseCheckoutAction
{
    Set,
    Disable,
}

[TypeSharp]
public sealed record SparseCheckoutState
{
    public bool Enabled { get; init; }
    public bool ConeMode { get; init; }
    public List<string> Patterns { get; init; } = [];
}
