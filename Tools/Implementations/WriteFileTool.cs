namespace Wayfare.Tools.Implementations;

using Wayfare.Tools;

internal sealed class WriteFileTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "write";
    public string DisplayName => "Write";
    public string Description => "Write content to a file, creating it if it does not exist or overwriting it if it does. Use this only for new files or full rewrites. To make targeted edits to an existing file, use replace instead. Parameters: path (string, required), content (string, required - the full content to write).";

    public ToolSchema Parameters => ToolSchema.Object(new Dictionary<string, ToolPropertySchema>
    {
        ["path"] = ToolPropertySchema.String("The path of the file to write."),
        ["content"] = ToolPropertySchema.String("The full content to write to the file.")
    });

    public string GetInvocationMessage(string arguments)
    {
        return toolHelpers.TryDeserialiseArguments(arguments, out WriteFileArguments? parsedArguments, out _)
            ? $"[{DisplayName}] [{parsedArguments.Path}]"
            : $"[{DisplayName}] [{arguments}]";
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!toolHelpers.TryDeserialiseArguments(arguments, out WriteFileArguments? writeFileArguments, out string? writeFileArgumentsError))
        {
            return new ToolExecutionResult(false, "Failed to write file due to invalid tool arguments.", string.Empty, $"Failed to write file: invalid tool arguments. Error: {writeFileArgumentsError}. Usage: {{\"path\": \"<file_path>\", \"content\": \"<file_content>\"}}");
        }

        if (!toolHelpers.TryGetRequiredPath(writeFileArguments.Path, out string? resolvedPath, out string? requiredPathError))
        {
            return new ToolExecutionResult(false, $"Failed to write file: access denied or invalid path '{writeFileArguments.Path}'.", string.Empty, $"Failed to write file: access denied or invalid path '{writeFileArguments.Path}'. Path must be within the working directory.");
        }

        try
        {
            toolHelpers.EnsureDirectoryExists(resolvedPath);

            await File.WriteAllTextAsync(resolvedPath, writeFileArguments.Content, cancellationToken);

            return new ToolExecutionResult(true, $"Successfully wrote content to file '{writeFileArguments.Path}'.", $"Successfully wrote all content to file '{writeFileArguments.Path}'.", string.Empty);
        }
        catch (Exception exception)
        {
            return new ToolExecutionResult(false, $"An unexpected error occurred while writing to file '{writeFileArguments.Path}'.", string.Empty, $"Failed to write file: an unexpected error occurred. Error: {exception.Message}", exception);
        }
    }

    internal record WriteFileArguments(string Path = "", string Content = "");
}
