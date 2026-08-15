using System.Text.Json.Serialization;

namespace Wayfare.Core.Models.Messages;

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
