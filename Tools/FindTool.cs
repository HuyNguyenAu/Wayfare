namespace WayFare.Tools;

internal sealed class FindTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "find";
    public string DisplayName => "Find";
    public string Description => "Search for files and directories whose name contains 'pattern' (case-insensitive substring match). Returns matching paths, one per line. Parameters: pattern path (string, optional - directory to search in, defaults to current directory), (string, required - the text to search for in file/directory names)";

    public string GetInvocationMessage(string arguments)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out FindArguments? findArguments, out string? findArgumentsError))
        {
            throw new ArgumentException($"Failed to deserialise arguments for {Name} tool. Error: {findArgumentsError}. Arguments: {arguments}");
        }

        string searchPath = string.IsNullOrWhiteSpace(findArguments.Path) ? "." : findArguments.Path;
        return $"[{DisplayName}] [{searchPath}] [{findArguments.Pattern}]";
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out FindArguments? findArguments, out string? findArgumentsError))
        {
            return new ToolExecutionResult(false, "Invalid arguments", string.Empty, findArgumentsError);
        }

        if (string.IsNullOrWhiteSpace(findArguments.Pattern))
        {
            return new ToolExecutionResult(false, "Missing pattern", string.Empty, "Failed to execute find because 'pattern' parameter is required");
        }

        string searchPath = string.IsNullOrWhiteSpace(findArguments.Path) ? "." : findArguments.Path;

        if (!toolHelpers.TryGetRequiredPath(searchPath, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, "Invalid path", string.Empty, $"Failed to execute find because {requiredPathError}");
        }

        if (!Directory.Exists(resolvedPath))
        {
            return new ToolExecutionResult(false, "Directory does not exist", string.Empty, $"Failed to execute find because directory does not exist: {resolvedPath}");
        }

        try
        {
            List<string> matches = [];

            foreach (string entry in Directory.EnumerateFileSystemEntries(resolvedPath, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (Path.GetFileName(entry).Contains(findArguments.Pattern, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(Path.GetRelativePath(Directory.GetCurrentDirectory(), entry));
                }
            }

            if (matches.Count <= 0)
            {
                return new ToolExecutionResult(true, "No matches found", string.Empty, "No matches found");
            }

            string result = string.Join(Environment.NewLine, matches);
            return new ToolExecutionResult(true, $"{matches.Count} matches found", result, string.Empty);
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult(false, "Exception occurred", string.Empty, $"Failed to execute find because {ex}", ex);
        }
    }

    internal record FindArguments(string? Path, string Pattern);
}
