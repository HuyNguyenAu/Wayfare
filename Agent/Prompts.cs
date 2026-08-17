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

        if (history.Count == 0 || history[^1] is not BranchContainerNode activeBranch)
        {
            throw new InvalidOperationException($"Session history must contain at least one {nameof(BranchContainerNode)}.");
        }

        List<SessionMessage> messages = [
            new SystemMessage(SystemPromptBuilder.Build()),
            .. activeBranch.Turns.ToProjectedMessages()
        ];

        return messages.AsReadOnly();
    }
}

public static class SquashPromptBuilder
{
    public static string Build() => """
        Summarise the milestone from the execution trace. Wrap the summary in <milestone_summary>...</milestone_summary> using STARL format (Situation, Task, Action, Result, Learnings) and explicitly capture Key Artifacts for Exact Invariants.
        Each STARL section must be 1-2 concise sentences.
        Under Key Artifacts, list exact file paths modified/created/read, exact commands executed, and key state/schema invariants established.

        Example:
        <milestone_summary>
        Situation: Context before starting this branch.
        Task: Specific task or goal.
        Action: Steps taken to address the task.
        Result: Concrete outcome or produced changes.
        Learnings: Key insights or constraints discovered.
        Key Artifacts:
        - Files: [exact file paths created, modified, or inspected]
        - Invariants: [exact verified outputs, exit codes, state transitions, or symbol/schema guarantees]
        </milestone_summary>
        """;
}
