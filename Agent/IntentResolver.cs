namespace Wayfare.Agent;

using Microsoft.Extensions.AI;

public sealed class IntentResolver(IChatClient chatClient) : IIntentResolver
{
    private readonly IChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    public async Task<string> ResolveAsync(string currentIntent, string userInput, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentIntent);
        ArgumentException.ThrowIfNullOrWhiteSpace(userInput);

        List<ChatMessage> messages = [
            new ChatMessage(ChatRole.System, IntentPromptBuilder.BuildSystem()),
            new ChatMessage(ChatRole.User, IntentPromptBuilder.BuildUser(currentIntent, userInput))
        ];

        ChatResponse response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        string resolvedIntent = (response.Text ?? string.Empty).Trim();

        if (resolvedIntent.Contains("<goal>") && resolvedIntent.Contains("</goal>"))
        {
            int start = resolvedIntent.IndexOf("<goal>") + "<goal>".Length;
            int end = resolvedIntent.IndexOf("</goal>", start);

            if (end > start)
            {
                resolvedIntent = resolvedIntent[start..end].Trim();
            }
        }

        return !string.IsNullOrWhiteSpace(resolvedIntent) ? resolvedIntent : userInput.Trim();
    }
}
