namespace Wayfare.Tools.Implementations;

using Wayfare.Tools;

internal sealed class ReplaceTool(IToolHelpers toolHelpers) : ITool
{
    private readonly IToolHelpers _toolHelpers = toolHelpers ?? throw new ArgumentNullException(nameof(toolHelpers));

    public string Name => "replace";
    public string DisplayName => "Replace";
    public string Description => "Replace a block of text in a file. 'oldText' must match the file exactly (including whitespace and line endings) and must appear exactly once. If it appears more than once, include more surrounding lines or specify startLine and endLine to make it unique. Parameters: path (string, required), oldText (string, required - the exact text to find), newText (string, required - the text to replace it with), startLine (int, optional - 1-based start line of search window), endLine (int, optional - 1-based end line of search window).";

    public ToolSchema Parameters => ToolSchema.Object(new Dictionary<string, ToolPropertySchema>
    {
        ["path"] = ToolPropertySchema.String("The path of the file to edit."),
        ["oldText"] = ToolPropertySchema.String("The exact block of text to replace. Must match the file uniquely."),
        ["newText"] = ToolPropertySchema.String("The replacement text to insert in place of oldText."),
        ["startLine"] = ToolPropertySchema.Integer("Optional 1-based start line of search window (default 1)."),
        ["endLine"] = ToolPropertySchema.Integer("Optional 1-based end line of search window (default end of file).")
    }, required: ["path", "oldText", "newText"]);

    public string GetInvocationMessage(string arguments)
    {
        return _toolHelpers.TryDeserialiseArguments(arguments, out ReplaceArguments? parsedArguments, out _)
            ? $"[{DisplayName}] [{parsedArguments.Path}] [Old \"{parsedArguments.OldText}\"] [New \"{parsedArguments.NewText}\"]"
            : $"[{DisplayName}] [{arguments}]";
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!_toolHelpers.TryDeserialiseArguments(arguments, out ReplaceArguments? replaceArguments, out string? replaceArgumentsError))
        {
            return new ToolExecutionResult(false, "Failed to replace text due to invalid tool arguments.", string.Empty, $"<observation tool=\"replace\" status=\"error\">\nFailed to replace text: invalid tool arguments. Error: {replaceArgumentsError}. Usage: {{\"path\": \"<file_path>\", \"oldText\": \"<exact_snippet>\", \"newText\": \"<replacement>\"}}\n</observation>");
        }

        if (!_toolHelpers.TryGetRequiredPath(replaceArguments.Path, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, $"Failed to replace text: access denied or invalid path '{replaceArguments.Path}'.", string.Empty, $"<observation tool=\"replace\" status=\"error\">\nFailed to replace text: access denied or invalid path '{replaceArguments.Path}'. Path must be within the working directory.\n</observation>");
        }

        if (!File.Exists(resolvedPath))
        {
            return new ToolExecutionResult(false, $"Failed to replace text: file does not exist '{replaceArguments.Path}'.", string.Empty, $"<observation tool=\"replace\" status=\"error\">\nFailed to replace text: file does not exist at '{replaceArguments.Path}'. Use 'find' or 'list' to verify existing file paths before attempting replacement.\n</observation>");
        }

        if (string.IsNullOrEmpty(replaceArguments.OldText))
        {
            return new ToolExecutionResult(false, "Failed to replace text because 'oldText' parameter is missing.", string.Empty, $"<observation tool=\"replace\" status=\"error\">\nFailed to replace text: 'oldText' parameter is required. Provide the exact text snippet to replace.\n</observation>");
        }

        try
        {
            string content = await File.ReadAllTextAsync(resolvedPath, cancellationToken);
            bool hasCrlf = content.Contains("\r\n");

            string normalisedContent = content.Replace("\r\n", "\n");
            string normalisedOldText = replaceArguments.OldText.Replace("\r\n", "\n");
            string normalisedNewText = replaceArguments.NewText.Replace("\r\n", "\n");

            WindowRange? windowRange = GetWindowRange(normalisedContent, replaceArguments.StartLine, replaceArguments.EndLine, out string? windowError);

            if (windowRange is null)
            {
                return new ToolExecutionResult(false, $"Failed to replace text: invalid search window in '{replaceArguments.Path}'.", string.Empty, $"<observation tool=\"replace\" status=\"error\">\nFailed to replace text: {windowError}\n</observation>");
            }

            int windowStartIndex = windowRange.StartIndex;
            int windowEndIndex = windowRange.EndIndex;
            string windowContent = normalisedContent[windowStartIndex..windowEndIndex];

            int matchInWindow = windowContent.IndexOf(normalisedOldText, StringComparison.Ordinal);

            if (matchInWindow == -1)
            {
                return new ToolExecutionResult(false, $"Failed to replace text: target text not found in '{replaceArguments.Path}'.", string.Empty, $"<observation tool=\"replace\" status=\"error\">\nFailed to replace text: 'oldText' was not found in '{replaceArguments.Path}' (0 matches found). The search block must match the file content exactly, including all indentation and whitespace. Use 'read' to inspect the exact current file content before retrying.\n</observation>");
            }

            int lastMatchInWindow = windowContent.LastIndexOf(normalisedOldText, StringComparison.Ordinal);

            if (matchInWindow != lastMatchInWindow)
            {
                return new ToolExecutionResult(false, $"Failed to replace text: target text is not unique in '{replaceArguments.Path}'.", string.Empty, $"<observation tool=\"replace\" status=\"error\">\nFailed to replace text: 'oldText' matches multiple locations in '{replaceArguments.Path}'. Please include more surrounding lines or specify 'startLine' and 'endLine' to restrict the search window.\n</observation>");
            }

            int matchIndex = windowStartIndex + matchInWindow;
            string replacedContent = normalisedContent.Remove(matchIndex, normalisedOldText.Length).Insert(matchIndex, normalisedNewText);
            string finalContent = hasCrlf ? replacedContent.Replace("\n", "\r\n") : replacedContent;

            await File.WriteAllTextAsync(resolvedPath, finalContent, cancellationToken);

            return new ToolExecutionResult(true, $"Successfully replaced text in '{replaceArguments.Path}'.", $"<observation tool=\"replace\" path=\"{replaceArguments.Path}\" status=\"success\">\nSuccessfully replaced text block in file '{replaceArguments.Path}'.\n</observation>", string.Empty);
        }
        catch (Exception exception)
        {
            return new ToolExecutionResult(false, $"An unexpected error occurred while replacing text in '{replaceArguments.Path}'.", string.Empty, $"<observation tool=\"replace\" status=\"error\">\nFailed to replace text: an unexpected error occurred. Error: {exception.Message}\n</observation>");
        }
    }

    private static WindowRange? GetWindowRange(string content, int? startLine, int? endLine, out string? error)
    {
        int totalLines = content.Split('\n').Length;
        int start = startLine ?? 1;
        int end = endLine ?? totalLines;

        if (start < 1 || end < 1 || start > end)
        {
            error = $"Invalid search window [startLine={startLine}, endLine={endLine}]. startLine must be >= 1 and <= endLine.";
            return null;
        }

        if (start > totalLines)
        {
            error = $"startLine {start} is beyond the total lines in the file ({totalLines}).";
            return null;
        }

        int startIndex = 0;

        if (start > 1)
        {
            int currentLine = 1;

            for (int characterIndex = 0; characterIndex < content.Length; characterIndex++)
            {
                if (content[characterIndex] == '\n')
                {
                    currentLine++;
                    if (currentLine == start)
                    {
                        startIndex = characterIndex + 1;
                        break;
                    }
                }
            }
        }

        int endIndex = content.Length;

        if (end < totalLines)
        {
            int currentLine = 1;

            for (int characterIndex = 0; characterIndex < content.Length; characterIndex++)
            {
                if (content[characterIndex] == '\n')
                {
                    if (currentLine == end)
                    {
                        endIndex = characterIndex + 1;
                        break;
                    }

                    currentLine++;
                }
            }
        }

        error = null;

        return new WindowRange(startIndex, endIndex);
    }

    internal record ReplaceArguments(string Path = "", string OldText = "", string NewText = "", int? StartLine = null, int? EndLine = null);
    internal record WindowRange(int StartIndex, int EndIndex);
}
