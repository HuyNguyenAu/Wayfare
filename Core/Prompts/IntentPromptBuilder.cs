namespace Wayfare.Core.Prompts;

using System.Text;

public static class IntentPromptBuilder
{
    public static string BuildSystem()
    {
        StringBuilder builder = new();
        builder.AppendLine("You are an intent summariser for a coding assistant.");
        builder.AppendLine("Given the current session intent and new user input, state the active goal in 1-2 concise sentences.");
        builder.AppendLine("Output ONLY the goal text without any labels, introductory, or concluding remarks.");

        return builder.ToString();
    }

    public static string BuildUser(string currentIntent, string userInput)
    {
        ArgumentNullException.ThrowIfNull(currentIntent);
        ArgumentException.ThrowIfNullOrWhiteSpace(userInput);

        StringBuilder builder = new();
        builder.AppendLine($"Current Session Intent: {(string.IsNullOrWhiteSpace(currentIntent) ? "(None)" : currentIntent)}");
        builder.AppendLine($"User Input: {userInput}");

        return builder.ToString();
    }
}
