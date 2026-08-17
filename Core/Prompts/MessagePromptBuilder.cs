namespace Wayfare.Core.Prompts;

using System.Text;
using Wayfare.Core.Abstractions;
using Wayfare.Core.Models;
using Wayfare.Core.Models.Ast;
using Wayfare.Core.Models.Messages;

public class MessagePromptBuilder : IMessagePromptBuilder
{
    public IReadOnlyList<SessionMessage> BuildMessages(IReadOnlyList<ITool> tools, IReadOnlyList<HistoryNode> history, string intent)
    {
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(intent);

        if (history.Count == 0 || history[^1] is not BranchNode activeBranch)
        {
            throw new InvalidOperationException($"Session history must contain at least one {nameof(BranchNode)}.");
        }

        List<SessionMessage> messages = [
            new SystemMessage(SystemPromptBuilder.Build(tools))
        ];

        if (history.Count > 1)
        {
            messages.Add(new UserMessage(BuildLinearTrunk(history, intent, activeBranch.Id)));
            messages.Add(new AssistantMessage("Acknowledged completed milestones and active intent. Proceeding with active horizon."));
        }
        else
        {
            messages.Add(new UserMessage(BuildInitialIntentHeader(intent, activeBranch.Id)));
            messages.Add(new AssistantMessage("Acknowledged active intent. Proceeding with active horizon."));
        }

        foreach (TurnNode turn in activeBranch.Turns)
        {
            messages.Add(turn.Message);
        }

        return messages.AsReadOnly();
    }

    private static string BuildLinearTrunk(IReadOnlyList<HistoryNode> history, string intent, string activeBranchId)
    {
        StringBuilder trunk = new();
        trunk.AppendLine("### Active Intent");
        trunk.AppendLine(string.IsNullOrWhiteSpace(intent) ? "(None)" : intent);
        trunk.AppendLine();
        trunk.AppendLine("Milestones:");

        for (int i = 0; i < history.Count - 1; i++)
        {
            BranchNode branch = (BranchNode)history[i];
            string statusTag = branch.Status == BranchStatus.Abandoned ? " [Abandoned]" : string.Empty;
            trunk.AppendLine($"{i + 1}. [{branch.Id}]{statusTag}: {branch.Summary}");
        }

        trunk.AppendLine();
        trunk.AppendLine($"Active Horizon: Milestone [{activeBranchId}]");
        trunk.AppendLine("Note: Past turns are squashed into milestones. Call inspect_milestone(id) to view all previous turns of that milestone.");

        return trunk.ToString();
    }

    private static string BuildInitialIntentHeader(string intent, string activeBranchId)
    {
        StringBuilder header = new();
        header.AppendLine("### Active Intent");
        header.AppendLine(string.IsNullOrWhiteSpace(intent) ? "(None)" : intent);
        header.AppendLine();
        header.AppendLine($"Active Horizon: Milestone [{activeBranchId}]");

        return header.ToString();
    }
}
