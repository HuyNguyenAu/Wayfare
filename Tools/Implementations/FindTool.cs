namespace Wayfare.Tools.Implementations;

using Wayfare.Tools;

internal sealed class FindTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "find";
    public string DisplayName => "Find";
    public string Description => "Search for files and directories whose name contains 'pattern' (case-insensitive substring match). Returns matching paths, one per line. Parameters: path (string, optional - directory to search in, defaults to current directory), pattern (string, required - the text to search for in file/directory names)";

    public ToolSchema Parameters => ToolSchema.Object(new Dictionary<string, ToolPropertySchema>
    {
        ["path"] = ToolPropertySchema.String("Directory to search in. Defaults to '.' (current directory)."),
        ["pattern"] = ToolPropertySchema.String("The text to search for in file/directory names.")
    });

    public string GetInvocationMessage(string arguments)
    {
        return toolHelpers.TryDeserializeArguments(arguments, out FindArguments? args, out _)
            ? $"[{DisplayName}] [{(string.IsNullOrWhiteSpace(args.Path) ? "." : args.Path)}] [{args.Pattern}]"
            : $"[{DisplayName}] [{arguments}]";
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out FindArguments? findArguments, out string? findArgumentsError))
        {
            return new ToolExecutionResult(false, "Failed to search due to invalid tool arguments.", string.Empty, $"Failed to search: invalid tool arguments. Error: {findArgumentsError}. Usage: {{\"path\": \".\", \"pattern\": \"<search_pattern>\"}}");
        }

        if (string.IsNullOrWhiteSpace(findArguments.Pattern))
        {
            return new ToolExecutionResult(false, "Failed to search because 'pattern' parameter is missing.", string.Empty, "Failed to search: 'pattern' parameter is required. Specify the filename or directory substring to search for, e.g. {\"path\": \".\", \"pattern\": \"csproj\"}.");
        }

        string searchPath = string.IsNullOrWhiteSpace(findArguments.Path) ? "." : findArguments.Path;

        if (!toolHelpers.TryGetRequiredPath(searchPath, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, $"Failed to search: access denied or invalid path '{searchPath}'.", string.Empty, $"Failed to search: access denied or invalid path '{searchPath}'. Path must be within the working directory.");
        }

        if (!Directory.Exists(resolvedPath))
        {
            return new ToolExecutionResult(false, $"Failed to search: directory does not exist '{resolvedPath}'.", string.Empty, $"Failed to search: directory does not exist at '{resolvedPath}'. Use 'list' with path \".\" to view available directories.");
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
                return new ToolExecutionResult(true, $"No matches found for '{findArguments.Pattern}' in '{searchPath}'.", $"No files or directories matching pattern '{findArguments.Pattern}' were found in '{searchPath}'.", string.Empty);
            }

            string result = $"Found {matches.Count} match(es) for pattern '{findArguments.Pattern}' in '{searchPath}':{Environment.NewLine}{string.Join(Environment.NewLine, matches)}";
            return new ToolExecutionResult(true, $"Found {matches.Count} match(es) for '{findArguments.Pattern}' in '{searchPath}'.", result, string.Empty);
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult(false, $"An unexpected error occurred while searching in '{resolvedPath}'.", string.Empty, $"Failed to search: an unexpected error occurred. Error: {ex.Message}", ex);
        }
    }

    internal record FindArguments(string Path = ".", string Pattern = "");
}
