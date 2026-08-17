namespace Wayfare.Session.Transformations;

using System.Text;
using Wayfare.Infrastructure.AI;
using Wayfare.Session;
using Wayfare.Tools;

public enum ToolActionCategory
{
    Exploratory,
    PersistentMutation,
    TerminalAction,
    Other,
}

public sealed class DiagnosticCollapseRule : ITransformationRule
{
    private static readonly HashSet<string> _alwaysExploratoryTools = new(StringComparer.OrdinalIgnoreCase) { "list", "find" };
    private static readonly HashSet<string> _mutationTools = new(StringComparer.OrdinalIgnoreCase) { "write", "replace" };

    public string Name => "DiagnosticCollapse";

    public TrunkTransformResult Apply(IReadOnlyList<HistoryNode> currentTrunk, HistoryNode newTurn, IResourceIndex resourceIndex)
    {
        ArgumentNullException.ThrowIfNull(currentTrunk);
        ArgumentNullException.ThrowIfNull(newTurn);
        ArgumentNullException.ThrowIfNull(resourceIndex);

        if (!IsTerminalAction(newTurn))
        {
            return new TrunkTransformResult(false, currentTrunk);
        }

        int newTurnIndex = -1;
        for (int i = 0; i < currentTrunk.Count; i++)
        {
            if (currentTrunk[i].Id == newTurn.Id)
            {
                newTurnIndex = i;
                break;
            }
        }

        if (newTurnIndex <= 0)
        {
            return new TrunkTransformResult(false, currentTrunk);
        }

        int exploratoryEndIndex = (newTurnIndex > 0 && currentTrunk[newTurnIndex - 1].ToProjectedMessage() is ToolCallMessage)
            ? newTurnIndex - 1
            : newTurnIndex;

        int exploratoryStartIndex = exploratoryEndIndex;
        for (int i = exploratoryEndIndex - 1; i >= 0; i--)
        {
            if (IsExploratoryTurn(currentTrunk[i]))
            {
                exploratoryStartIndex = i;
            }
            else
            {
                break;
            }
        }

        int count = exploratoryEndIndex - exploratoryStartIndex;
        if (count < 1)
        {
            return new TrunkTransformResult(false, currentTrunk);
        }

        List<HistoryNode> exploratoryNodes = currentTrunk.Take(exploratoryEndIndex).Skip(exploratoryStartIndex).ToList();
        string summary = BuildExplorationSummary(exploratoryNodes);
        SessionMessage collapsedMessage = new SystemMessage(
            $"<collapsed_exploration count=\"{exploratoryNodes.Count}\" summary=\"{summary}\">\n" +
            $"[Collapsed {exploratoryNodes.Count} exploratory diagnostic turns preceding terminal action]\n" +
            $"</collapsed_exploration>");

        CollapsedExplorationNode collapsedNode = new(
            CollapsedNodes: exploratoryNodes.AsReadOnly(),
            CollapsedMessage: collapsedMessage,
            Summary: summary);

        List<HistoryNode> newTrunk = [
            .. currentTrunk.Take(exploratoryStartIndex),
            collapsedNode,
            .. currentTrunk.Skip(exploratoryEndIndex)
        ];

        return new TrunkTransformResult(true, newTrunk.AsReadOnly());
    }

    public static ToolActionCategory Categorise(string? toolName, string arguments = "", bool isSuccess = true)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return ToolActionCategory.Other;
        }

        if (_alwaysExploratoryTools.Contains(toolName))
        {
            return ToolActionCategory.Exploratory;
        }

        if (_mutationTools.Contains(toolName))
        {
            return isSuccess ? ToolActionCategory.TerminalAction : ToolActionCategory.PersistentMutation;
        }

        if (toolName.Equals("execute", StringComparison.OrdinalIgnoreCase))
        {
            bool isVerification = arguments.Contains("test", StringComparison.OrdinalIgnoreCase) ||
                                  arguments.Contains("build", StringComparison.OrdinalIgnoreCase);

            return isVerification && isSuccess ? ToolActionCategory.TerminalAction : ToolActionCategory.Exploratory;
        }

        return ToolActionCategory.Other;
    }

    public static bool IsTerminalAction(HistoryNode node)
    {
        SessionMessage message = node.ToProjectedMessage();

        if (message is ToolResultMessage toolResultMessage)
        {
            return toolResultMessage.Results.Any(result =>
                result.Success &&
                Categorise(result.ToolName, result.DisplayMessage, result.Success) == ToolActionCategory.TerminalAction);
        }

        return false;
    }

    public static bool IsExploratoryTurn(HistoryNode node)
    {
        if (node is CollapsedExplorationNode || node is SupersededStateNode)
        {
            return false;
        }

        SessionMessage message = node.ToProjectedMessage();

        if (message is ToolCallMessage toolCallMessage)
        {
            return toolCallMessage.ToolCalls.All(call =>
                Categorise(call.Name, call.Arguments) == ToolActionCategory.Exploratory);
        }

        if (message is ToolResultMessage toolResultMessage)
        {
            return toolResultMessage.Results.All(result =>
                Categorise(result.ToolName, result.DisplayMessage, result.Success) == ToolActionCategory.Exploratory);
        }

        return false;
    }

    private static string BuildExplorationSummary(IReadOnlyList<HistoryNode> nodes)
    {
        List<string> toolActions = [];

        foreach (HistoryNode node in nodes)
        {
            SessionMessage message = node.ToProjectedMessage();

            if (message is ToolCallMessage toolCallMessage)
            {
                foreach (ToolCall call in toolCallMessage.ToolCalls)
                {
                    toolActions.Add(call.Name);
                }
            }
            else if (message is ToolResultMessage toolResultMessage)
            {
                foreach (ToolExecutionResult res in toolResultMessage.Results)
                {
                    toolActions.Add(res.ToolName);
                }
            }
        }

        return toolActions.Count > 0
            ? $"Explored via [{string.Join(", ", toolActions.Distinct())}]"
            : "Exploratory diagnostic sequence";
    }
}
