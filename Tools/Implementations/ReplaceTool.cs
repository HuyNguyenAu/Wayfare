namespace Wayfare.Tools.Implementations;

using Wayfare.Tools;

internal sealed class ReplaceTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "replace";
    public string DisplayName => "Replace";
    public string Description => "Replace a block of text in a file. 'oldText' must match the file exactly (including whitespace and line endings) and must appear exactly once. If it appears more than once, include more surrounding lines to make it unique. Parameters: path (string, required), oldText (string, required - the exact text to find), newText (string, required - the text to replace it with).";

    public ToolSchema Parameters => ToolSchema.Object(new Dictionary<string, ToolPropertySchema>
    {
        ["path"] = ToolPropertySchema.String("The path of the file to edit."),
        ["oldText"] = ToolPropertySchema.String("The exact block of text to replace. Must match the file uniquely."),
        ["newText"] = ToolPropertySchema.String("The replacement text to insert in place of oldText.")
    });

    public string GetInvocationMessage(string arguments)
    {
        return toolHelpers.TryDeserialiseArguments(arguments, out ReplaceArguments? parsedArguments, out _)
            ? $"[{DisplayName}] [{parsedArguments.Path}] [Old \"{parsedArguments.OldText}\"] [New \"{parsedArguments.NewText}\"]"
            : $"[{DisplayName}] [{arguments}]";
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!toolHelpers.TryDeserialiseArguments(arguments, out ReplaceArguments? replaceArguments, out string? replaceArgumentsError))
        {
            return new ToolExecutionResult(false, "Failed to replace text due to invalid tool arguments.", string.Empty, $"Failed to replace text: invalid tool arguments. Error: {replaceArgumentsError}. Usage: {{\"path\": \"<file_path>\", \"oldText\": \"<exact_snippet>\", \"newText\": \"<replacement>\"}}");
        }

        if (!toolHelpers.TryGetRequiredPath(replaceArguments.Path, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, $"Failed to replace text: access denied or invalid path '{replaceArguments.Path}'.", string.Empty, $"Failed to replace text: access denied or invalid path '{replaceArguments.Path}'. Path must be within the working directory.");
        }

        if (!File.Exists(resolvedPath))
        {
            return new ToolExecutionResult(false, $"Failed to replace text: file does not exist '{replaceArguments.Path}'.", string.Empty, $"Failed to replace text: file does not exist at '{replaceArguments.Path}'. Use 'find' or 'list' to verify existing file paths before attempting replacement.");
        }

        if (string.IsNullOrEmpty(replaceArguments.OldText))
        {
            return new ToolExecutionResult(false, "Failed to replace text because 'oldText' parameter is missing.", string.Empty, "Failed to replace text: 'oldText' parameter is required. Provide the exact text snippet to replace.");
        }

        try
        {
            string content = await File.ReadAllTextAsync(resolvedPath, cancellationToken);
            bool hasCrlf = content.Contains("\r\n");

            string normalisedContent = content.Replace("\r\n", "\n");
            string normalisedOldText = replaceArguments.OldText.Replace("\r\n", "\n");
            string normalisedNewText = replaceArguments.NewText.Replace("\r\n", "\n");

            int matchIndex = normalisedContent.IndexOf(normalisedOldText, StringComparison.Ordinal);

            if (matchIndex == -1)
            {
                return new ToolExecutionResult(false, $"Failed to replace text: target text not found in '{replaceArguments.Path}'.", string.Empty, $"Failed to replace text: 'oldText' was not found in '{replaceArguments.Path}' (0 matches found). The search block must match the file content exactly, including all indentation and whitespace. Use 'read' to inspect the exact current file content before retrying.");
            }

            int lastMatchIndex = normalisedContent.LastIndexOf(normalisedOldText, StringComparison.Ordinal);

            if (matchIndex != lastMatchIndex)
            {
                return new ToolExecutionResult(false, $"Failed to replace text: target text is not unique in '{replaceArguments.Path}'.", string.Empty, $"Failed to replace text: 'oldText' matches multiple locations in '{replaceArguments.Path}'. Please include more surrounding lines/context above or below the target block to make the search string unique.");
            }

            string replacedContent = normalisedContent.Remove(matchIndex, normalisedOldText.Length).Insert(matchIndex, normalisedNewText);
            string finalContent = hasCrlf ? replacedContent.Replace("\r\n", "\n").Replace("\n", "\r\n") : replacedContent;

            await File.WriteAllTextAsync(resolvedPath, finalContent, cancellationToken);

            return new ToolExecutionResult(true, $"Successfully replaced text in '{replaceArguments.Path}'.", $"Successfully replaced text block in file '{replaceArguments.Path}'.", string.Empty);
        }
        catch (Exception exception)
        {
            return new ToolExecutionResult(false, $"An unexpected error occurred while replacing text in '{replaceArguments.Path}'.", string.Empty, $"Failed to replace text: an unexpected error occurred. Error: {exception.Message}", exception);
        }
    }

    internal record ReplaceArguments(string Path = "", string OldText = "", string NewText = "");
}
