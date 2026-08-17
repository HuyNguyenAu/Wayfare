namespace Wayfare.Core.Abstractions;

using Wayfare.Core.Models;
using Wayfare.Core.Models.Ast;
using Wayfare.Core.Models.Messages;

public interface ISession
{
    SessionState State { get; }
    string Intent { get; }
    IReadOnlyList<HistoryNode> History { get; }

    void StartBranch();
    void AppendTurn(SessionMessage message);
    void SquashBranch(string summary, BranchStatus status);
    void UpdateIntent(string intent);
    SessionMessage GetLastMessage();
    SessionProgress GetProgress();
    void TransitionTo(SessionState newState);
}
