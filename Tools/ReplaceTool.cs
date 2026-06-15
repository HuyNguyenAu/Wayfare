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
            return new ToolExecutionResult(false, "Invalid arguments", string.Empty, replaceArgumentsError);
        }

        if (!toolHelpers.TryGetRequiredPath(replaceArguments.Path, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, "Invalid path", string.Empty, requiredPathError);
        }

        if (!File.Exists(resolvedPath))
        {
            return new ToolExecutionResult(false, "File does not exist", string.Empty, $"Failed to replace text because file does not exist at path '{replaceArguments.Path}'");
        }

        if (string.IsNullOrEmpty(replaceArguments.OldText))
        {
            return new ToolExecutionResult(false, "Invalid arguments", string.Empty, "Failed to replace text: 'oldText' is required and cannot be empty");
        }

        try
        {
            string content = await File.ReadAllTextAsync(resolvedPath, cancellationToken);

            int index = content.IndexOf(replaceArguments.OldText, StringComparison.Ordinal);

            if (index == -1)
            {
                return new ToolExecutionResult(false, "Text not found", string.Empty, $"Failed to replace text: 'oldText' not found in '{replaceArguments.Path}'. It must match the file exactly, including whitespace and line endings. Read the file first to confirm the exact text");
            }

            int lastIndex = content.LastIndexOf(replaceArguments.OldText, StringComparison.Ordinal);

            if (index != lastIndex)
            {
                return new ToolExecutionResult(false, "Text not unique", string.Empty, $"Failed to replace text: 'oldText' appears more than once in '{replaceArguments.Path}'. Add more surrounding lines to make it unique");
            }

            string newContent = content.Remove(index, replaceArguments.OldText.Length).Insert(index, replaceArguments.NewText ?? string.Empty);

            await File.WriteAllTextAsync(resolvedPath, newContent, cancellationToken);

            return new ToolExecutionResult(true, "Text replaced successfully", $"Successfully replaced text in '{replaceArguments.Path}'", string.Empty);
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult(false, "Exception occurred", string.Empty, $"Failed to replace text because {ex}", ex);
        }
    }

    internal record ReplaceArguments(string Path, string OldText, string NewText);
}

