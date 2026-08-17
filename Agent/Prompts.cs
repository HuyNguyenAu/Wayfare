namespace Wayfare.Agent;

using System.Text;
using Wayfare.Session;
using Wayfare.Tools;

public static class SystemPromptBuilder
{
    public static string Build()
    {
        StringBuilder promptBuilder = new();

        promptBuilder.AppendLine("You are an expert autonomous software engineer.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Rules:");
        promptBuilder.AppendLine("- Read a file before editing it.");
        promptBuilder.AppendLine("- When editing with replace, oldText must match the file content exactly, including whitespace.");
        promptBuilder.AppendLine("- Keep responses concise and focused.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"Operating system: {Environment.OSVersion}");
        promptBuilder.AppendLine($"Working directory: {Directory.GetCurrentDirectory()}");

        return promptBuilder.ToString();
    }
}

public sealed class MessagePromptBuilder : IMessagePromptBuilder
{
    private readonly int _maxActiveObservationsToRetain;

    public MessagePromptBuilder(int maxActiveObservationsToRetain)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxActiveObservationsToRetain);
        _maxActiveObservationsToRetain = maxActiveObservationsToRetain;
    }

    public IReadOnlyList<SessionMessage> BuildMessages(IReadOnlyList<HistoryNode> history, string intent)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(intent);

        if (history.Count == 0 || history[^1] is not BranchNode activeBranch)
        {
            throw new InvalidOperationException($"Session history must contain at least one {nameof(BranchNode)}.");
        }

        List<SessionMessage> messages = [
            new SystemMessage(SystemPromptBuilder.Build())
        ];

        List<TurnNode> turns = activeBranch.Turns;

        List<int> toolResultIndices = [];

        for (int turnIndex = 0; turnIndex < turns.Count; turnIndex++)
        {
            if (turns[turnIndex].Message is ToolResultMessage)
            {
                toolResultIndices.Add(turnIndex);
            }
        }

        HashSet<int> indicesToTombstone = [.. toolResultIndices.Take(Math.Max(0, toolResultIndices.Count - _maxActiveObservationsToRetain))];

        for (int turnIndex = 0; turnIndex < turns.Count; turnIndex++)
        {
            SessionMessage turnMessage = turns[turnIndex].Message;

            if (indicesToTombstone.Contains(turnIndex) && turnMessage is ToolResultMessage toolResultMessage)
            {
                List<ToolExecutionResult> compactResults = [.. toolResultMessage.Results.Select(result => result with
                {
                    Result = $"<observation tool=\"{result.ToolName}\" status=\"tombstoned\">[Historical output compacted]</observation>",
                    DisplayMessage = "[Compacted historical observation]"
                })];

                messages.Add(new ToolResultMessage(compactResults));
            }
            else
            {
                messages.Add(turnMessage);
            }
        }

        string stateBoard = $"""
        <state_board>
        Active Goal: {(string.IsNullOrWhiteSpace(intent) ? "Complete user task" : intent)}
        Working Directory: {Directory.GetCurrentDirectory()}
        </state_board>
        """;

        messages.Add(new UserMessage(stateBoard));

        return messages.AsReadOnly();
    }
}

public static class IntentPromptBuilder
{
    public static string BuildSystem() => """
        Extract the active goal from user input. Wrap the active goal in <goal>...</goal>.
        Example:
        <goal>Fix null reference exception in SessionStore.cs</goal>
        """;

    public static string BuildUser(string currentIntent, string userInput)
    {
        ArgumentNullException.ThrowIfNull(currentIntent);
        ArgumentException.ThrowIfNullOrWhiteSpace(userInput);

        return $"""
        <current_intent>{(string.IsNullOrWhiteSpace(currentIntent) ? "none" : currentIntent.Trim())}</current_intent>
        <user_input>{userInput.Trim()}</user_input>
        """;
    }
}

public static class SquashPromptBuilder
{
    public static string Build() => """
        Summarise the milestone from the execution trace. Wrap the summary in <milestone_summary>...</milestone_summary> using STARL format (Situation, Task, Action, Result, Learnings).
        Each section must be 1-2 concise sentences.

        Example:
        <milestone_summary>
        Situation: Context before starting this branch.
        Task: Specific task or goal.
        Action: Steps taken to address the task.
        Result: Concrete outcome or produced artifacts.
        Learnings: Key insights or constraints discovered.
        </milestone_summary>
        """;
}
