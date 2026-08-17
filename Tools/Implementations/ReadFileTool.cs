namespace Wayfare.Tools.Implementations;

using Wayfare.Tools;

internal sealed class ReadFileTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "read";
    public string DisplayName => "Read";
    public string Description => "Read lines from a file and return them as text. Parameters: path (string, required), offset (int, optional - first line to return, 0-based, default 0), limit (int, optional - maximum number of lines to return, default 2000). If the file has more lines than limit, call again with a higher offset to read the rest.";

    public ToolSchema Parameters => ToolSchema.Object(new Dictionary<string, ToolPropertySchema>
    {
        ["path"] = ToolPropertySchema.String("The path of the file to read."),
        ["offset"] = ToolPropertySchema.Integer("The 0-based line number to start reading from. Defaults to 0."),
        ["limit"] = ToolPropertySchema.Integer("Maximum number of lines to return. Defaults to 2000.")
    });

    public string GetInvocationMessage(string arguments)
    {
        return toolHelpers.TryDeserialiseArguments(arguments, out ReadFileArguments? parsedArguments, out _)
            ? $"[{DisplayName}] [{parsedArguments.Path}] [Offset {parsedArguments.Offset}] [Limit {parsedArguments.Limit}]"
            : $"[{DisplayName}] [{arguments}]";
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!toolHelpers.TryDeserialiseArguments(arguments, out ReadFileArguments? readFileArguments, out string? readFileArgumentsError))
        {
            return new ToolExecutionResult(false, "Failed to read file due to invalid tool arguments.", string.Empty, $"Failed to read file: invalid tool arguments. Error: {readFileArgumentsError}. Usage: {{\"path\": \"<file_path>\", \"offset\": 0, \"limit\": 2000}}");
        }

        if (readFileArguments.Offset < 0)
        {
            return new ToolExecutionResult(false, "Failed to read file because 'offset' cannot be negative.", string.Empty, "Failed to read file: 'offset' parameter must be non-negative (0 or greater). Usage: {\"path\": \"<path>\", \"offset\": 0, \"limit\": 2000}.");
        }

        if (readFileArguments.Limit < 0)
        {
            return new ToolExecutionResult(false, "Failed to read file because 'limit' cannot be negative.", string.Empty, "Failed to read file: 'limit' parameter must be non-negative. Usage: {\"path\": \"<path>\", \"offset\": 0, \"limit\": 2000}.");
        }

        if (!toolHelpers.TryGetRequiredPath(readFileArguments.Path, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, $"Failed to read file: access denied or invalid path '{readFileArguments.Path}'.", string.Empty, $"Failed to read file: access denied or invalid path '{readFileArguments.Path}'. Path must be within the working directory.");
        }

        if (!File.Exists(resolvedPath))
        {
            return new ToolExecutionResult(false, $"Failed to read file: file does not exist '{readFileArguments.Path}'.", string.Empty, $"Failed to read file: file does not exist at '{readFileArguments.Path}'. Use 'find' or 'list' to verify existing file paths before reading.");
        }

        try
        {
            string[] lines = await File.ReadAllLinesAsync(resolvedPath, cancellationToken);
            string[] lineSlice = [.. lines.Skip(readFileArguments.Offset).Take(readFileArguments.Limit)];

            bool hasMoreLines = readFileArguments.Offset + lineSlice.Length < lines.Length;
            string result = $"[File: {readFileArguments.Path}, Offset: {readFileArguments.Offset}, Lines Read: {lineSlice.Length}, Total Lines: {lines.Length}, Has More Lines: {(hasMoreLines ? "True" : "False")}]{Environment.NewLine}{string.Join(Environment.NewLine, lineSlice)}";

            return new ToolExecutionResult(true, $"Read {lineSlice.Length} lines from '{readFileArguments.Path}'.", result, string.Empty);
        }
        catch (Exception exception)
        {
            return new ToolExecutionResult(false, $"An unexpected error occurred while reading file '{readFileArguments.Path}'.", string.Empty, $"Failed to read file: an unexpected error occurred. Error: {exception.Message}", exception);
        }
    }

    internal record ReadFileArguments(string Path = "", int Offset = 0, int Limit = 2000);
}
