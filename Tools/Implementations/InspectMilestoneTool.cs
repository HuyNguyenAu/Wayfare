namespace Wayfare.Tools.Implementations;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wayfare.Infrastructure.AI;
using Wayfare.Session;
using Wayfare.Tools;

internal sealed class InspectMilestoneTool(ISession session) : ITool
{
    public string Name => "inspect_milestone";
    public string DisplayName => "Inspect Milestone";
    public string Description => "Inspect details of a past milestone including all turns. Parameters: id (string, required).";

    public ToolSchema Parameters => ToolSchema.Object(new Dictionary<string, ToolPropertySchema>
    {
        ["id"] = ToolPropertySchema.String("The milestone ID or milestone ID prefix to inspect.")
    });

    public string GetInvocationMessage(string arguments)
    {
        return TryParseId(arguments, out string? id)
            ? $"[{DisplayName}] [{id}]"
            : $"[{DisplayName}] [{arguments}]";
    }

    public Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        string availableIds = session.History.Count > 0 ?
            string.Join(", ", session.History.Select(branch => branch.Id)) : "None";

        if (string.IsNullOrWhiteSpace(arguments) || !TryParseId(arguments, out string? targetId) || string.IsNullOrWhiteSpace(targetId))
        {
            return Task.FromResult(new ToolExecutionResult(
                Success: false,
                DisplayMessage: "Failed to inspect milestone: 'id' parameter is required.",
                Result: string.Empty,
                Error: $"Failed to inspect milestone: 'id' parameter is required. Available milestone IDs in session: [{availableIds}]. Usage: {{\"id\": \"<milestone_id>\"}}"));
        }

        BranchNode? matchedBranch = null;

        foreach (HistoryNode node in session.History)
        {
            if (node is BranchNode branchNode && (
                branchNode.Id.Equals(targetId, StringComparison.OrdinalIgnoreCase) ||
                branchNode.Id.StartsWith(targetId, StringComparison.OrdinalIgnoreCase)))
            {
                matchedBranch = branchNode;
                break;
            }
        }

        if (matchedBranch is null)
        {
            return Task.FromResult(new ToolExecutionResult(
                Success: false,
                DisplayMessage: $"Milestone '{targetId}' not found.",
                Result: string.Empty,
                Error: $"No milestone found matching ID '{targetId}'. Available milestone IDs in session: [{availableIds}]. Call inspect_milestone with a valid ID from this list."));
        }

        StringBuilder output = new();
        output.AppendLine($"Milestone [{matchedBranch.Id}]");
        output.AppendLine($"Status: {matchedBranch.Status}");
        output.AppendLine($"Created At: {matchedBranch.CreatedAt:yyyy-MM-dd HH:mm:ss UTC}");
        output.AppendLine($"Summary: {(string.IsNullOrWhiteSpace(matchedBranch.Summary) ? "(Active / Unsquashed)" : matchedBranch.Summary)}");
        output.AppendLine("Turns:");

        if (matchedBranch.Turns.Count == 0)
        {
            output.AppendLine("  (No turns in this milestone)");
        }
        else
        {
            for (int turnIndex = 0; turnIndex < matchedBranch.Turns.Count; turnIndex++)
            {
                TurnNode turn = matchedBranch.Turns[turnIndex];
                output.AppendLine($"--- Turn {turnIndex + 1} ---");
                FormatTurn(turn, output);
            }
        }

        return Task.FromResult(new ToolExecutionResult(
            Success: true,
            DisplayMessage: $"Inspected milestone [{matchedBranch.Id}] ({matchedBranch.Turns.Count} turns).",
            Result: output.ToString(),
            Error: string.Empty));
    }

    private static void FormatTurn(TurnNode turn, StringBuilder builder)
    {
        switch (turn.Message)
        {
            case UserMessage userMessage:
                builder.AppendLine($"User: {userMessage.Content}");
                break;
            case AssistantMessage assistantMessage:
                builder.AppendLine($"Assistant: {assistantMessage.Content}");
                break;
            case ToolCallMessage toolCallMessage:
                foreach (ToolCall toolCall in toolCallMessage.ToolCalls)
                {
                    builder.AppendLine($"Tool Call: {toolCall.Name}({toolCall.Arguments})");
                }
                break;
            case ToolResultMessage toolResultMessage:
                foreach (ToolExecutionResult toolResult in toolResultMessage.Results)
                {
                    string content = toolResult.Success ? toolResult.Result : toolResult.Error;
                    builder.AppendLine($"Tool Result ({toolResult.ToolName}): {content}");
                }
                break;
            default:
                builder.AppendLine($"{turn.Message.GetType().Name}: {turn.Message}");
                break;
        }
    }

    private static bool TryParseId(string arguments, out string? id)
    {
        try
        {
            InspectMilestoneArguments? parsedArguments = JsonSerializer.Deserialize<InspectMilestoneArguments>(arguments, ToolHelpers.JsonOptions);
            id = parsedArguments?.Id;
            return !string.IsNullOrWhiteSpace(id);
        }
        catch
        {
            id = null;
            return false;
        }
    }

    private sealed record InspectMilestoneArguments([property: JsonPropertyName("id")] string Id = "");
}
