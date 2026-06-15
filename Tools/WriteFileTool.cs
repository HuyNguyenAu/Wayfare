namespace WayFare.Tools;

internal sealed class WriteFileTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "write";
    public string DisplayName => "Write";
    public string Description => "Write content to a file, creating it if it does not exist or overwriting it if it does. Use this only for new files or full rewrites. To make targeted edits to an existing file, use replace instead. Parameters: path (string, required), content (string, required - the full content to write).";

    public string GetInvocationMessage(string arguments)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out WriteFileArguments? writeFileArguments, out string? writeFileArgumentsError))
        {
            throw new ArgumentException($"Failed to deserialise arguments for {Name} tool. Error: {writeFileArgumentsError}. Arguments: {arguments}");
        }

        return $"[{DisplayName}] [{writeFileArguments.Path}]";
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out WriteFileArguments? writeFileArguments, out string? writeFileArgumentsError))
        {
            return new ToolExecutionResult(false, "Invalid arguments", string.Empty, writeFileArgumentsError);
        }

        if (!toolHelpers.TryGetRequiredPath(writeFileArguments.Path, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, "Invalid path", string.Empty, requiredPathError);
        }

        toolHelpers.EnsureDirectoryExists(resolvedPath);

        await File.WriteAllTextAsync(resolvedPath, writeFileArguments.Content ?? string.Empty, cancellationToken);

        return new ToolExecutionResult(true, "File written successfully", $"Successfully wrote file '{writeFileArguments.Path}'", string.Empty);
    }

    internal record WriteFileArguments(string Path, string Content);
}

