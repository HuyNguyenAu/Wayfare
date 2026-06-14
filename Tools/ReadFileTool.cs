namespace WayFare.Tools;

internal sealed class ReadFileTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "Read File";
    public string Description => "Read lines from a file and return them as text. Parameters: path (string, required), offset (int, optional - first line to return, 0-based, default 0), limit (int, optional - maximum number of lines to return, default 2000). If the file has more lines than limit, call again with a higher offset to read the rest.";

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out ReadFileArguments? readFileArguments, out string? readFileArgumentsError))
        {
            return new ToolExecutionResult(false, string.Empty, readFileArgumentsError);
        }

        if (!toolHelpers.TryGetRequiredPath(readFileArguments.Path, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, string.Empty, $"Failed to read file because {requiredPathError}");
        }

        if (!File.Exists(resolvedPath))
        {
            return new ToolExecutionResult(false, string.Empty, $"Failed to read file because file does not exist at path '{readFileArguments.Path}'");
        }

        try
        {
            string[] lines = await File.ReadAllLinesAsync(resolvedPath, cancellationToken);
            string[] slice = [.. lines.Skip(readFileArguments.Offset).Take(readFileArguments.Limit)];

            return new ToolExecutionResult(true, string.Join(Environment.NewLine, slice), string.Empty);
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult(false, string.Empty, $"Failed to read file because {ex}", ex);
        }
    }

    internal record ReadFileArguments(string Path, int Offset = 0, int Limit = 2000);
}

