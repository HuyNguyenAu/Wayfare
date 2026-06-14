namespace WayFare.Tools;

internal sealed class FindTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "Find";
    public string Description => "Search for files and directories whose name contains 'pattern' (case-insensitive substring match). Returns matching paths, one per line. Parameters: pattern path (string, optional - directory to search in, defaults to current directory), (string, required - the text to search for in file/directory names)";

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out FindArguments? findArguments, out string? findArgumentsError))
        {
            return new ToolExecutionResult(false, string.Empty, findArgumentsError);
        }

        if (string.IsNullOrWhiteSpace(findArguments.Pattern))
        {
            return new ToolExecutionResult(false, string.Empty, "Failed to execute find because 'pattern' parameter is required");
        }

        string searchPath = string.IsNullOrWhiteSpace(findArguments.Path) ? "." : findArguments.Path;

        if (!toolHelpers.TryGetRequiredPath(searchPath, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, string.Empty, $"Failed to execute find because {requiredPathError}");
        }

        if (!Directory.Exists(resolvedPath))
        {
            return new ToolExecutionResult(false, string.Empty, $"Failed to execute find because directory does not exist: {resolvedPath}");
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
                return new ToolExecutionResult(true, string.Empty, "No matches found");
            }

            string result = string.Join(Environment.NewLine, matches);
            return new ToolExecutionResult(true, result, string.Empty);
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult(false, string.Empty, $"Failed to execute find because {ex}", ex);
        }
    }

    internal record FindArguments(string? Path, string Pattern);
}
