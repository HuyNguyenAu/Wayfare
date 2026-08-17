namespace Wayfare.Session;

using System.Text;
using System.Text.Json.Serialization;
using Wayfare.Infrastructure.AI;
using Wayfare.Tools;

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

public sealed record SessionProgress(IReadOnlyList<string> Milestones);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BranchContainerNode), "branch_container")]
[JsonDerivedType(typeof(TurnNode), "turn")]
[JsonDerivedType(typeof(SupersededStateNode), "superseded_state")]
[JsonDerivedType(typeof(CollapsedExplorationNode), "collapsed_exploration")]
public abstract record HistoryNode
{
    public string Id { get; init; } = Guid.CreateVersion7().ToString();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    public abstract SessionMessage ToProjectedMessage();
    [JsonIgnore] public abstract IEnumerable<HistoryNode> Children { get; }
    public virtual TurnNode? GetRootTurn() => null;
}

public sealed record TurnNode(SessionMessage Message) : HistoryNode
{
    public override SessionMessage ToProjectedMessage() => Message;
    [JsonIgnore] public override IEnumerable<HistoryNode> Children => [];
    public override TurnNode? GetRootTurn() => this;
}

public sealed record SupersededStateNode(
    HistoryNode TargetNode,
    SessionMessage RevisedMessage,
    string ResourceKey,
    string SupersededByNodeId,
    string Reason) : HistoryNode
{
    public override SessionMessage ToProjectedMessage() => RevisedMessage;
    [JsonIgnore] public override IEnumerable<HistoryNode> Children => [TargetNode];
    public override TurnNode? GetRootTurn() => TargetNode.GetRootTurn();
}

public sealed record CollapsedExplorationNode(
    IReadOnlyList<HistoryNode> CollapsedNodes,
    SessionMessage CollapsedMessage,
    string Summary) : HistoryNode
{
    public override SessionMessage ToProjectedMessage() => CollapsedMessage;
    public override IEnumerable<HistoryNode> Children => CollapsedNodes;
}

public sealed record BranchContainerNode(
    string Summary,
    List<HistoryNode> Turns,
    BranchStatus Status) : HistoryNode
{
    public override SessionMessage ToProjectedMessage() =>
        !string.IsNullOrWhiteSpace(Summary)
            ? new SystemMessage($"<milestone_summary id=\"{Id}\">\n{Summary}\n</milestone_summary>")
            : new SystemMessage($"<active_branch id=\"{Id}\"/>");

    public override IEnumerable<HistoryNode> Children => Turns;
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(SystemMessage), "system")]
[JsonDerivedType(typeof(UserMessage), "user")]
[JsonDerivedType(typeof(AssistantMessage), "assistant")]
[JsonDerivedType(typeof(ToolCallMessage), "tool_call")]
[JsonDerivedType(typeof(ToolResultMessage), "tool_result")]
public abstract record SessionMessage
{
    public abstract void AppendTraceLines(StringBuilder builder);
}

public sealed record SystemMessage(string Content) : SessionMessage
{
    public override void AppendTraceLines(StringBuilder builder)
    {
        builder.AppendLine($"System: {Content}");
    }
}

public sealed record UserMessage(string Content) : SessionMessage
{
    public override void AppendTraceLines(StringBuilder builder)
    {
        builder.AppendLine($"User: {Content}");
    }
}

public sealed record AssistantMessage(string Content) : SessionMessage
{
    public override void AppendTraceLines(StringBuilder builder)
    {
        builder.AppendLine($"Assistant: {Content}");
    }
}

public sealed record ToolCallMessage(IReadOnlyList<ToolCall> ToolCalls) : SessionMessage
{
    public override void AppendTraceLines(StringBuilder builder)
    {
        foreach (ToolCall toolCall in ToolCalls)
        {
            builder.AppendLine($"Tool Call: {toolCall.Name}({toolCall.Arguments})");
        }
    }
}

public sealed record ToolResultMessage(IReadOnlyList<ToolExecutionResult> Results) : SessionMessage
{
    public override void AppendTraceLines(StringBuilder builder)
    {
        foreach (ToolExecutionResult result in Results)
        {
            string content = result.Success ? result.Result : result.Error;
            builder.AppendLine($"Tool Result ({result.ToolName}): {content}");
        }
    }
}

public static class HistoryNodeExtensions
{
    public static IReadOnlyList<SessionMessage> ToProjectedMessages(this IEnumerable<HistoryNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        return [.. nodes.Select(node => node.ToProjectedMessage())];
    }
}
