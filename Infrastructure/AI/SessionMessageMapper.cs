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

        foreach (SessionMessage sessionMessage in sessionMessages)
        {
            switch (sessionMessage)
            {
                case SystemMessage systemMessage:
                    chatMessages.Add(new ChatMessage(ChatRole.System, systemMessage.Content));
                    break;
                case UserMessage userMessage:
                    chatMessages.Add(new ChatMessage(ChatRole.User, userMessage.Content));
                    break;
                case AssistantMessage assistantMessage:
                    chatMessages.Add(new ChatMessage(ChatRole.Assistant, assistantMessage.Content));
                    break;
                case ToolCallMessage toolCallMessage:
                    List<AIContent> callContents = [];
                    foreach (ToolCall toolCall in toolCallMessage.ToolCalls)
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
                                // Plain text / non-json fallback
                            }
                        }
                        callContents.Add(new FunctionCallContent(toolCall.ToolId, toolCall.Name, arguments));
                    }

                    chatMessages.Add(new ChatMessage(ChatRole.Assistant, callContents));

                    break;
                case ToolResultMessage toolResultMessage:
                    foreach (ToolExecutionResult result in toolResultMessage.Results)
                    {
                        string resultText = result.Success
                            ? (string.IsNullOrWhiteSpace(result.Result) ? $"Tool '{result.ToolName}' completed successfully with no output." : result.Result)
                            : (string.IsNullOrWhiteSpace(result.Error) ? $"ERROR: Tool '{result.ToolName}' failed without an explicit error message. Verify tool parameters and check file paths with 'list' or 'find' before retrying." : $"ERROR: {result.Error}");

                        chatMessages.Add(new ChatMessage(ChatRole.Tool, [new FunctionResultContent(result.ToolId, resultText)]));
                    }

                    break;
                default:
                    throw new InvalidOperationException($"Unknown message type: {sessionMessage.GetType().Name}");
            }
        }

        return chatMessages;
    }
}
