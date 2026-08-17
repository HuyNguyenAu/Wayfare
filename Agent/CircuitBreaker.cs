namespace Wayfare.Agent;

using System.Diagnostics.CodeAnalysis;
using Wayfare.Infrastructure.AI;
using Wayfare.Tools;

public sealed class CircuitBreaker : ICircuitBreaker
{
    private readonly int _maxTurns;
    private readonly HashSet<(string ToolName, string Arguments)> _previousTurnFailedCalls = [];
    private int _currentTurn = 0;

    public CircuitBreaker(int maxTurns = 15)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTurns);
        _maxTurns = maxTurns;
    }

    public bool TryAdvanceTurn([NotNullWhen(false)] out string? reason)
    {
        if (++_currentTurn > _maxTurns)
        {
            reason = $"Cycle step limit of {_maxTurns} turns exceeded. Finalising cycle.";
            return false;
        }

        reason = null;
        return true;
    }

    public bool TryIntercept(ToolCall toolCall, [NotNullWhen(true)] out ToolExecutionResult? interceptedResult)
    {
        ArgumentNullException.ThrowIfNull(toolCall);

        if (_previousTurnFailedCalls.Contains((toolCall.Name, toolCall.Arguments)))
        {
            string loopMessage = $"LOOP DETECTED: You executed the tool '{toolCall.Name}' with identical arguments that previously failed. You MUST adjust your approach: read the file first to check exact content, change arguments, or try a different tool.";

            interceptedResult = new ToolExecutionResult(
                Success: false,
                DisplayMessage: "Loop detected: tool call repeated identical failing arguments.",
                Result: string.Empty,
                Error: loopMessage,
                ToolId: toolCall.ToolId,
                ToolName: toolCall.Name);

            return true;
        }

        interceptedResult = null;
        return false;
    }

    public void RecordResults(IReadOnlyList<ToolCall> toolCalls, IReadOnlyList<ToolExecutionResult> results)
    {
        ArgumentNullException.ThrowIfNull(toolCalls);
        ArgumentNullException.ThrowIfNull(results);

        _previousTurnFailedCalls.Clear();

        int resultCount = Math.Min(toolCalls.Count, results.Count);

        for (int resultIndex = 0; resultIndex < resultCount; resultIndex++)
        {
            if (!results[resultIndex].Success)
            {
                _previousTurnFailedCalls.Add((toolCalls[resultIndex].Name, toolCalls[resultIndex].Arguments));
            }
        }
    }

    public void Reset()
    {
        _currentTurn = 0;
        _previousTurnFailedCalls.Clear();
    }
}
