namespace Wayfare.Core.Models;

#region Enums & Tool Invocation

/// <summary>
/// Indicates why the language model completed token generation.
/// </summary>
public enum AgentFinishReason
{
    Stop,
    Length,
    ContentFilter,
    ToolCalls,
}

/// <summary>
/// Represents a requested tool invocation from the language model.
/// </summary>
public record ToolCall(string ToolId, string Name, string Arguments);

#endregion

#region Completion & Streaming Payloads

/// <summary>
/// Result from a non-streaming chat completion request.
/// </summary>
public record ChatCompletionResult(
    string Content,
    AgentFinishReason FinishReason
);

/// <summary>
/// Incremental chunk for a streaming tool call invocation.
/// </summary>
public record StreamingToolCallChunk(
    int Index,
    string ToolId = "",
    string FunctionName = "",
    string FunctionArgumentsUpdate = ""
);

/// <summary>
/// Incremental streaming update from a chat completion stream.
/// </summary>
public record StreamingChatUpdate(
    string? ContentUpdate = null,
    StreamingToolCallChunk? ToolCallUpdate = null,
    AgentFinishReason? FinishReason = null
);

#endregion
