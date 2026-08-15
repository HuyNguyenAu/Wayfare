namespace Wayfare.Core.Models.Messages;

public record ToolResultMessage(IReadOnlyList<ToolExecutionResult> Results) : SessionMessage;
