using System.Text.Json.Serialization;

namespace Wayfare.Core.Models.Messages;

#region Base Session Message

/// <summary>
/// Polymorphic base record for all session messages.
/// </summary>
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

#endregion

#region Message Variants

public record SystemMessage(string Content) : SessionMessage;

public record UserMessage(string Content) : SessionMessage;

public record AssistantMessage(string Content) : SessionMessage;

public record ToolCallMessage(IReadOnlyList<ToolCall> ToolCalls) : SessionMessage;

public record ToolResultMessage(IReadOnlyList<ToolExecutionResult> Results) : SessionMessage;

#endregion
