namespace Wayfare.Tools.Implementations;

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wayfare.Session;
using Wayfare.Tools;

internal sealed class InspectMilestoneTool(ISession session) : ITool
{
    private readonly ISession _session = session ?? throw new ArgumentNullException(nameof(session));

    public string Name => "inspect_milestone";
    public string DisplayName => "Inspect Milestone";
    public string Description => "Inspect details and compressed turns of a past milestone. Parameters: id (string, required).";

    public ToolSchema Parameters => ToolSchema.Object(new Dictionary<string, ToolPropertySchema>
    {
        ["id"] = ToolPropertySchema.String("The milestone ID or milestone ID prefix to inspect.")
    }, required: ["id"]);

    public string GetInvocationMessage(string arguments)
    {
        return TryParseArguments(arguments, out InspectMilestoneArguments? parsed)
            ? $"[{DisplayName}] [{parsed.Id}]"
            : $"[{DisplayName}] [{arguments}]";
    }

    public Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        string availableIds = _session.History.Count > 0 ?
            string.Join(", ", _session.History.Select(branch => branch.Id)) : "None";

        if (string.IsNullOrWhiteSpace(arguments) || !TryParseArguments(arguments, out InspectMilestoneArguments? args) || string.IsNullOrWhiteSpace(args.Id))
        {
            return Task.FromResult(new ToolExecutionResult(
                Success: false,
                DisplayMessage: "Failed to inspect milestone: 'id' parameter is required.",
                Result: string.Empty,
                Error: $"Failed to inspect milestone: 'id' parameter is required. Available milestone IDs in session: [{availableIds}]. Usage: {{\"id\": \"<milestone_id>\"}}"));
        }

        string targetId = args.Id;
        BranchContainerNode? matchedBranch = null;

        foreach (HistoryNode node in _session.History)
        {
            if (node is BranchContainerNode branchNode && (
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
        output.AppendLine("Turns (Compressed Milestone Turns):");

        if (matchedBranch.Turns.Count == 0)
        {
            output.AppendLine("  (No turns in this milestone)");
        }
        else
        {
            for (int turnIndex = 0; turnIndex < matchedBranch.Turns.Count; turnIndex++)
            {
                HistoryNode node = matchedBranch.Turns[turnIndex];
                output.AppendLine($"--- Turn {turnIndex + 1} ---");
                node.ToProjectedMessage().AppendTraceLines(output);
            }
        }

        return Task.FromResult(new ToolExecutionResult(
            Success: true,
            DisplayMessage: $"Inspected milestone [{matchedBranch.Id}] ({matchedBranch.Turns.Count} compressed turns).",
            Result: output.ToString(),
            Error: string.Empty));
    }

    private static bool TryParseArguments(string arguments, [NotNullWhen(true)] out InspectMilestoneArguments? parsed)
    {
        try
        {
            parsed = JsonSerializer.Deserialize<InspectMilestoneArguments>(arguments, ToolHelpers.JsonOptions);
            return parsed is not null && !string.IsNullOrWhiteSpace(parsed.Id);
        }
        catch
        {
            parsed = null;
            return false;
        }
    }

    private sealed record InspectMilestoneArguments([property: JsonPropertyName("id")] string Id = "");
}
