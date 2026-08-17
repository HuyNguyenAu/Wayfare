namespace Wayfare.Infrastructure.AI;

using System.Text.Json;
using Microsoft.Extensions.AI;
using Wayfare.Session;
using Wayfare.Tools;

public static class SessionMessageMapper
{
    public static List<ChatMessage> ToChatMessages(IReadOnlyList<SessionMessage> sessionMessages)
    {
        ArgumentNullException.ThrowIfNull(sessionMessages);

        List<ChatMessage> chatMessages = [];

        for (int messageIndex = 0; messageIndex < sessionMessages.Count; messageIndex++)
        {
            SessionMessage sessionMessage = sessionMessages[messageIndex];

            switch (sessionMessage)
            {
                case SystemMessage systemMessage:
                    if (!string.IsNullOrWhiteSpace(systemMessage.Content))
                    {
                        chatMessages.Add(new ChatMessage(ChatRole.System, systemMessage.Content.Trim()));
                    }
                    break;

                case UserMessage userMessage:
                    if (!string.IsNullOrWhiteSpace(userMessage.Content))
                    {
                        chatMessages.Add(new ChatMessage(ChatRole.User, userMessage.Content.Trim()));
                    }
                    break;

                case AssistantMessage assistantMessage:
                    // If followed by a ToolCallMessage, merge into a single assistant turn.
                    if (messageIndex + 1 < sessionMessages.Count && sessionMessages[messageIndex + 1] is ToolCallMessage nextToolCallMessage)
                    {
                        List<AIContent> contents = [];

                        if (!string.IsNullOrWhiteSpace(assistantMessage.Content))
                        {
                            contents.Add(new TextContent(assistantMessage.Content.Trim()));
                        }

                        foreach (ToolCall toolCall in nextToolCallMessage.ToolCalls)
                        {
                            contents.Add(CreateFunctionCallContent(toolCall));
                        }

                        chatMessages.Add(new ChatMessage(ChatRole.Assistant, contents));
                        messageIndex++; // Consume the merged ToolCallMessage.
                    }
                    else if (!string.IsNullOrWhiteSpace(assistantMessage.Content))
                    {
                        if (chatMessages.Count > 0 && chatMessages[^1].Role == ChatRole.Assistant)
                        {
                            chatMessages[^1].Contents.Add(new TextContent(assistantMessage.Content.Trim()));
                        }
                        else
                        {
                            chatMessages.Add(new ChatMessage(ChatRole.Assistant, assistantMessage.Content.Trim()));
                        }
                    }
                    break;

                case ToolCallMessage toolCallMessage:
                    List<AIContent> callContents = [.. toolCallMessage.ToolCalls.Select(CreateFunctionCallContent)];

                    if (chatMessages.Count > 0 && chatMessages[^1].Role == ChatRole.Assistant)
                    {
                        foreach (AIContent content in callContents)
                        {
                            chatMessages[^1].Contents.Add(content);
                        }
                    }
                    else
                    {
                        chatMessages.Add(new ChatMessage(ChatRole.Assistant, callContents));
                    }
                    break;

                case ToolResultMessage toolResultMessage:
                    foreach (ToolExecutionResult result in toolResultMessage.Results)
                    {
                        string resultText = FormatToolResult(result);
                        chatMessages.Add(new ChatMessage(ChatRole.Tool, [new FunctionResultContent(result.ToolId, resultText)]));
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unknown message type: {sessionMessage.GetType().Name}");
            }
        }

        return chatMessages;
    }

    private static string FormatToolResult(ToolExecutionResult result)
    {
        if (result.Success)
        {
            return string.IsNullOrWhiteSpace(result.Result)
                ? $"Tool '{result.ToolName}' completed successfully with no output."
                : result.Result.Trim();
        }

        if (string.IsNullOrWhiteSpace(result.Error))
        {
            return $"ERROR: Tool '{result.ToolName}' failed without an explicit error message. Verify tool parameters and check file paths with 'list' or 'find' before retrying.";
        }

        return result.Error.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase)
            ? result.Error.Trim()
            : $"ERROR: {result.Error.Trim()}";
    }

    private static FunctionCallContent CreateFunctionCallContent(ToolCall toolCall)
    {
        IDictionary<string, object?>? arguments = null;

        if (!string.IsNullOrWhiteSpace(toolCall.Arguments))
        {
            try
            {
                arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(toolCall.Arguments);
            }
            catch
            {
                // Fallback for plain text / non-json.
            }
        }

        return new FunctionCallContent(toolCall.ToolId, toolCall.Name, arguments);
    }
}
