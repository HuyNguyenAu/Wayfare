namespace Wayfare.Session;

using Wayfare.Session.Transformations;

public interface ISession
{
    SessionState State { get; }
    IReadOnlyList<HistoryNode> History { get; }
    IReadOnlyList<HistoryNode> LinearTrunk { get; }
    ITransformationPipeline Pipeline { get; }

    void StartBranch();
    void AppendTurn(SessionMessage message, IReadOnlyDictionary<string, string>? metadata = null);
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
