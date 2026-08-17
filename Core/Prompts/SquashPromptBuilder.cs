namespace Wayfare.Core.Prompts;

using System.Text;

public static class SquashPromptBuilder
{
    public static string Build()
    {
        StringBuilder promptBuilder = new();

        promptBuilder.AppendLine("You are a concise technical summariser. Extract the key milestone data from the provided execution trace.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Follow the STARL format (Situation, Task, Action, Result, Learnings) exactly.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Rules:");
        promptBuilder.AppendLine("- Be strictly factual and concise. No conversational filler, intros, or outros.");
        promptBuilder.AppendLine("- Keep each field to 1-2 sentences.");
        promptBuilder.AppendLine("- Output ONLY the formatted text below.");

        return promptBuilder.ToString();
    }
}

