namespace WayFare.Tools;

internal sealed class ReplaceTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "replace";
    public string DisplayName => "Replace";
    public string Description => "Replace a block of text in a file. 'oldText' must match the file exactly (including whitespace and line endings) and must appear exactly once. If it appears more than once, include more surrounding lines to make it unique. Parameters: path (string, required), oldText (string, required - the exact text to find), newText (string, required - the text to replace it with).";

    public string GetInvocationMessage(string arguments)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out ReplaceArguments? replaceArguments, out string? replaceArgumentsError))
        {
            throw new ArgumentException($"Failed to deserialise arguments for {Name} tool. Error: {replaceArgumentsError}. Arguments: {arguments}");
        }

        return $"[{DisplayName}] [{replaceArguments.Path}] [Old \"{replaceArguments.OldText}\"] [New \"{replaceArguments.NewText}\"]";
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out ReplaceArguments? replaceArguments, out string? replaceArgumentsError))
        {
            return new ToolExecutionResult(false, "Failed to replace text due to invalid tool arguments.", string.Empty, $"Failed to replace text: invalid tool arguments. Error: {replaceArgumentsError}");
        }

        if (!toolHelpers.TryGetRequiredPath(replaceArguments.Path, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, $"Failed to replace text: access denied or invalid path '{replaceArguments.Path}'.", string.Empty, $"Failed to replace text: access denied or invalid path '{replaceArguments.Path}'.");
        }

        if (!File.Exists(resolvedPath))
        {
            return new ToolExecutionResult(false, $"Failed to replace text: file does not exist '{replaceArguments.Path}'.", string.Empty, $"Failed to replace text: file does not exist at '{replaceArguments.Path}'.");
        }

        if (string.IsNullOrEmpty(replaceArguments.OldText))
        {
            return new ToolExecutionResult(false, "Failed to replace text because 'oldText' parameter is missing.", string.Empty, "Failed to replace text: 'oldText' parameter is required.");
        }

        try
        {
            string content = await File.ReadAllTextAsync(resolvedPath, cancellationToken);

            int index = content.IndexOf(replaceArguments.OldText, StringComparison.Ordinal);

            if (index == -1)
            {
                return new ToolExecutionResult(false, $"Failed to replace text: target text not found in '{replaceArguments.Path}'.", string.Empty, $"Failed to replace text: 'oldText' was not found in '{replaceArguments.Path}'. The search block must match the file content exactly, including all whitespace, indentation, and newlines. Please read the file first to verify the exact content.");
            }

            int lastIndex = content.LastIndexOf(replaceArguments.OldText, StringComparison.Ordinal);

            if (index != lastIndex)
            {
                return new ToolExecutionResult(false, $"Failed to replace text: target text is not unique in '{replaceArguments.Path}'.", string.Empty, $"Failed to replace text: 'oldText' matches multiple locations in '{replaceArguments.Path}'. Please include more surrounding lines/context to make the block unique.");
            }

            string newContent = content.Remove(index, replaceArguments.OldText.Length).Insert(index, replaceArguments.NewText ?? string.Empty);

            await File.WriteAllTextAsync(resolvedPath, newContent, cancellationToken);

            return new ToolExecutionResult(true, $"Successfully replaced text in '{replaceArguments.Path}'.", $"Successfully replaced text block in file '{replaceArguments.Path}'.", string.Empty);
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult(false, $"An unexpected error occurred while replacing text in '{replaceArguments.Path}'.", string.Empty, $"Failed to replace text: an unexpected error occurred. Error: {ex.Message}", ex);
        }
    }

    internal record ReplaceArguments(string Path, string OldText, string NewText);
}

