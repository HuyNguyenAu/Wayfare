namespace Wayfare.Agent;

using System.Text;
using Wayfare.Session;

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
    public IReadOnlyList<SessionMessage> BuildMessages(IReadOnlyList<HistoryNode> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        if (history.Count == 0 || history[^1] is not BranchNode activeBranch)
        {
            throw new InvalidOperationException($"Session history must contain at least one {nameof(BranchNode)}.");
        }

        List<SessionMessage> messages = [
            new SystemMessage(SystemPromptBuilder.Build())
        ];

        foreach (TurnNode turn in activeBranch.Turns)
        {
            messages.Add(turn.Message);
        }

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
