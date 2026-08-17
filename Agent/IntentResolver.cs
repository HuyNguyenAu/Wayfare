namespace Wayfare.Agent;

using Microsoft.Extensions.AI;
using Wayfare.Session;

public class IntentResolver(IChatClient chatClient) : IIntentResolver
{
    public async Task<string> ResolveAsync(string currentIntent, string userInput, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentIntent);
        ArgumentException.ThrowIfNullOrWhiteSpace(userInput);

        List<ChatMessage> messages = [
            new ChatMessage(ChatRole.System, IntentPromptBuilder.BuildSystem()),
            new ChatMessage(ChatRole.User, IntentPromptBuilder.BuildUser(currentIntent, userInput))
        ];

        ChatResponse response = await chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        string resolvedIntent = (response.Text ?? string.Empty).Trim();

        return !string.IsNullOrWhiteSpace(resolvedIntent) ? resolvedIntent : userInput.Trim();
    }
}
