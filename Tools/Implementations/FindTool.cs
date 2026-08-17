namespace Wayfare.Tools.Implementations;

using System.Text;
using Wayfare.Tools;

internal sealed class FindTool(IToolHelpers toolHelpers) : ITool
{
    private readonly IToolHelpers _toolHelpers = toolHelpers ?? throw new ArgumentNullException(nameof(toolHelpers));

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
        return _toolHelpers.TryDeserialiseArguments(arguments, out FindArguments? parsedArguments, out _)
            ? $"[{DisplayName}] [{(string.IsNullOrWhiteSpace(parsedArguments.Path) ? "." : parsedArguments.Path)}] [{parsedArguments.Pattern}]"
            : $"[{DisplayName}] [{arguments}]";
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!_toolHelpers.TryDeserialiseArguments(arguments, out FindArguments? findArguments, out string? findArgumentsError))
        {
            return new ToolExecutionResult(false, "Failed to search due to invalid tool arguments.", string.Empty, $"Failed to search: invalid tool arguments. Error: {findArgumentsError}. Usage: {{\"path\": \".\", \"pattern\": \"<search_pattern>\"}}");
        }

        if (string.IsNullOrWhiteSpace(findArguments.Pattern))
        {
            return new ToolExecutionResult(false, "Failed to search because 'pattern' parameter is missing.", string.Empty, "Failed to search: 'pattern' parameter is required. Specify the filename or directory substring to search for, e.g. {\"path\": \".\", \"pattern\": \"csproj\"}.");
        }

        string searchPath = string.IsNullOrWhiteSpace(findArguments.Path) ? "." : findArguments.Path;

        if (!_toolHelpers.TryGetRequiredPath(searchPath, out string? resolvedPath, out string? requiredPathError))
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
            bool truncated = false;

            foreach (string entry in Directory.EnumerateFileSystemEntries(resolvedPath, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), entry);

                if (_toolHelpers.IsPathIgnored(relativePath))
                {
                    continue;
                }

                if (Path.GetFileName(entry).Contains(findArguments.Pattern, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(relativePath);

                    if (matches.Count >= 50)
                    {
                        truncated = true;
                        break;
                    }
                }
            }

            StringBuilder resultBuilder = new();
            resultBuilder.AppendLine($"<observation tool=\"find\" pattern=\"{findArguments.Pattern}\" path=\"{searchPath}\" count=\"{matches.Count}\">");

            if (matches.Count <= 0)
            {
                resultBuilder.AppendLine($"No files or directories matching pattern '{findArguments.Pattern}' were found in '{searchPath}'.");
                resultBuilder.Append("</observation>");
                return new ToolExecutionResult(true, $"No matches found for '{findArguments.Pattern}' in '{searchPath}'.", resultBuilder.ToString(), string.Empty);
            }

            foreach (string match in matches)
            {
                resultBuilder.AppendLine(match);
            }

            if (truncated)
            {
                resultBuilder.AppendLine("[... truncated at 50 entries]");
            }

            resultBuilder.Append("</observation>");

            return new ToolExecutionResult(true, $"Found {matches.Count} match(es) for '{findArguments.Pattern}' in '{searchPath}'.", resultBuilder.ToString(), string.Empty);
        }
        catch (Exception exception)
        {
            return new ToolExecutionResult(false, $"An unexpected error occurred while searching in '{resolvedPath}'.", string.Empty, $"Failed to search: an unexpected error occurred. Error: {exception.Message}");
        }
    }

    internal record FindArguments(string Path = ".", string Pattern = "");
}
