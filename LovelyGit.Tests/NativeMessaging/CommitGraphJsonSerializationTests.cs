using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

namespace LovelyGit.Tests.NativeMessaging;

public sealed class CommitGraphJsonSerializationTests
{
    [Fact]
    public void StableRow_OmitsEmptyAndDuplicateGraphCollections()
    {
        var row = Row();
        row.ActiveLanesAbove = [0, 1];
        row.ActiveLanesBelow = [0, 1];
        row.LaneColorsAbove = [new CommitLaneColor(0, 3)];
        row.LaneColorsBelow = [new CommitLaneColor(0, 3)];

        using var document = JsonDocument.Parse(Serialize(row));
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("activeLanesAbove", out _));
        Assert.True(root.TryGetProperty("laneColorsAbove", out _));
        Assert.False(root.TryGetProperty("activeLanesBelow", out _));
        Assert.False(root.TryGetProperty("laneColorsBelow", out _));
        Assert.False(root.TryGetProperty("edgesAbove", out _));
        Assert.False(root.TryGetProperty("lane", out _));
        Assert.False(root.TryGetProperty("colorIndex", out _));
        Assert.False(root.GetProperty("laneColorsAbove")[0].TryGetProperty("lane", out _));
        Assert.False(root.GetProperty("commit").TryGetProperty("refs", out _));
    }

    [Fact]
    public void TransitionRow_PreservesChangedGraphCollectionsAndRefs()
    {
        var row = Row();
        row.ActiveLanesAbove = [0];
        row.ActiveLanesBelow = [0, 1];
        row.LaneColorsAbove = [new CommitLaneColor(0, 3)];
        row.LaneColorsBelow = [new CommitLaneColor(0, 4)];
        row.Commit.Refs = [new CommitRefInfo { Name = "main" }];

        using var document = JsonDocument.Parse(Serialize(row));
        var root = document.RootElement;

        Assert.Equal(2, root.GetProperty("activeLanesBelow").GetArrayLength());
        Assert.Equal(4, root.GetProperty("laneColorsBelow")[0].GetProperty("colorIndex").GetInt32());
        Assert.Equal("main", root.GetProperty("commit").GetProperty("refs")[0].GetProperty("name").GetString());
    }

    [Fact]
    public void TerminalRow_PreservesExplicitlyEmptyBelowState()
    {
        var row = Row();
        row.ActiveLanesAbove = [0];
        row.ActiveLanesBelow = [];
        row.LaneColorsAbove = [new CommitLaneColor(0, 3)];
        row.LaneColorsBelow = [];

        using var document = JsonDocument.Parse(Serialize(row));
        var root = document.RootElement;

        Assert.Equal(0, root.GetProperty("activeLanesBelow").GetArrayLength());
        Assert.Equal(0, root.GetProperty("laneColorsBelow").GetArrayLength());
    }

    private static string Serialize(CommitGraphRow row)
    {
        return JsonSerializer.Serialize(
            row,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                TypeInfoResolver = CommitGraphJsonSerialization.Resolver,
            });
    }

    private static CommitGraphRow Row()
    {
        return new CommitGraphRow
        {
            Commit = new CommitInfo
            {
                Hash = new string('a', 40),
                Message = "Message",
            },
        };
    }
}
