using System.Net.Mail;
using ExpressThat.LovelyGit.Services.Git.Cli;

namespace ExpressThat.LovelyGit.Services.Git.Configuration;

internal sealed class GitCommitIdentityCommandService
{
    private readonly GitOperationService _operations;
    private readonly NativeGitCommitIdentityReader _reader;

    public GitCommitIdentityCommandService(
        GitOperationService operations,
        NativeGitCommitIdentityReader reader)
    {
        _operations = operations;
        _reader = reader;
    }

    public async Task<GitCommitIdentity> SaveAsync(
        string repositoryPath,
        string? name,
        string? email,
        CancellationToken cancellationToken)
    {
        var validatedName = ValidateName(name);
        var validatedEmail = ValidateEmail(email);
        await SetAsync(repositoryPath, "user.name", validatedName, cancellationToken)
            .ConfigureAwait(false);
        await SetAsync(repositoryPath, "user.email", validatedEmail, cancellationToken)
            .ConfigureAwait(false);
        return await _reader.ReadAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
    }

    public async Task<GitCommitIdentity> ClearAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        await UnsetAsync(repositoryPath, "user.name", cancellationToken).ConfigureAwait(false);
        await UnsetAsync(repositoryPath, "user.email", cancellationToken).ConfigureAwait(false);
        return await _reader.ReadAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
    }

    private Task SetAsync(
        string repositoryPath,
        string key,
        string value,
        CancellationToken cancellationToken) =>
        _operations.ExecuteRequiredBufferedAsync(
            "Save commit identity",
            ["config", "--local", "--replace-all", key, value],
            repositoryPath,
            "Review the repository-local Git configuration and try again.",
            cancellationToken);

    private async Task UnsetAsync(
        string repositoryPath,
        string key,
        CancellationToken cancellationToken)
    {
        var result = await _operations.ExecuteBufferedAsync(
            "Clear commit identity",
            ["config", "--local", "--unset-all", key],
            repositoryPath,
            "Review the repository-local Git configuration and try again.",
            cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess && result.ExitCode != 5)
        {
            throw new GitOperationException(result);
        }
    }

    private static string ValidateName(string? value)
    {
        var name = value?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Enter the name to record on commits.", nameof(value));
        }

        if (name.Contains('\n') || name.Contains('\r'))
        {
            throw new ArgumentException("The commit name cannot contain a line break.", nameof(value));
        }

        return name;
    }

    private static string ValidateEmail(string? value)
    {
        var email = value?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Enter the email to record on commits.", nameof(value));
        }

        try
        {
            var parsed = new MailAddress(email);
            if (!parsed.Address.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException();
            }
        }
        catch (FormatException)
        {
            throw new ArgumentException("Enter a valid commit email address.", nameof(value));
        }

        return email;
    }
}
