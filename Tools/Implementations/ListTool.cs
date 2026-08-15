using Wayfare.Core.Abstractions;
using Wayfare.Core.Models;

namespace Wayfare.Tools.Implementations;

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
            return new ToolExecutionResult(false, "Failed to list directory due to invalid tool arguments.", string.Empty, $"Failed to list directory: invalid tool arguments. Error: {listArgumentsError}");
        }

        if (!toolHelpers.TryGetRequiredPath(listArguments.Path, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, $"Failed to list: access denied or invalid path '{listArguments.Path}'.", string.Empty, $"Failed to list: access denied or invalid path '{listArguments.Path}'.");
        }

        if (!Directory.Exists(resolvedPath))
        {
            return new ToolExecutionResult(false, $"Failed to list: directory does not exist '{listArguments.Path}'.", string.Empty, $"Failed to list: directory does not exist at '{listArguments.Path}'.");
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

            string result = matches.Count <= 0
                ? $"Directory '{listArguments.Path}' is empty."
                : $"Directory entries in '{listArguments.Path}':{Environment.NewLine}{string.Join(Environment.NewLine, matches)}";

            return new ToolExecutionResult(true, $"Listed {matches.Count} entries in '{listArguments.Path}'.", result, string.Empty);
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult(false, $"An unexpected error occurred while listing directory '{listArguments.Path}'.", string.Empty, $"Failed to list directory: an unexpected error occurred. Error: {ex.Message}", ex);
        }
    }

    internal record ListFilesArguments(string Path);
}
