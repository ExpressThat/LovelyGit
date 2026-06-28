using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Checkout;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CherryPick;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Merge;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Reset;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Revert;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Tags;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

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

    [Fact]
    public void TagsContext_DeserializesCamelCaseDeleteArguments()
    {
        var arguments = JsonSerializer.Deserialize(
            $$"""
            {
              "repositoryId": "{{RepositoryId}}",
              "tagName": "codex-test"
            }
            """,
            TagsJsonSerializerContext.Default.DeleteTagCommandArguments);

        Assert.Equal(RepositoryId, arguments?.RepositoryId);
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
                CreateJson("tagName", "v-test"),
                CheckoutJsonSerializerContext.Default.CheckoutTagCommandArguments,
                value =>
                {
                    var arguments = Assert.IsType<CheckoutTagCommandArguments>(value);
                    Assert.Equal(RepositoryId, arguments.RepositoryId);
                    Assert.Equal("v-test", arguments.TagName);
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
            {
                """
                {
                  "repositoryId": "bd3d4c1a-5061-453c-abef-70a2aafa6050",
                  "commitHash": "abc123",
                  "path": "README.md",
                  "viewMode": "Combined",
                  "ignoreWhitespace": true
                }
                """,
                CommitGraphJsonSerializerContext.Default.GetCommitFileDiffCommandArguments,
                value =>
                {
                    var arguments = Assert.IsType<GetCommitFileDiffCommandArguments>(value);
                    Assert.Equal(RepositoryId, arguments.RepositoryId);
                    Assert.Equal("README.md", arguments.Path);
                    Assert.True(arguments.IgnoreWhitespace);
                }
            },
            {
                """
                {
                  "repositoryId": "bd3d4c1a-5061-453c-abef-70a2aafa6050",
                  "path": "README.md",
                  "group": "Unstaged",
                  "viewMode": "Combined",
                  "ignoreWhitespace": true
                }
                """,
                WorkingTreeJsonSerializerContext.Default.GetWorkingTreeFileDiffArguments,
                value =>
                {
                    var arguments = Assert.IsType<GetWorkingTreeFileDiffArguments>(value);
                    Assert.Equal(RepositoryId, arguments.RepositoryId);
                    Assert.Equal("README.md", arguments.Path);
                    Assert.Equal(WorkingTreeChangeGroup.Unstaged, arguments.Group);
                    Assert.True(arguments.IgnoreWhitespace);
                }
            },
            {
                $$"""
                {
                  "repositoryId": "{{RepositoryId}}",
                  "files": [
                    {
                      "path": "README.md",
                      "status": "Modified",
                      "group": "Unstaged",
                      "additions": 1,
                      "deletions": 0,
                      "isBinary": false
                    }
                  ]
                }
                """,
                WorkingTreeJsonSerializerContext.Default.DiscardWorkingTreeChangesCommandArguments,
                value =>
                {
                    var arguments = Assert.IsType<DiscardWorkingTreeChangesCommandArguments>(value);
                    Assert.Equal(RepositoryId, arguments.RepositoryId);
                    var file = Assert.Single(arguments.Files);
                    Assert.Equal("README.md", file.Path);
                    Assert.Equal(WorkingTreeChangeGroup.Unstaged, file.Group);
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
