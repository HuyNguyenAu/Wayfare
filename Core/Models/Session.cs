using System.Text.Json.Serialization;

namespace Wayfare.Core.Models;

#region Session Enums & DTOs

public enum SessionState
{
    Idle,
    Thinking,
    Acting,
    Observing,
    Done,
}

public enum BranchStatus
{
    Active,
    Completed,
    Abandoned,
}

public record SessionProgress(string Objective, IReadOnlyList<string> Milestones);

public sealed record ToolExecutionResult(
    bool Success,
    string DisplayMessage,
    string Result,
    string Error,
    [property: JsonIgnore] Exception? Exception = null,
    string ToolId = "",
    string ToolName = "");

#endregion

#region Session Implementation

public class Session : ISession
{
    private readonly List<HistoryNode> _history = [];

    public SessionState State { get; private set; } = SessionState.Idle;
    public string Intent { get; private set; } = string.Empty;
    public IReadOnlyList<HistoryNode> History => _history.AsReadOnly();

    public void StartBranch()
    {
        _history.Add(new BranchNode(string.Empty, [], BranchStatus.Active));
    }

    public void AppendTurn(SessionMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        GetActiveBranch().Turns.Add(new TurnNode(message));
    }

    public void SquashBranch(string summary, BranchStatus status)
    {
        ArgumentNullException.ThrowIfNull(summary);

        BranchNode lastBranchNode = GetActiveBranch();
        _history[^1] = lastBranchNode with
        {
            Summary = summary,
            Status = status
        };
    }

    public void UpdateIntent(string intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        Intent = intent;
    }

    public SessionMessage GetLastMessage()
    {
        return GetActiveBranch().Turns[^1].Message;
    }

    public SessionProgress GetProgress()
    {
        if (_history.Count == 0)
        {
            return new SessionProgress(string.Empty, []);
        }

        string objective = string.IsNullOrWhiteSpace(Intent) ? "Awaiting directive..." : Intent;
        List<string> milestones = [.. _history.OfType<BranchNode>().Select(branch => branch.Summary)];

        return new SessionProgress(objective, milestones.AsReadOnly());
    }

    public void TransitionTo(SessionState newState)
    {
        State = newState;
    }

    private BranchNode GetActiveBranch()
    {
        if (_history.Count == 0 || _history[^1] is not BranchNode branchNode)
        {
            throw new InvalidOperationException("Session operation requires an active BranchNode in history.");
        }

        return branchNode;
    }
}

#endregion
