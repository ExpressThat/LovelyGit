using System.Text;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;

namespace ExpressThat.LovelyGit.Services.Git.Rebase;

internal static class InteractiveRebaseTodoWriter
{
    public static async Task<string> WriteAsync(
        string directory,
        InteractiveRebasePlanResponse current,
        IReadOnlyList<InteractiveRebasePlanItem> plan,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);
        var subjects = current.Commits.ToDictionary(commit => commit.Hash, commit => commit.Subject);
        var todo = new StringBuilder(plan.Count * 80);
        for (var index = 0; index < plan.Count; index++)
        {
            var item = plan[index];
            var action = item.Action == InteractiveRebaseAction.Reword
                ? "pick"
                : item.Action.ToString().ToLowerInvariant();
            todo.Append(action).Append(' ').Append(item.Hash).Append(' ')
                .AppendLine(SanitizeSubject(subjects[item.Hash]));
            if (item.Action == InteractiveRebaseAction.Reword)
            {
                var messagePath = Path.Combine(directory, $"message-{index}.txt");
                await File.WriteAllTextAsync(
                    messagePath, item.Message, new UTF8Encoding(false), cancellationToken)
                    .ConfigureAwait(false);
                todo.Append("exec git commit --amend -F ")
                    .AppendLine(GitShellArgument.Quote(messagePath));
            }
        }

        var todoPath = Path.Combine(directory, "todo");
        await File.WriteAllTextAsync(
            todoPath, todo.ToString(), new UTF8Encoding(false), cancellationToken)
            .ConfigureAwait(false);
        return todoPath;
    }

    private static string SanitizeSubject(string value) =>
        value.Replace('\r', ' ').Replace('\n', ' ');

}
