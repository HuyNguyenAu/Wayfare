namespace Wayfare.Core;

using Wayfare.Core.Models;
using Wayfare.Core.Prompts;

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
        string trimmed = response.Content.Trim();

        return !string.IsNullOrWhiteSpace(trimmed) ? trimmed : userInput.Trim();
    }
}
