namespace Wayfare.Tools.Implementations;

using System.Text;
using Wayfare.Tools;

internal sealed class ListTool(IToolHelpers toolHelpers) : ITool
{
    private readonly IToolHelpers _toolHelpers = toolHelpers ?? throw new ArgumentNullException(nameof(toolHelpers));

    public string Name => "list";
    public string DisplayName => "List";
    public string Description => "List the files and directories inside a directory. Directory names end with a path separator. Parameters: path (string, required - the directory to list)";

    public ToolSchema Parameters => ToolSchema.Object(new Dictionary<string, ToolPropertySchema>
    {
        ["path"] = ToolPropertySchema.String("The directory path to list.")
    });

    public string GetInvocationMessage(string arguments)
    {
        return _toolHelpers.TryDeserialiseArguments(arguments, out ListFilesArguments? parsedArguments, out _)
            ? $"[{DisplayName}] [{parsedArguments.Path}]"
            : $"[{DisplayName}] [{arguments}]";
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!_toolHelpers.TryDeserialiseArguments(arguments, out ListFilesArguments? listArguments, out string? listArgumentsError))
        {
            return new ToolExecutionResult(false, "Failed to list directory due to invalid tool arguments.", string.Empty, $"Failed to list directory: invalid tool arguments. Error: {listArgumentsError}. Usage: {{\"path\": \"<directory_path>\"}}");
        }

        if (!_toolHelpers.TryGetRequiredPath(listArguments.Path, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, $"Failed to list: access denied or invalid path '{listArguments.Path}'.", string.Empty, $"Failed to list: access denied or invalid path '{listArguments.Path}'. Path must be within the working directory.");
        }

        if (!Directory.Exists(resolvedPath))
        {
            return new ToolExecutionResult(false, $"Failed to list: directory does not exist '{listArguments.Path}'.", string.Empty, $"Failed to list: directory does not exist at '{listArguments.Path}'. Use 'find' or 'list' with path \".\" to view existing directories.");
        }

        try
        {
            List<string> matches = [];
            bool truncated = false;

            foreach (string entry in Directory.EnumerateFileSystemEntries(resolvedPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), entry);
                bool isDirectory = Directory.Exists(entry);

                if (_toolHelpers.IsPathIgnored(relativePath))
                {
                    continue;
                }

                string name = Path.GetFileName(entry);

                if (isDirectory)
                {
                    name += Path.DirectorySeparatorChar;
                }

                matches.Add(name);

                if (matches.Count >= 100)
                {
                    truncated = true;
                    break;
                }
            }

            StringBuilder resultBuilder = new();
            resultBuilder.AppendLine("<observation tool=\"list\">");

            if (matches.Count <= 0)
            {
                resultBuilder.AppendLine($"Directory '{listArguments.Path}' is empty.");
            }
            else
            {
                resultBuilder.AppendLine($"Directory entries in '{listArguments.Path}':");

                foreach (string match in matches)
                {
                    resultBuilder.AppendLine(match);
                }

                if (truncated)
                {
                    resultBuilder.AppendLine("[... truncated at 100 entries]");
                }
            }

            resultBuilder.Append("</observation>");

            return new ToolExecutionResult(true, $"Listed {matches.Count} entries in '{listArguments.Path}'.", resultBuilder.ToString(), string.Empty);
        }
        catch (Exception exception)
        {
            return new ToolExecutionResult(false, $"An unexpected error occurred while listing directory '{listArguments.Path}'.", string.Empty, $"Failed to list directory: an unexpected error occurred. Error: {exception.Message}");
        }
    }

    internal record ListFilesArguments(string Path = "");
}
