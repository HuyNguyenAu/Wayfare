namespace Wayfare.Agent;

using System.Text;
using Microsoft.Extensions.AI;
using Wayfare.Session;

public sealed class BranchSquasher(IChatClient chatClient) : IBranchSquasher
{
    private readonly IChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    public async Task<string> SquashAsync(ISession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.History.Count == 0 || session.History[^1] is not BranchContainerNode activeBranch)
        {
            throw new InvalidOperationException($"Cannot squash active branch because session history does not end with a {nameof(BranchContainerNode)}.");
        }

        List<ChatMessage> messages = [
            new ChatMessage(ChatRole.System, SquashPromptBuilder.Build()),
            new ChatMessage(ChatRole.User, BuildTraceString(activeBranch))
        ];
        ChatResponse response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        string summary = (response.Text ?? string.Empty).Trim();

        if (summary.Contains("<milestone_summary>", StringComparison.Ordinal) && summary.Contains("</milestone_summary>", StringComparison.Ordinal))
        {
            int start = summary.IndexOf("<milestone_summary>", StringComparison.Ordinal) + "<milestone_summary>".Length;
            int end = summary.IndexOf("</milestone_summary>", start, StringComparison.Ordinal);

            if (end > start)
            {
                summary = summary[start..end].Trim();
            }
        }

        return summary;
    }

    private static string BuildTraceString(BranchContainerNode activeBranch)
    {
        StringBuilder traceBuilder = new();
        traceBuilder.AppendLine("<trace>");

        foreach (HistoryNode turn in activeBranch.Turns)
        {
            turn.ToProjectedMessage().AppendTraceLines(traceBuilder);
        }

        traceBuilder.Append("</trace>");

        return traceBuilder.ToString();
    }
}
