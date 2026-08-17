namespace Wayfare.Infrastructure.AI;

#region Enums & Tool Invocation

public enum AgentFinishReason
{
    Stop,
    Length,
    ContentFilter,
    ToolCalls,
}

public record ToolCall(string ToolId, string Name, string Arguments);

#endregion

#region Completion & Streaming Payloads

public record ChatCompletionResult(
    string Content,
    AgentFinishReason FinishReason
);

public record StreamingToolCallChunk(
    int Index,
    string ToolId = "",
    string FunctionName = "",
    string FunctionArgumentsUpdate = ""
);

public record StreamingChatUpdate(
    string? ContentUpdate = null,
    StreamingToolCallChunk? ToolCallUpdate = null,
    AgentFinishReason? FinishReason = null
);

#endregion
