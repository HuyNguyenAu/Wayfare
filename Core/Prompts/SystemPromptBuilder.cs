using System.Text;
using Wayfare.Core.Abstractions;

namespace Wayfare.Core.Prompts;

public static class SystemPromptBuilder
{
    public static string Build(IReadOnlyList<ITool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        StringBuilder promptBuilder = new();

        promptBuilder.AppendLine("You are a coding agent harness. You help users by reading and editing files.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Available tools:");

        foreach (ITool tool in tools)
        {
            promptBuilder.AppendLine($"- {tool.Name}: {tool.Description}");
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Rules:");
        promptBuilder.AppendLine("- To see what files exist, use list or find.");
        promptBuilder.AppendLine("- To read a file, use read.");
        promptBuilder.AppendLine("- To edit a file, use replace. The oldText must match exactly what is in the file, including whitespace.");
        promptBuilder.AppendLine("- The oldText in replace must appear exactly once in the file. If it appears more than once, add more surrounding lines to make it unique.");
        promptBuilder.AppendLine("- To create a new file or completely overwrite one, use write.");
        promptBuilder.AppendLine("- Always read a file before editing it.");
        promptBuilder.AppendLine("- Keep responses short. Show file paths when working with files.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"Current date: {DateTime.UtcNow:yyyy-MM-dd HH:mm UTC}");
        promptBuilder.AppendLine($"Current operating system: {Environment.OSVersion}");
        promptBuilder.AppendLine($"Current working directory: {Directory.GetCurrentDirectory()}");

        return promptBuilder.ToString();
    }
}
