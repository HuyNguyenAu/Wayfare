namespace Wayfare.Infrastructure.Clients;

using System.ClientModel.Primitives;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Wayfare.Infrastructure.AI;

public class OpenAIClient : DelegatingChatClient
{
    public OpenAIClient(IChatClient innerClient) : base(innerClient)
    {
        ArgumentNullException.ThrowIfNull(innerClient);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chatMessages);

        await foreach (ChatResponseUpdate update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            string? reasoning = ExtractReasoningFromUpdate(update);

            if (!string.IsNullOrEmpty(reasoning))
            {
                update.Contents.Add(new ReasoningContent(reasoning));
            }

            yield return update;
        }
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chatMessages);

        ChatResponse response = await base.GetResponseAsync(chatMessages, options, cancellationToken);
        string? reasoning = ExtractReasoningFromResponse(response);

        if (!string.IsNullOrEmpty(reasoning) && response.Messages.Count > 0)
        {
            response.Messages[^1].Contents.Add(new ReasoningContent(reasoning));
        }

        return response;
    }

    internal static string? ExtractReasoningFromUpdate(ChatResponseUpdate update)
    {
        if (update.RawRepresentation is OpenAI.Chat.StreamingChatCompletionUpdate openAIUpdate)
        {
            return ExtractReasoningContent(openAIUpdate);
        }

        return null;
    }

    internal static string? ExtractReasoningFromResponse(ChatResponse response)
    {
        if (response.RawRepresentation is OpenAI.Chat.ChatCompletion openAICompletion)
        {
            return ExtractReasoningContent(openAICompletion);
        }

        return null;
    }

    private static readonly string[] _reasoningPropertyNames = ["reasoning_content", "reasoning", "thought"];

    internal static string? ExtractReasoningContent(OpenAI.Chat.StreamingChatCompletionUpdate update)
    {
        BinaryData serialisedData = ModelReaderWriter.Write(update);
        using JsonDocument document = JsonDocument.Parse(serialisedData);

        if (!document.RootElement.TryGetProperty("choices", out JsonElement choices) || choices.GetArrayLength() == 0)
        {
            return null;
        }

        JsonElement firstChoice = choices[0];

        if (firstChoice.TryGetProperty("delta", out JsonElement delta))
        {
            return GetReasoningProperty(delta);
        }

        return null;
    }

    internal static string? ExtractReasoningContent(OpenAI.Chat.ChatCompletion chatCompletion)
    {
        BinaryData serialisedData = ModelReaderWriter.Write(chatCompletion);
        using JsonDocument document = JsonDocument.Parse(serialisedData);

        if (!document.RootElement.TryGetProperty("choices", out JsonElement choices) || choices.GetArrayLength() == 0)
        {
            return null;
        }

        JsonElement firstChoice = choices[0];

        if (firstChoice.TryGetProperty("message", out JsonElement message))
        {
            return GetReasoningProperty(message);
        }

        return null;
    }

    private static string? GetReasoningProperty(JsonElement container)
    {
        foreach (string propertyName in _reasoningPropertyNames)
        {
            if (container.TryGetProperty(propertyName, out JsonElement element) && element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }
        }

        return null;
    }
}

public static class ReasoningChatClientExtensions
{
    public static ChatClientBuilder UseReasoningExtraction(this ChatClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Use(innerClient => new OpenAIClient(innerClient));
    }
}
