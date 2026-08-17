using System.Runtime.CompilerServices;
using OpenAI.Chat;
using Wayfare.Core;
using Wayfare.Core.Models;

namespace Wayfare.Infrastructure.Clients;

public class OpenAIClient(ChatClient client) : IChatClient
{
    public async IAsyncEnumerable<StreamingChatUpdate> StreamChatAsync(
        IReadOnlyList<SessionMessage> sessionMessages,
        IReadOnlyList<ITool> tools,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ChatCompletionOptions options = CreateChatCompletionOptions(tools);
        List<ChatMessage> messages = MapSessionMessagesToChatMessages(sessionMessages);

        await foreach (StreamingChatCompletionUpdate update in client.CompleteChatStreamingAsync(messages, options, cancellationToken))
        {
            foreach (ChatMessageContentPart part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    yield return new StreamingChatUpdate(ContentUpdate: part.Text);
                }
            }

            foreach (StreamingChatToolCallUpdate toolCallUpdate in update.ToolCallUpdates)
            {
                yield return new StreamingChatUpdate(
                    ToolCallUpdate: new StreamingToolCallChunk(
                        toolCallUpdate.Index,
                        toolCallUpdate.ToolCallId ?? string.Empty,
                        toolCallUpdate.FunctionName ?? string.Empty,
                        toolCallUpdate.FunctionArgumentsUpdate?.ToString() ?? string.Empty
                    )
                );
            }

            if (update.FinishReason is not null)
            {
                yield return new StreamingChatUpdate(FinishReason: MapFinishReason(update.FinishReason.Value));
            }
        }
    }

    public async Task<ChatCompletionResult> CompleteChatAsync(
        IReadOnlyList<SessionMessage> sessionMessages,
        IReadOnlyList<ITool> tools,
        CancellationToken cancellationToken)
    {
        ChatCompletionOptions options = CreateChatCompletionOptions(tools);
        List<ChatMessage> messages = MapSessionMessagesToChatMessages(sessionMessages);

        ChatCompletion chatCompletion = await client.CompleteChatAsync(messages, options, cancellationToken);
        string content = chatCompletion.Content.Count > 0 ? chatCompletion.Content[0].Text ?? string.Empty : string.Empty;

        return new ChatCompletionResult(
            Content: content,
            FinishReason: MapFinishReason(chatCompletion.FinishReason)
        );
    }

    private static ChatCompletionOptions CreateChatCompletionOptions(IReadOnlyList<ITool> tools)
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

    private static List<ChatMessage> MapSessionMessagesToChatMessages(IReadOnlyList<SessionMessage> sessionMessages)
    {
        List<ChatMessage> chatMessages = [];

        foreach (SessionMessage sessionMessage in sessionMessages)
        {
            List<ChatMessage> mappedMessages = sessionMessage switch
            {
                SystemMessage message => [new SystemChatMessage(message.Content)],
                UserMessage message => [new UserChatMessage(message.Content)],
                AssistantMessage message => [new AssistantChatMessage(message.Content)],
                ToolCallMessage message => [new AssistantChatMessage(message.ToolCalls.Select(toolCall => ChatToolCall.CreateFunctionToolCall(toolCall.ToolId, toolCall.Name, BinaryData.FromString(toolCall.Arguments))))],
                ToolResultMessage message => [.. message.Results.Select(toolResult => new ToolChatMessage(toolResult.ToolId, toolResult.Result))],
                _ => throw new InvalidOperationException($"Unknown message type: {sessionMessage.GetType().Name}")
            };

            chatMessages.AddRange(mappedMessages);
        }

        return chatMessages;
    }

    private static AgentFinishReason MapFinishReason(OpenAI.Chat.ChatFinishReason finishReason)
    {
        return finishReason switch
        {
            ChatFinishReason.Stop => AgentFinishReason.Stop,
            ChatFinishReason.Length => AgentFinishReason.Length,
            ChatFinishReason.ContentFilter => AgentFinishReason.ContentFilter,
            ChatFinishReason.ToolCalls => AgentFinishReason.ToolCalls,
            _ => throw new InvalidOperationException($"Unexpected finish reason: {finishReason}")
        };
    }
}
