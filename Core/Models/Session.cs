namespace Wayfare.Core.Models;

using Wayfare.Core.Abstractions;
using Wayfare.Core.Models.Ast;
using Wayfare.Core.Models.Messages;

internal class Session : ISession
{
    private readonly List<HistoryNode> _history = [];

    public SessionState State { get; private set; } = SessionState.Idle;
    public IReadOnlyList<HistoryNode> History => _history.AsReadOnly();

    public void StartBranch()
    {
        _history.Add(new BranchNode(string.Empty, []));
    }

    public void AppendTurn(SessionMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        GetActiveBranch().Turns.Add(new TurnNode(message));
    }

    public void SquashBranch(string summary)
    {
        BranchNode lastBranchNode = GetActiveBranch();
        _history[^1] = lastBranchNode with { Summary = summary };
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

        string objective = _history[0] is BranchNode firstBranch && firstBranch.Turns.Count > 0 && firstBranch.Turns[0].Message is UserMessage userMessage
            ? userMessage.Content
            : string.Empty;

        List<string> milestones = [.. _history.OfType<BranchNode>().Select(b => b.Summary)];

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
