namespace Wayfare.Session.Transformations;

using Wayfare.Session;
using Wayfare.Tools;

public sealed class WriteShadowingRule : ITransformationRule
{
    public string Name => "WriteShadowing";

    public TrunkTransformResult Apply(IReadOnlyList<HistoryNode> currentTrunk, HistoryNode newTurn, IResourceIndex resourceIndex)
    {
        ArgumentNullException.ThrowIfNull(currentTrunk);
        ArgumentNullException.ThrowIfNull(newTurn);
        ArgumentNullException.ThrowIfNull(resourceIndex);

        List<ResourceAccessRecord> currentTurnWrites = [.. ResourceIndex.ExtractAccessRecords(newTurn, currentTrunk.Count, currentTrunk)
            .Where(record => record.AccessType == ResourceAccessType.Write)];

        if (currentTurnWrites.Count == 0)
        {
            return new TrunkTransformResult(false, currentTrunk);
        }

        HashSet<string> writtenResources = new(currentTurnWrites.Select(write => write.ResourceKey), StringComparer.OrdinalIgnoreCase);
        List<HistoryNode> newTrunk = [.. currentTrunk];
        bool modified = false;

        // Find the boundary of current write action (exclude new turn itself)
        int newTurnIndex = newTrunk.FindIndex(node => node.Id == newTurn.Id);
        if (newTurnIndex < 0)
        {
            newTurnIndex = newTrunk.Count - 1;
        }

        int scanEndIndex = newTurnIndex;

        if (scanEndIndex > 0 && newTurn.ToProjectedMessage() is ToolResultMessage &&
            newTrunk[scanEndIndex - 1].ToProjectedMessage() is ToolCallMessage)
        {
            scanEndIndex--;
        }

        for (int trunkIndex = 0; trunkIndex < scanEndIndex; trunkIndex++)
        {
            HistoryNode priorNode = newTrunk[trunkIndex];

            // Only wrap observation payloads (ToolResultMessage)
            if (priorNode.ToProjectedMessage() is not ToolResultMessage)
            {
                continue;
            }

            // Don't double shadow if already shadowed for this exact resource
            if (priorNode is SupersededStateNode existingSuperseded &&
                writtenResources.Contains(existingSuperseded.ResourceKey))
            {
                continue;
            }

            List<ResourceAccessRecord> priorAccesses = ResourceIndex.ExtractAccessRecords(priorNode, trunkIndex, newTrunk);

            foreach (ResourceAccessRecord access in priorAccesses)
            {
                if (writtenResources.Contains(access.ResourceKey))
                {
                    SessionMessage revisedStub = CreateSupersededStub(priorNode, access.ResourceKey, newTurn);

                    SupersededStateNode supersededNode = new(
                        TargetNode: priorNode,
                        RevisedMessage: revisedStub,
                        ResourceKey: access.ResourceKey,
                        SupersededByNodeId: newTurn.Id,
                        Reason: $"Superseded by write to '{access.ResourceKey}' at node {newTurn.Id} ({newTurn.CreatedAt:u})");

                    newTrunk[trunkIndex] = supersededNode;
                    priorNode = supersededNode;
                    modified = true;
                }
            }
        }

        return new TrunkTransformResult(modified, newTrunk.AsReadOnly());
    }

    private static SessionMessage CreateSupersededStub(HistoryNode priorNode, string resourceKey, HistoryNode newTurn)
    {
        SessionMessage message = priorNode.ToProjectedMessage();

        if (message is ToolResultMessage toolResultMessage)
        {
            List<ToolExecutionResult> stubbedResults = [];

            foreach (ToolExecutionResult result in toolResultMessage.Results)
            {
                string stubbedText = $"<observation tool=\"{result.ToolName}\" path=\"{resourceKey}\" status=\"superseded\">\n[Observation superseded by subsequent write to '{resourceKey}' at node {newTurn.Id}]\n</observation>";
                stubbedResults.Add(result with
                {
                    Result = stubbedText,
                    DisplayMessage = $"[Superseded by write to '{resourceKey}']"
                });
            }

            return new ToolResultMessage(stubbedResults.AsReadOnly());
        }

        return new SystemMessage($"[Observation for '{resourceKey}' superseded by write at node {newTurn.Id}]");
    }
}
