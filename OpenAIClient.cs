using System.Text;
using OpenAI.Chat;
using WayFare.Tools;

namespace WayFare;

internal enum ChatFinishReason
{
    Stop,
    Length,
    ContentFilter,
    ToolCalls,
}

internal record ChatToolCall(string ToolId, string Name, string Arguments);
internal record ChatResponse(string Content, ChatToolCall[] ToolCalls, ChatFinishReason FinishReason);

internal interface IChatClient
{
    Task<ChatResponse> ChatAsync(ISessionMessage[] messages, ITool[] tools, CancellationToken cancellationToken);
}

internal class OpenAIClient(ChatClient client) : IChatClient
{
    public async Task<ChatResponse> ChatAsync(ISessionMessage[] sessionMessages, ITool[] tools, CancellationToken cancellationToken)
    {
        OpenAI.Chat.ChatFinishReason? chatFinishReason = null;
        StringBuilder assembledContent = new();
        Dictionary<int, ToolCallBuilder> toolCalls = [];

        await foreach (StreamingChatCompletionUpdate chatCompletionUpdate in client.CompleteChatStreamingAsync(MapSessionMessagesToChatMessages(sessionMessages), CreateChatCompletionOptions(tools), cancellationToken))
        {
            foreach (ChatMessageContentPart part in chatCompletionUpdate.ContentUpdate)
            {
                if (string.IsNullOrEmpty(part.Text))
                {
                    continue;
                }

                assembledContent.Append(part.Text);
                Console.Write(part.Text);
            }

            HandleToolCallUpdate([.. chatCompletionUpdate.ToolCallUpdates], toolCalls);

            if (chatCompletionUpdate.FinishReason is not null)
            {
                chatFinishReason = chatCompletionUpdate.FinishReason;
            }
        }

        ChatToolCall[] assembledToolCalls = [.. toolCalls.Values.Select(toolCall => new ChatToolCall(toolCall.ToolId.ToString(), toolCall.Name.ToString(), toolCall.Args.ToString()))];

        if (chatFinishReason is null)
        {
            throw new InvalidOperationException("Chat completion did not provide a finish reason.");
        }

        return new(assembledContent.ToString(), [.. assembledToolCalls], MapFinishReason(chatFinishReason));
    }

    private static ChatCompletionOptions CreateChatCompletionOptions(ITool[] tools)
    {
        ChatCompletionOptions options = new()
        {
            AllowParallelToolCalls = false,
        };
        
        foreach (ITool tool in tools)
        {
            options.Tools.Add(ChatTool.CreateFunctionTool(tool.Name, tool.Description));
        }

        return options;
    }

    private static ChatMessage[] MapSessionMessagesToChatMessages(ISessionMessage[] sessionMessages)
    {
        List<ChatMessage> chatMessages = [];

        foreach (ISessionMessage sessionMessage in sessionMessages)
        {
            ChatMessage[] mappedMessages = sessionMessage switch
            {
                SystemMessage message => [new SystemChatMessage(message.Content)],
                UserMessage message => [new UserChatMessage(message.Content)],
                AssistantMessage message => [new AssistantChatMessage(message.Content)],
                ToolCallMessage message => [new AssistantChatMessage(message.ToolCalls.Select(toolCall => OpenAI.Chat.ChatToolCall.CreateFunctionToolCall(toolCall.ToolId, toolCall.Name, BinaryData.FromString(toolCall.Arguments))))],
                ToolResultMessage message => [.. message.ToolResults.Select(toolResult => new ToolChatMessage(toolResult.ToolId, toolResult.Result))],
                _ => throw new InvalidOperationException($"Unknown message type: {sessionMessage.GetType().Name}")
            };

            chatMessages.AddRange(mappedMessages);
        }

        return [.. chatMessages];
    }

    private static ChatFinishReason MapFinishReason(OpenAI.Chat.ChatFinishReason? finishReason)
    {
        return finishReason switch
        {
            OpenAI.Chat.ChatFinishReason.Stop => ChatFinishReason.Stop,
            OpenAI.Chat.ChatFinishReason.Length => ChatFinishReason.Length,
            OpenAI.Chat.ChatFinishReason.ContentFilter => ChatFinishReason.ContentFilter,
            OpenAI.Chat.ChatFinishReason.ToolCalls => ChatFinishReason.ToolCalls,
            null => throw new InvalidOperationException("Chat completion did not provide a finish reason."),
            _ => throw new InvalidOperationException($"Unexpected finish reason: {finishReason}")
        };
    }

    private static void HandleToolCallUpdate(StreamingChatToolCallUpdate[] toolCallUpdates, Dictionary<int, ToolCallBuilder> toolCalls)
    {
        foreach (StreamingChatToolCallUpdate toolCallUpdate in toolCallUpdates)
        {
            if (!toolCalls.TryGetValue(toolCallUpdate.Index, out ToolCallBuilder? toolCall))
            {
                toolCall = new ToolCallBuilder();
                toolCalls[toolCallUpdate.Index] = toolCall;
            }

            if (!string.IsNullOrEmpty(toolCallUpdate.ToolCallId))
            {
                toolCall.ToolId.Append(toolCallUpdate.ToolCallId);
            }

            if (!string.IsNullOrEmpty(toolCallUpdate.FunctionName))
            {
                toolCall.Name.Append(toolCallUpdate.FunctionName);
            }

            toolCall.Args.Append(toolCallUpdate.FunctionArgumentsUpdate);
        }
    }

    private sealed class ToolCallBuilder
    {
        public StringBuilder ToolId { get; } = new();
        public StringBuilder Name { get; } = new();
        public StringBuilder Args { get; } = new();
    }
}