using Wayfare.Core.Abstractions;
using Wayfare.Core.Models;

namespace Wayfare.Tools.Implementations;

internal sealed class ReadFileTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "read";
    public string DisplayName => "Read";
    public string Description => "Read lines from a file and return them as text. Parameters: path (string, required), offset (int, optional - first line to return, 0-based, default 0), limit (int, optional - maximum number of lines to return, default 2000). If the file has more lines than limit, call again with a higher offset to read the rest.";

    public string GetInvocationMessage(string arguments)
    {
        return toolHelpers.TryDeserializeArguments(arguments, out ReadFileArguments? args, out _)
            ? $"[{DisplayName}] [{args.Path}] [Offset {args.Offset}] [Limit {args.Limit}]"
            : $"[{DisplayName}] [{arguments}]";
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out ReadFileArguments? readFileArguments, out string? readFileArgumentsError))
        {
            return new ToolExecutionResult(false, "Failed to read file due to invalid tool arguments.", string.Empty, $"Failed to read file: invalid tool arguments. Error: {readFileArgumentsError}");
        }

        if (readFileArguments.Offset < 0)
        {
            return new ToolExecutionResult(false, "Failed to read file because 'offset' cannot be negative.", string.Empty, "Failed to read file: 'offset' parameter must be non-negative.");
        }

        if (readFileArguments.Limit < 0)
        {
            return new ToolExecutionResult(false, "Failed to read file because 'limit' cannot be negative.", string.Empty, "Failed to read file: 'limit' parameter must be non-negative.");
        }

        if (!toolHelpers.TryGetRequiredPath(readFileArguments.Path, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, $"Failed to read file: access denied or invalid path '{readFileArguments.Path}'.", string.Empty, $"Failed to read file: access denied or invalid path '{readFileArguments.Path}'.");
        }

        if (!File.Exists(resolvedPath))
        {
            return new ToolExecutionResult(false, $"Failed to read file: file does not exist '{readFileArguments.Path}'.", string.Empty, $"Failed to read file: file does not exist at '{readFileArguments.Path}'.");
        }

        try
        {
            string[] lines = await File.ReadAllLinesAsync(resolvedPath, cancellationToken);
            string[] slice = [.. lines.Skip(readFileArguments.Offset).Take(readFileArguments.Limit)];

            bool hasMoreLines = readFileArguments.Offset + slice.Length < lines.Length;
            string result = $"[File: {readFileArguments.Path}, Offset: {readFileArguments.Offset}, Lines Read: {slice.Length}, Total Lines: {lines.Length}, Has More Lines: {(hasMoreLines ? "True" : "False")}]{Environment.NewLine}{string.Join(Environment.NewLine, slice)}";

            return new ToolExecutionResult(true, $"Read {slice.Length} lines from '{readFileArguments.Path}'.", result, string.Empty);
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult(false, $"An unexpected error occurred while reading file '{readFileArguments.Path}'.", string.Empty, $"Failed to read file: an unexpected error occurred. Error: {ex.Message}", ex);
        }
    }

    internal record ReadFileArguments(string Path = "", int Offset = 0, int Limit = 2000);
}
