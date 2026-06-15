namespace WayFare.Tools;

internal sealed class ReadFileTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "read";
    public string DisplayName => "Read";
    public string Description => "Reads lines from a file. If the file has more lines than the limit, make subsequent calls with an increased offset. Expects a JSON object with properties: \"path\" (string, required - the path of the file to read), \"offset\" (integer, optional - the 0-based index of the first line to return, defaults to 0), \"limit\" (integer, optional - the maximum number of lines to return, defaults to 2000). Example: {\"path\": \"Program.cs\", \"offset\": 0, \"limit\": 100}";

    public string GetInvocationMessage(string arguments)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out ReadFileArguments? readFileArguments, out string? readFileArgumentsError))
        {
            throw new ArgumentException($"Failed to deserialise arguments for {Name} tool. Error: {readFileArgumentsError}. Arguments: {arguments}");
        }

        return $"[{DisplayName}] [{readFileArguments.Path}] [Offset {readFileArguments.Offset}] [Limit {readFileArguments.Limit}]";
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

            return new ToolExecutionResult(true, $"Read {slice.Length} lines from '{readFileArguments.Path}' (offset: {readFileArguments.Offset}, limit: {readFileArguments.Limit}).", result, string.Empty);
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult(false, $"An unexpected error occurred while reading file '{readFileArguments.Path}'.", string.Empty, $"Failed to read file: an unexpected error occurred. Error: {ex.Message}", ex);
        }
    }

    internal record ReadFileArguments(string Path, int Offset = 0, int Limit = 2000);
}

