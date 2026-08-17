namespace Wayfare.Agent;

using System.Text;
using Wayfare.Session;
using Wayfare.Tools;

#region System Prompt Builder

public static class SystemPromptBuilder
{
    public static string Build(IReadOnlyList<ITool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        StringBuilder promptBuilder = new();

        promptBuilder.AppendLine("You assist human co-creators by nurturing codebases as living ecosystems—reading, editing, and cultivating sustainable code health.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Available tools:");

        foreach (ITool tool in tools)
        {
            promptBuilder.AppendLine($"- {tool.Name}: {tool.Description}");
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Rules:");
        promptBuilder.AppendLine("- To see what files exist, use list or find.");
        promptBuilder.AppendLine("- To read a file, use read.");
        promptBuilder.AppendLine("- To edit a file, use replace. The oldText must match exactly what is in the file, including whitespace.");
        promptBuilder.AppendLine("- The oldText in replace must appear exactly once in the file. If it appears more than once, add more surrounding lines to make it unique.");
        promptBuilder.AppendLine("- To create a new file or completely overwrite one, use write.");
        promptBuilder.AppendLine("- To view all turns of a past milestone, use inspect_milestone with its id.");
        promptBuilder.AppendLine("- Always read a file before editing it.");
        promptBuilder.AppendLine("- Keep responses short and focused. Show exact file paths when working with files.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"Current date: {DateTime.UtcNow:yyyy-MM-dd HH:mm UTC}");
        promptBuilder.AppendLine($"Current operating system: {Environment.OSVersion}");
        promptBuilder.AppendLine($"Current working directory: {Directory.GetCurrentDirectory()}");

        return promptBuilder.ToString();
    }
}

#endregion

#region Message Prompt Builder

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

#endregion

#region Intent Prompt Builder

public static class IntentPromptBuilder
{
    public static string BuildSystem()
    {
        StringBuilder builder = new();
        builder.AppendLine("You are an intent summariser for a coding assistant.");
        builder.AppendLine("Given the current session intent and new user input, state the active goal in 1-2 concise sentences.");
        builder.AppendLine("Output ONLY the goal text without any labels, introductory, or concluding remarks.");

        return builder.ToString();
    }

    public static string BuildUser(string currentIntent, string userInput)
    {
        ArgumentNullException.ThrowIfNull(currentIntent);
        ArgumentException.ThrowIfNullOrWhiteSpace(userInput);

        StringBuilder builder = new();
        builder.AppendLine($"Current Session Intent: {(string.IsNullOrWhiteSpace(currentIntent) ? "(None)" : currentIntent)}");
        builder.AppendLine($"User Input: {userInput}");

        return builder.ToString();
    }
}

#endregion

#region Squash Prompt Builder

public static class SquashPromptBuilder
{
    public static string Build()
    {
        StringBuilder promptBuilder = new();

        promptBuilder.AppendLine("You are a concise technical summariser. Extract the key milestone data from the provided execution trace.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Follow the STARL format (Situation, Task, Action, Result, Learnings) exactly.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Rules:");
        promptBuilder.AppendLine("- Be strictly factual and concise. No conversational filler, intros, or outros.");
        promptBuilder.AppendLine("- Keep each field to 1-2 sentences.");
        promptBuilder.AppendLine("- Output ONLY the formatted text below.");

        return promptBuilder.ToString();
    }
}

#endregion
