namespace WayFare.Tools;

internal sealed class WriteFileTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "Write File";
    public string Description => "Write content to a file, creating it if it does not exist or overwriting it if it does. Use this only for new files or full rewrites. To make targeted edits to an existing file, use replace instead. Parameters: path (string, required), content (string, required - the full content to write).";

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out WriteFileArguments? writeFileArguments, out string? writeFileArgumentsError))
        {
            return new ToolExecutionResult(false, string.Empty, writeFileArgumentsError);
        }

        if (!toolHelpers.TryGetRequiredPath(writeFileArguments.Path, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, string.Empty, $"Failed to write file because {requiredPathError}");
        }

        toolHelpers.EnsureDirectoryExists(resolvedPath);

        await File.WriteAllTextAsync(resolvedPath, writeFileArguments.Content ?? string.Empty, cancellationToken);

        return new ToolExecutionResult(true, $"Successfully wrote file '{writeFileArguments.Path}'", string.Empty);
    }

    internal record WriteFileArguments(string Path, string Content);
}

