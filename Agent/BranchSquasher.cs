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

        if (session.History.Count == 0 || session.History[^1] is not BranchNode activeBranch)
        {
            throw new InvalidOperationException($"Cannot squash active branch because session history does not end with a {nameof(BranchNode)}.");
        }

        List<ChatMessage> messages = [
            new ChatMessage(ChatRole.System, SquashPromptBuilder.Build()),
            new ChatMessage(ChatRole.User, BuildTraceString(activeBranch))
        ];
        ChatResponse response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);

        return (response.Text ?? string.Empty).Trim();
    }

    private static string BuildTraceString(BranchNode activeBranch)
    {
        StringBuilder traceBuilder = new();
        traceBuilder.AppendLine("<trace>");

        foreach (TurnNode turn in activeBranch.Turns)
        {
            turn.Message.AppendTraceLines(traceBuilder);
        }

        traceBuilder.Append("</trace>");

        return traceBuilder.ToString();
    }
}

