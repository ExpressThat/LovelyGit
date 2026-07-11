using System.Text;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitObjectSignatureParserTests
{
    [Theory]
    [InlineData("-----BEGIN SSH SIGNATURE-----", CommitSignatureKind.Ssh)]
    [InlineData("-----BEGIN PGP SIGNATURE-----", CommitSignatureKind.OpenPgp)]
    [InlineData("-----BEGIN SIGNED MESSAGE-----", CommitSignatureKind.X509)]
    [InlineData("unrecognized signature", CommitSignatureKind.Unknown)]
    public void ParseCommit_DetectsHeaderSignatureWithoutAllocatingItsBody(
        string signature,
        CommitSignatureKind expected)
    {
        var commit = Parse($"gpgsig {signature}\n continuation");

        Assert.Equal(expected, CommitGraphCommitMapper.MapSignatureKind(commit.SignatureKind));
        Assert.Empty(commit.Body);
    }

    [Fact]
    public void ParseCommit_DetectsSha256SignatureHeaderWithCrLf()
    {
        var commit = Parse("gpgsig-sha256 -----BEGIN SSH SIGNATURE-----\r\n continuation", "\r\n");

        Assert.Equal(GitSignatureKind.Ssh, commit.SignatureKind);
    }

    [Fact]
    public void ParseCommit_DoesNotTreatSignatureTextInMessageAsAHeader()
    {
        const string body = "Message mentioning\ngpgsig -----BEGIN PGP SIGNATURE-----";
        var data = $"tree {TreeHash}\nauthor Test <test@example.invalid> 1 +0000\n\n{body}";

        var commit = GitObjectParsers.ParseCommit(CommitId, Encoding.UTF8.GetBytes(data));

        Assert.Equal(GitSignatureKind.None, commit.SignatureKind);
    }

    private static GitCommit Parse(string signatureHeader, string newline = "\n")
    {
        var data = string.Join(newline,
            $"tree {TreeHash}",
            "author Test <test@example.invalid> 1 +0000",
            signatureHeader,
            string.Empty,
            "Subject");
        return GitObjectParsers.ParseCommit(
            CommitId,
            Encoding.UTF8.GetBytes(data),
            includeBody: false);
    }

    private static readonly GitObjectId CommitId = GitObjectId.Parse(new string('a', 40));
    private const string TreeHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
}
