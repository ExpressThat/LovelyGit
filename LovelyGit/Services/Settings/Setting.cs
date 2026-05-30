using System.Text.Json.Serialization;
using Tapper;

namespace ExpressThat.LovelyGit.Services.Settings
{
    [TranspilationSource]
    [JsonConverter(typeof(JsonStringEnumConverter<Setting>))]
    public enum Setting
    {
        CurrentGitRepositoryId
    }
}
