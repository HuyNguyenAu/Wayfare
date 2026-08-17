namespace Wayfare.Session;

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

public sealed record SessionProgress(string Objective, IReadOnlyList<string> Milestones);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BranchNode), "branch")]
[JsonDerivedType(typeof(TurnNode), "turn")]
public abstract record HistoryNode
{
    public string Id { get; init; } = Guid.CreateVersion7().ToString();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public sealed record BranchNode(
    string Summary,
    List<TurnNode> Turns,
    BranchStatus Status) : HistoryNode;

public sealed record TurnNode(SessionMessage Message) : HistoryNode;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(SystemMessage), "system")]
[JsonDerivedType(typeof(UserMessage), "user")]
[JsonDerivedType(typeof(AssistantMessage), "assistant")]
[JsonDerivedType(typeof(ToolCallMessage), "tool_call")]
[JsonDerivedType(typeof(ToolResultMessage), "tool_result")]
public abstract record SessionMessage;

public sealed record SystemMessage(string Content) : SessionMessage;

public sealed record UserMessage(string Content) : SessionMessage;

public sealed record AssistantMessage(string Content) : SessionMessage;

public sealed record ToolCallMessage(IReadOnlyList<ToolCall> ToolCalls) : SessionMessage;

public sealed record ToolResultMessage(IReadOnlyList<ToolExecutionResult> Results) : SessionMessage;

public static class SessionMessageExtensions
{
    public static void AppendTraceLines(this SessionMessage message, System.Text.StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(builder);

        switch (message)
        {
            case UserMessage userMessage:
                builder.AppendLine($"User: {userMessage.Content}");
                break;
            case AssistantMessage assistantMessage:
                builder.AppendLine($"Assistant: {assistantMessage.Content}");
                break;
            case ToolCallMessage toolCallMessage:
                foreach (ToolCall toolCall in toolCallMessage.ToolCalls)
                {
                    builder.AppendLine($"Tool Call: {toolCall.Name}({toolCall.Arguments})");
                }
                break;
            case ToolResultMessage toolResultMessage:
                foreach (ToolExecutionResult result in toolResultMessage.Results)
                {
                    string content = result.Success ? result.Result : result.Error;
                    builder.AppendLine($"Tool Result ({result.ToolName}): {content}");
                }
                break;
            default:
                builder.AppendLine($"{message.GetType().Name}: {message}");
                break;
        }
    }
}
