namespace Wayfare.Session;

public interface ISession
{
    SessionState State { get; }
    IReadOnlyList<HistoryNode> History { get; }

    void StartBranch();
    void AppendTurn(SessionMessage message);
    void SquashBranch(string summary, BranchStatus status);
    SessionMessage? GetLastMessage();
    SessionProgress GetProgress();
    void TransitionTo(SessionState newState);
}

public interface ISessionStore : IAsyncDisposable
{
    ISession Session { get; }
    Task SaveAsync(CancellationToken cancellationToken);
}
