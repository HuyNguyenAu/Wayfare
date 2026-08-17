namespace Wayfare.Agent;

using Wayfare.Infrastructure.AI;
using Wayfare.Session;

public class IntentResolver(IChatClient chatClient) : IIntentResolver
{
    public async Task<string> ResolveAsync(string currentIntent, string userInput, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentIntent);
        ArgumentException.ThrowIfNullOrWhiteSpace(userInput);

        IReadOnlyList<SessionMessage> messages = [
            new SystemMessage(IntentPromptBuilder.BuildSystem()),
            new UserMessage(IntentPromptBuilder.BuildUser(currentIntent, userInput))
        ];

        ChatCompletionResult response = await chatClient.CompleteChatAsync(messages, [], cancellationToken);
        string resolvedIntent = response.Content.Trim();

        return !string.IsNullOrWhiteSpace(resolvedIntent) ? resolvedIntent : userInput.Trim();
    }
}
