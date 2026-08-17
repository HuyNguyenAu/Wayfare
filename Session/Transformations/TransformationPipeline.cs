namespace Wayfare.Session.Transformations;

using Wayfare.Session;

public sealed class TransformationPipeline(IReadOnlyList<ITransformationRule> rules, IResourceIndex resourceIndex) : ITransformationPipeline
{
    private readonly IReadOnlyList<ITransformationRule> _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    private readonly IResourceIndex _resourceIndex = resourceIndex ?? throw new ArgumentNullException(nameof(resourceIndex));

    public TransformationPipeline() : this(
        [new WriteShadowingRule(), new DiagnosticCollapseRule()],
        new ResourceIndex())
    {
    }

    public IReadOnlyList<ITransformationRule> Rules => _rules;
    public IResourceIndex ResourceIndex => _resourceIndex;

    public IReadOnlyList<HistoryNode> Apply(IReadOnlyList<HistoryNode> trunk, HistoryNode newTurn)
    {
        ArgumentNullException.ThrowIfNull(trunk);
        ArgumentNullException.ThrowIfNull(newTurn);

        _resourceIndex.IndexNode(newTurn, trunk.Count - 1);

        IReadOnlyList<HistoryNode> currentTrunk = trunk;

        foreach (ITransformationRule rule in _rules)
        {
            TrunkTransformResult result = rule.Apply(currentTrunk, newTurn, _resourceIndex);

            if (result.Modified)
            {
                currentTrunk = result.TransformedTrunk;
                _resourceIndex.RebuildIndex(currentTrunk);
            }
        }

        return currentTrunk;
    }
}
