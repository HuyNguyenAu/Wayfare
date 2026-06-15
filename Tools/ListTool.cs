namespace WayFare.Tools;

internal sealed class ListTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "list";
    public string DisplayName => "List";
    public string Description => "List the files and directories inside a directory. Directory names end with a path separator. Parameters: path (string, required - the directory to list)";

    public string GetInvocationMessage(string arguments)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out ListFilesArguments? listArguments, out string? listArgumentsError))
        {
            throw new ArgumentException($"Failed to deserialise arguments for {Name} tool. Error: {listArgumentsError}. Arguments: {arguments}");
        }

        return $"[{DisplayName}] [{listArguments.Path}]";
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out ListFilesArguments? listArguments, out string? listArgumentsError))
        {
            return new ToolExecutionResult(false, "Invalid arguments", string.Empty, listArgumentsError);
        }

        if (!toolHelpers.TryGetRequiredPath(listArguments.Path, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, "Invalid path", string.Empty, requiredPathError);
        }

        if (!Directory.Exists(resolvedPath))
        {
            return new ToolExecutionResult(false, "Directory does not exist", string.Empty, $"Failed to execute list because directory does not exist '{listArguments.Path}'");
        }

        try
        {
            string[] entries = [.. Directory.EnumerateFileSystemEntries(resolvedPath)];
            List<string> matches = [];

            foreach (string entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string name = Path.GetFileName(entry);

                if (Directory.Exists(entry))
                {
                    name += Path.DirectorySeparatorChar;
                }

                matches.Add(name);
            }

            return new ToolExecutionResult(true, $"{matches.Count} entries found", string.Join(Environment.NewLine, matches), string.Empty);
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult(false, "Exception occurred", string.Empty, $"Failed to execute list because {ex}", ex);
        }
    }

    internal record ListFilesArguments(string Path);
}

