namespace Wayfare.Core.Models;

public record StreamingToolCallChunk(int Index, string ToolId = "", string FunctionName = "", string FunctionArgumentsUpdate = "");

public record StreamingChatUpdate(
    string? ContentUpdate = null,
    StreamingToolCallChunk? ToolCallUpdate = null,
    AgentFinishReason? FinishReason = null
);
