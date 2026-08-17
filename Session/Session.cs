namespace Wayfare.Session;

using Wayfare.Session.Transformations;

public sealed class Session(ITransformationPipeline pipeline) : ISession
{
    private readonly List<HistoryNode> _history = [];
    private readonly ITransformationPipeline _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));

    public Session() : this(new TransformationPipeline(
        [new WriteShadowingRule(), new DiagnosticCollapseRule()],
        new ResourceIndex()))
    {
    }

    public SessionState State { get; private set; } = SessionState.Idle;
    public IReadOnlyList<HistoryNode> History => _history.AsReadOnly();
    public ITransformationPipeline Pipeline => _pipeline;

    public IReadOnlyList<HistoryNode> LinearTrunk =>
        _history.Count > 0 && _history[^1] is BranchContainerNode activeBranch
            ? activeBranch.Turns.AsReadOnly()
            : [];

    public void StartBranch()
    {
        _history.Add(new BranchContainerNode(string.Empty, [], BranchStatus.Active));
    }

    public void AppendTurn(SessionMessage message, IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        BranchContainerNode activeBranch = GetActiveBranch();
        TurnNode turnNode = new(message)
        {
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        activeBranch.Turns.Add(turnNode);

        // Apply transformation pipeline on a snapshot of active branch trunk
        IReadOnlyList<HistoryNode> transformedTrunk = _pipeline.Apply([.. activeBranch.Turns], turnNode);

        activeBranch.Turns.Clear();
        activeBranch.Turns.AddRange(transformedTrunk);
    }

    public void SquashBranch(string summary, BranchStatus status)
    {
        ArgumentNullException.ThrowIfNull(summary);

        BranchContainerNode lastBranchNode = GetActiveBranch();
        _history[^1] = lastBranchNode with
        {
            Summary = summary,
            Status = status
        };
    }

    public SessionMessage? GetLastMessage()
    {
        if (_history.Count == 0 || _history[^1] is not BranchContainerNode branch)
        {
            return null;
        }

        return branch.Turns.Count > 0 ? branch.Turns[^1].ToProjectedMessage() : null;
    }

    public SessionProgress GetProgress()
    {
        List<string> milestones = [.. _history.OfType<BranchContainerNode>().Select(branch => branch.Summary)];
        return new SessionProgress(milestones.AsReadOnly());
    }

    public void TransitionTo(SessionState newState)
    {
        State = newState;
    }

    private BranchContainerNode GetActiveBranch()
    {
        if (_history.Count == 0 || _history[^1] is not BranchContainerNode branchNode)
        {
            throw new InvalidOperationException("Session operation requires an active BranchContainerNode in history.");
        }

        return branchNode;
    }
}
