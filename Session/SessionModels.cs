namespace Wayfare.Session;

using System.Text.Json.Serialization;
using Wayfare.Infrastructure.AI;
using Wayfare.Tools;

#region Session State & Progress

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

public record SessionProgress(string Objective, IReadOnlyList<string> Milestones);

#endregion

#region AST History Nodes

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BranchNode), "branch")]
[JsonDerivedType(typeof(TurnNode), "turn")]
public abstract record HistoryNode
{
    public string Id { get; init; } = Guid.CreateVersion7().ToString();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public record BranchNode(
    string Summary,
    List<TurnNode> Turns,
    BranchStatus Status) : HistoryNode;

public record TurnNode(SessionMessage Message) : HistoryNode;

#endregion

#region Session Messages

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(SystemMessage), "system")]
[JsonDerivedType(typeof(UserMessage), "user")]
[JsonDerivedType(typeof(AssistantMessage), "assistant")]
[JsonDerivedType(typeof(ToolCallMessage), "tool_call")]
[JsonDerivedType(typeof(ToolResultMessage), "tool_result")]
public abstract record SessionMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record SystemMessage(string Content) : SessionMessage;

public record UserMessage(string Content) : SessionMessage;

public record AssistantMessage(string Content) : SessionMessage;

public record ToolCallMessage(IReadOnlyList<ToolCall> ToolCalls) : SessionMessage;

public record ToolResultMessage(IReadOnlyList<ToolExecutionResult> Results) : SessionMessage;

#endregion
