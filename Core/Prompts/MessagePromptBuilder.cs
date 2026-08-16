using System.Text;
using Wayfare.Core.Abstractions;
using Wayfare.Core.Models.Ast;
using Wayfare.Core.Models.Messages;

namespace Wayfare.Core.Prompts;

public class MessagePromptBuilder : IMessagePromptBuilder
{
    public IReadOnlyList<SessionMessage> BuildMessages(IReadOnlyList<ITool> tools, IReadOnlyList<HistoryNode> history)
    {
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(history);

        if (history.Count == 0 || history[^1] is not BranchNode activeBranch)
        {
            throw new InvalidOperationException($"Session history must contain at least one {nameof(BranchNode)}.");
        }

        List<SessionMessage> messages = [
            new SystemMessage(SystemPromptBuilder.Build(tools))
        ];

        if (history.Count > 1)
        {
            messages.Add(new UserMessage(BuildLinearTrunk(history)));
            messages.Add(new AssistantMessage("Acknowledged completed milestones. Proceeding with active task."));
        }

        foreach (TurnNode turn in activeBranch.Turns)
        {
            messages.Add(turn.Message);
        }

        return messages.AsReadOnly();
    }

    private static string BuildLinearTrunk(IReadOnlyList<HistoryNode> history)
    {
        BranchNode firstBranch = (BranchNode)history[0];
        UserMessage firstUserMessage = (UserMessage)firstBranch.Turns[0].Message;

        StringBuilder trunk = new();
        trunk.AppendLine($"Objective: {firstUserMessage.Content}");
        trunk.AppendLine("\nMilestones:");

        for (int i = 0; i < history.Count - 1; i++)
        {
            BranchNode branch = (BranchNode)history[i];
            trunk.AppendLine($"{i + 1}. [{branch.Id}]: {branch.Summary}");
        }

        trunk.AppendLine("\nNote: Past turns are squashed into milestones. Call inspect_milestones(id) to view more details.");

        return trunk.ToString();
    }
}
