using System.Text;
using OpenAI.Chat;

namespace WayFare;

internal enum FinishReason
{
    Stop,
    Length,
    ContentFilter,
    ToolCalls,
}

internal record ToolCall(string ToolId, string Name, string Arguments);
internal record ChatResponse(string Content, List<ToolCall> ToolCalls, FinishReason FinishReason);

internal interface IChatClient
{
    Task<ChatResponse> ChatAsync(ISessionMessage[] messages, CancellationToken cancellationToken);
}

internal class OpenAIClient(ChatClient client) : IChatClient
{
    private readonly ChatCompletionOptions options = new()
    {
        AllowParallelToolCalls = false
    };

    public async Task<ChatResponse> ChatAsync(ISessionMessage[] sessionMessages, CancellationToken cancellationToken)
    {
        ChatFinishReason? chatFinishReason = null;
        StringBuilder assembledContent = new();
        Dictionary<int, ToolCallBuilder> toolCalls = [];

        List<ChatMessage> messages = [];

        foreach (ISessionMessage sessionMessage in sessionMessages)
        {
            ChatMessage chatMessage = sessionMessage switch
            {
                SystemMessage message => new SystemChatMessage(message.Content),
                UserMessage message => new UserChatMessage(message.Content),
                AssistantMessage message => new AssistantChatMessage(message.Content),
                ToolCallMessage message => new AssistantChatMessage([ChatToolCall.CreateFunctionToolCall(message.ToolId, message.ToolName, BinaryData.FromString(message.Arguments))]),
                ToolResultMessage message => new ToolChatMessage(message.ToolId, message.Result),
                _ => throw new InvalidOperationException($"Unknown message type: {sessionMessage.GetType().Name}")
            };
            messages.Add(chatMessage);
        }

        await foreach (StreamingChatCompletionUpdate chatCompletionUpdate in client.CompleteChatStreamingAsync(messages, options, cancellationToken))
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

            foreach (StreamingChatToolCallUpdate toolCallUpdate in chatCompletionUpdate.ToolCallUpdates)
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

            if (chatCompletionUpdate.FinishReason is not null)
            {
                chatFinishReason = chatCompletionUpdate.FinishReason;
            }
        }

        List<ToolCall> assembledToolCalls = [];

        foreach (ToolCallBuilder toolCall in toolCalls.Values)
        {
            assembledToolCalls.Add(new ToolCall(toolCall.ToolId.ToString(), toolCall.Name.ToString(), toolCall.Args.ToString()));
        }

        if (chatFinishReason is null)
        {
            throw new InvalidOperationException("Chat completion did not provide a finish reason.");
        }

        FinishReason finishReason = chatFinishReason switch
        {
            ChatFinishReason.Stop => FinishReason.Stop,
            ChatFinishReason.Length => FinishReason.Length,
            ChatFinishReason.ContentFilter => FinishReason.ContentFilter,
            ChatFinishReason.ToolCalls => FinishReason.ToolCalls,
            _ => throw new InvalidOperationException($"Unexpected finish reason: {chatFinishReason}")
        };

        return new(assembledContent.ToString(), assembledToolCalls, finishReason);
    }

    private sealed class ToolCallBuilder
    {
        public StringBuilder ToolId { get; } = new();
        public StringBuilder Name { get; } = new();
        public StringBuilder Args { get; } = new();
    }
}