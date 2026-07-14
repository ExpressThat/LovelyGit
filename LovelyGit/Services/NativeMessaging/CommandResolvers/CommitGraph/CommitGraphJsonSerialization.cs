using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal static class CommitGraphJsonSerialization
{
    public static IJsonTypeInfoResolver Resolver { get; } =
        CommitGraphJsonSerializerContext.Default.WithAddedModifier(ConfigureType);

    private static void ConfigureType(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type == typeof(CommitInfo))
        {
            OmitEmptyList(typeInfo, "refs");
            return;
        }

        if (typeInfo.Type != typeof(CommitGraphRow))
        {
            return;
        }

        OmitEmptyList(typeInfo, "activeLanesAbove");
        OmitDuplicateNumbers(typeInfo, "activeLanesBelow");
        OmitEmptyList(typeInfo, "laneColorsAbove");
        OmitDuplicateColors(typeInfo, "laneColorsBelow");
        OmitEmptyList(typeInfo, "edgesAbove");
        OmitEmptyList(typeInfo, "edgesBelow");
    }

    private static void OmitEmptyList(JsonTypeInfo typeInfo, string propertyName)
    {
        FindProperty(typeInfo, propertyName).ShouldSerialize =
            static (_, value) => value is System.Collections.ICollection { Count: > 0 };
    }

    private static void OmitDuplicateNumbers(JsonTypeInfo typeInfo, string propertyName)
    {
        FindProperty(typeInfo, propertyName).ShouldSerialize = static (parent, value) =>
            parent is CommitGraphRow row &&
            value is List<int> values &&
            !values.SequenceEqual(row.ActiveLanesAbove);
    }

    private static void OmitDuplicateColors(JsonTypeInfo typeInfo, string propertyName)
    {
        FindProperty(typeInfo, propertyName).ShouldSerialize = static (parent, value) =>
            parent is CommitGraphRow row &&
            value is List<CommitLaneColor> values &&
            !SameColors(values, row.LaneColorsAbove);
    }

    private static bool SameColors(
        IReadOnlyList<CommitLaneColor> left,
        IReadOnlyList<CommitLaneColor> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (left[index].Lane != right[index].Lane ||
                left[index].ColorIndex != right[index].ColorIndex)
            {
                return false;
            }
        }

        return true;
    }

    private static JsonPropertyInfo FindProperty(JsonTypeInfo typeInfo, string name)
    {
        return typeInfo.Properties.First(property =>
            string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
