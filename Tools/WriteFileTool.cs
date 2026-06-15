namespace WayFare.Tools;

internal sealed class WriteFileTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "write";
    public string DisplayName => "Write";
    public string Description => "Writes the specified content to a file. Overwrites the file if it already exists, or creates it and any parent directories if it does not. Use only for new files or complete rewrites; for modifications, use the \"replace\" tool instead. Expects a JSON object with properties: \"path\" (string, required - the path of the file to write), \"content\" (string, required - the full text content to write). Example: {\"path\": \"src/newfile.txt\", \"content\": \"Hello World\"}";

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
            return new ToolExecutionResult(false, "Failed to write file due to invalid tool arguments.", string.Empty, $"Failed to write file: invalid tool arguments. Error: {writeFileArgumentsError}");
        }

        if (!toolHelpers.TryGetRequiredPath(writeFileArguments.Path, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, $"Failed to write file: access denied or invalid path '{writeFileArguments.Path}'.", string.Empty, $"Failed to write file: access denied or invalid path '{writeFileArguments.Path}'.");
        }

        try
        {
            toolHelpers.EnsureDirectoryExists(resolvedPath);

            await File.WriteAllTextAsync(resolvedPath, writeFileArguments.Content ?? string.Empty, cancellationToken);

            return new ToolExecutionResult(true, $"Successfully wrote content to file '{writeFileArguments.Path}'.", $"Successfully wrote all content to file '{writeFileArguments.Path}'.", string.Empty);
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult(false, $"An unexpected error occurred while writing to file '{writeFileArguments.Path}'.", string.Empty, $"Failed to write file: an unexpected error occurred. Error: {ex.Message}", ex);
        }
    }

    internal record WriteFileArguments(string Path, string Content);
}

