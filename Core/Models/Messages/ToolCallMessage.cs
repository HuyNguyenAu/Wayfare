namespace Wayfare.Core.Models.Messages;

public record ToolCallMessage(IReadOnlyList<ToolCall> ToolCalls) : SessionMessage;
