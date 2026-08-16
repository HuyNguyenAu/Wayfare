namespace Wayfare.Core.Models;

public record ChatCompletionResult(
    string Content,
    AgentFinishReason FinishReason
);
