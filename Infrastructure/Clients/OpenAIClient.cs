namespace Wayfare.Infrastructure.Clients;

using System.ClientModel.Primitives;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Wayfare.Infrastructure.AI;

#region Delegating Chat Client Implementation

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
        ChatResponse response = await base.GetResponseAsync(chatMessages, options, cancellationToken);
        string? reasoning = ExtractReasoningFromResponse(response);

        if (!string.IsNullOrEmpty(reasoning) && response.Messages.Count > 0)
        {
            response.Messages[^1].Contents.Add(new ReasoningContent(reasoning));
        }

        return response;
    }

    #region Internal Reasoning Extraction Helpers

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

    internal static string? ExtractReasoningContent(OpenAI.Chat.StreamingChatCompletionUpdate update)
    {
        BinaryData data = ModelReaderWriter.Write(update);
        using JsonDocument document = JsonDocument.Parse(data);

        if (!document.RootElement.TryGetProperty("choices", out JsonElement choices) || choices.GetArrayLength() == 0)
        {
            return null;
        }

        JsonElement firstChoice = choices[0];

        if (firstChoice.TryGetProperty("delta", out JsonElement delta))
        {
            if (delta.TryGetProperty("reasoning_content", out JsonElement reasoningContent) && reasoningContent.ValueKind == JsonValueKind.String)
            {
                return reasoningContent.GetString();
            }

            if (delta.TryGetProperty("reasoning", out JsonElement reasoning) && reasoning.ValueKind == JsonValueKind.String)
            {
                return reasoning.GetString();
            }

            if (delta.TryGetProperty("thought", out JsonElement thought) && thought.ValueKind == JsonValueKind.String)
            {
                return thought.GetString();
            }
        }

        return null;
    }

    internal static string? ExtractReasoningContent(OpenAI.Chat.ChatCompletion chatCompletion)
    {
        BinaryData data = ModelReaderWriter.Write(chatCompletion);
        using JsonDocument document = JsonDocument.Parse(data);

        if (!document.RootElement.TryGetProperty("choices", out JsonElement choices) || choices.GetArrayLength() == 0)
        {
            return null;
        }

        JsonElement firstChoice = choices[0];

        if (firstChoice.TryGetProperty("message", out JsonElement message))
        {
            if (message.TryGetProperty("reasoning_content", out JsonElement reasoningContent) && reasoningContent.ValueKind == JsonValueKind.String)
            {
                return reasoningContent.GetString();
            }

            if (message.TryGetProperty("reasoning", out JsonElement reasoning) && reasoning.ValueKind == JsonValueKind.String)
            {
                return reasoning.GetString();
            }

            if (message.TryGetProperty("thought", out JsonElement thought) && thought.ValueKind == JsonValueKind.String)
            {
                return thought.GetString();
            }
        }

        return null;
    }

    #endregion
}

#endregion

#region Builder Extensions

public static class ReasoningChatClientExtensions
{
    public static ChatClientBuilder UseReasoningExtraction(this ChatClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Use(innerClient => new OpenAIClient(innerClient));
    }
}

#endregion
