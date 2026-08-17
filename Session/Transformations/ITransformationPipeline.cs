namespace Wayfare.Session.Transformations;

using Wayfare.Session;

public enum ResourceAccessType
{
    Read,
    Write,
}

public sealed record ResourceAccessRecord(
    string ResourceKey,
    string NodeId,
    int TrunkIndex,
    ResourceAccessType AccessType,
    DateTime Timestamp);

public sealed record TrunkTransformResult(
    bool Modified,
    IReadOnlyList<HistoryNode> TransformedTrunk);

public interface IResourceIndex
{
    void IndexNode(HistoryNode node, int trunkIndex);
    IReadOnlyList<ResourceAccessRecord> GetAccessHistory(string resourceKey);
    IReadOnlyList<ResourceAccessRecord> GetAllRecords();
    void RebuildIndex(IReadOnlyList<HistoryNode> trunk);
    void Clear();
}

public interface ITransformationRule
{
    string Name { get; }
    TrunkTransformResult Apply(IReadOnlyList<HistoryNode> currentTrunk, HistoryNode newTurn, IResourceIndex resourceIndex);
}

public interface ITransformationPipeline
{
    IReadOnlyList<ITransformationRule> Rules { get; }
    IResourceIndex ResourceIndex { get; }
    IReadOnlyList<HistoryNode> Apply(IReadOnlyList<HistoryNode> trunk, HistoryNode newTurn);
}
