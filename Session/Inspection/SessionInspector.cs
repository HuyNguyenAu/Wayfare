namespace Wayfare.Session.Inspection;

using Wayfare.Session;

public sealed record SessionAuditReport(
    int TotalRawTurns,
    int ProjectedTurns,
    int ShadowedNodesCount,
    int CollapsedGroupsCount,
    int TotalExploratoryNodesCollapsed,
    double CompressionRatio);

public interface ISessionInspector
{
    IReadOnlyList<HistoryNode> GetTransformationHistory(HistoryNode node);
    TurnNode? GetRootTurn(HistoryNode node);
    IReadOnlyList<TurnNode> GetRawNodes(HistoryNode node);
    IReadOnlyList<TurnNode> GetRawHistory(ISession session);
    SessionAuditReport GenerateAuditReport(ISession session);
}

public sealed class SessionInspector : ISessionInspector
{
    public IReadOnlyList<HistoryNode> GetTransformationHistory(HistoryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        List<HistoryNode> chain = [node];
        HistoryNode current = node;

        while (current is SupersededStateNode superseded)
        {
            current = superseded.TargetNode;
            chain.Add(current);
        }

        return chain.AsReadOnly();
    }

    public TurnNode? GetRootTurn(HistoryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node.GetRootTurn();
    }

    public IReadOnlyList<TurnNode> GetRawNodes(HistoryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        List<TurnNode> rawTurns = [];
        CollectRawTurns(node, rawTurns);
        return rawTurns.AsReadOnly();
    }

    public IReadOnlyList<TurnNode> GetRawHistory(ISession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        List<TurnNode> rawTurns = [];

        foreach (HistoryNode root in session.History)
        {
            CollectRawTurns(root, rawTurns);
        }

        return rawTurns.AsReadOnly();
    }

    public SessionAuditReport GenerateAuditReport(ISession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        IReadOnlyList<TurnNode> rawHistory = GetRawHistory(session);
        IReadOnlyList<HistoryNode> trunk = session.LinearTrunk;

        int totalRawTurns = rawHistory.Count;
        int projectedTurns = trunk.Count;

        int shadowedCount = 0;
        int collapsedGroupsCount = 0;
        int totalExploratoryCollapsed = 0;

        foreach (HistoryNode node in trunk)
        {
            if (node is SupersededStateNode)
            {
                shadowedCount++;
            }
            else if (node is CollapsedExplorationNode collapsed)
            {
                collapsedGroupsCount++;
                totalExploratoryCollapsed += collapsed.CollapsedNodes.Count;
            }
        }

        double compressionRatio = totalRawTurns > 0
            ? 1.0 * projectedTurns / totalRawTurns
            : 1.0;

        return new SessionAuditReport(
            TotalRawTurns: totalRawTurns,
            ProjectedTurns: projectedTurns,
            ShadowedNodesCount: shadowedCount,
            CollapsedGroupsCount: collapsedGroupsCount,
            TotalExploratoryNodesCollapsed: totalExploratoryCollapsed,
            CompressionRatio: Math.Round(compressionRatio, 2));
    }

    private static void CollectRawTurns(HistoryNode node, List<TurnNode> results)
    {
        if (node is TurnNode turn)
        {
            results.Add(turn);
        }
        else
        {
            foreach (HistoryNode child in node.Children)
            {
                CollectRawTurns(child, results);
            }
        }
    }
}
