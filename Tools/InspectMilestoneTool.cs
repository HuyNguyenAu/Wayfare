namespace Wayfare.Tools;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wayfare.Core.Abstractions;
using Wayfare.Core.Models;
using Wayfare.Core.Models.Ast;
using Wayfare.Core.Models.Messages;

internal sealed class InspectMilestoneTool(ISession session) : ITool
{
    public string Name => "inspect_milestone";
    public string DisplayName => "Inspect Milestone";
    public string Description => "Inspect details of a past milestone including all turns. Parameters: id (string, required).";

    public string GetInvocationMessage(string arguments)
    {
        return TryParseId(arguments, out string? id)
            ? $"[{DisplayName}] [{id}]"
            : $"[{DisplayName}] [{arguments}]";
    }

    public Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(arguments) || !TryParseId(arguments, out string? targetId) || string.IsNullOrWhiteSpace(targetId))
        {
            return Task.FromResult(new ToolExecutionResult(
                Success: false,
                DisplayMessage: "Failed to inspect milestone: 'id' parameter is required.",
                Result: string.Empty,
                Error: "Failed to inspect milestone: 'id' parameter is required. Usage: {\"id\": \"<milestone_id>\"}"));
        }

        BranchNode? matchedBranch = null;

        foreach (HistoryNode node in session.History)
        {
            if (node is BranchNode branchNode && branchNode.Id == targetId)
            {
                matchedBranch = branchNode;
            }
        }

        if (matchedBranch is null)
        {
            string availableIds = session.History.Count > 0 ?
                string.Join(", ", session.History.Select(branch => branch.Id)) : "None";

            return Task.FromResult(new ToolExecutionResult(
                Success: false,
                DisplayMessage: $"Milestone '{targetId}' not found.",
                Result: string.Empty,
                Error: $"No milestone found matching ID '{targetId}'. Available milestone IDs: [{availableIds}]"));
        }

        StringBuilder output = new();
        output.AppendLine($"Milestone [{matchedBranch.Id}]");
        output.AppendLine($"Created At: {matchedBranch.CreatedAt:yyyy-MM-dd HH:mm:ss UTC}");
        output.AppendLine($"Summary: {(string.IsNullOrWhiteSpace(matchedBranch.Summary) ? "(Active / Unsquashed)" : matchedBranch.Summary)}");
        output.AppendLine("Turns:");

        if (matchedBranch.Turns.Count == 0)
        {
            output.AppendLine("  (No turns in this milestone)");
        }
        else
        {
            for (int i = 0; i < matchedBranch.Turns.Count; i++)
            {
                TurnNode turn = matchedBranch.Turns[i];
                output.AppendLine($"--- Turn {i + 1} ---");
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
                foreach (ToolCall call in toolCallMessage.ToolCalls)
                {
                    builder.AppendLine($"Tool Call: {call.Name}({call.Arguments})");
                }
                break;
            case ToolResultMessage toolResultMessage:
                foreach (ToolExecutionResult result in toolResultMessage.Results)
                {
                    string content = result.Success ? result.Result : result.Error;
                    builder.AppendLine($"Tool Result ({result.ToolName}): {content}");
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
            InspectMilestoneArguments? args = JsonSerializer.Deserialize<InspectMilestoneArguments>(arguments, ToolHelpers.JsonOptions);
            id = args?.EffectiveId;
            return !string.IsNullOrWhiteSpace(id);
        }
        catch
        {
            id = null;
            return false;
        }
    }

    private sealed record InspectMilestoneArguments(
        [property: JsonPropertyName("id")] string Id = "",
        [property: JsonPropertyName("branchId")] string BranchId = "",
        [property: JsonPropertyName("milestoneId")] string MilestoneId = "")
    {
        public string EffectiveId => !string.IsNullOrWhiteSpace(Id) ? Id : (!string.IsNullOrWhiteSpace(BranchId) ? BranchId : MilestoneId);
    }
}
