using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Checkout;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CherryPick;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Merge;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Reset;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Revert;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Tags;

namespace LovelyGit.Tests.NativeMessaging;

public sealed class CommandArgumentJsonContextTests
{
    private static readonly Guid RepositoryId =
        Guid.Parse("bd3d4c1a-5061-453c-abef-70a2aafa6050");

    [Fact]
    public void TagsContext_DeserializesCamelCaseArguments()
    {
        var arguments = JsonSerializer.Deserialize(
            CreateJson("tagName", "codex-test"),
            TagsJsonSerializerContext.Default.CreateTagAtCommitCommandArguments);

        Assert.Equal(RepositoryId, arguments?.RepositoryId);
        Assert.Equal("abc123", arguments?.CommitHash);
        Assert.Equal("codex-test", arguments?.TagName);
    }

    [Theory]
    [MemberData(nameof(CamelCasePayloads))]
    public void MutatingCommandContexts_DeserializeCamelCaseArguments(
        string json,
        JsonTypeInfo jsonTypeInfo,
        Action<object?> assertArguments)
    {
        var arguments = JsonSerializer.Deserialize(json, jsonTypeInfo);

        assertArguments(arguments);
    }

    public static TheoryData<string, JsonTypeInfo, Action<object?>> CamelCasePayloads()
    {
        return new TheoryData<string, JsonTypeInfo, Action<object?>>
        {
            {
                CreateJson("branchName", "feature/test"),
                MergeJsonSerializerContext.Default.MergeBranchIntoCurrentCommandArguments,
                value => AssertBranch(value as MergeBranchIntoCurrentCommandArguments)
            },
            {
                CreateJson("branchName", "main"),
                RebaseJsonSerializerContext.Default.RebaseCurrentBranchOntoBranchCommandArguments,
                value => AssertBranch(value as RebaseCurrentBranchOntoBranchCommandArguments)
            },
            {
                CreateJson("commitHash", "abc123"),
                CherryPickJsonSerializerContext.Default.CherryPickCommitCommandArguments,
                value => AssertCommit(value as CherryPickCommitCommandArguments)
            },
            {
                CreateJson("commitHash", "abc123"),
                RevertJsonSerializerContext.Default.RevertCommitCommandArguments,
                value => AssertCommit(value as RevertCommitCommandArguments)
            },
            {
                CreateJson("branchName", "main"),
                CheckoutJsonSerializerContext.Default.CheckoutBranchCommandArguments,
                value =>
                {
                    var arguments = Assert.IsType<CheckoutBranchCommandArguments>(value);
                    Assert.Equal(RepositoryId, arguments.RepositoryId);
                    Assert.Equal("main", arguments.BranchName);
                }
            },
            {
                CreateJson("resetMode", "Hard"),
                ResetJsonSerializerContext.Default.ResetCurrentBranchToCommitCommandArguments,
                value =>
                {
                    var arguments = Assert.IsType<ResetCurrentBranchToCommitCommandArguments>(value);
                    Assert.Equal(RepositoryId, arguments.RepositoryId);
                    Assert.Equal("abc123", arguments.CommitHash);
                    Assert.Equal(GitResetMode.Hard, arguments.ResetMode);
                }
            },
        };
    }

    private static string CreateJson(string extraProperty, string extraValue)
    {
        return $$"""
            {
              "repositoryId": "{{RepositoryId}}",
              "commitHash": "abc123",
              "{{extraProperty}}": "{{extraValue}}"
            }
            """;
    }

    private static void AssertBranch(MergeBranchIntoCurrentCommandArguments? arguments)
    {
        Assert.NotNull(arguments);
        Assert.Equal(RepositoryId, arguments.RepositoryId);
        Assert.Equal("feature/test", arguments.BranchName);
    }

    private static void AssertBranch(RebaseCurrentBranchOntoBranchCommandArguments? arguments)
    {
        Assert.NotNull(arguments);
        Assert.Equal(RepositoryId, arguments.RepositoryId);
        Assert.Equal("main", arguments.BranchName);
    }

    private static void AssertCommit(CherryPickCommitCommandArguments? arguments)
    {
        Assert.NotNull(arguments);
        Assert.Equal(RepositoryId, arguments.RepositoryId);
        Assert.Equal("abc123", arguments.CommitHash);
    }

    private static void AssertCommit(RevertCommitCommandArguments? arguments)
    {
        Assert.NotNull(arguments);
        Assert.Equal(RepositoryId, arguments.RepositoryId);
        Assert.Equal("abc123", arguments.CommitHash);
    }
}
