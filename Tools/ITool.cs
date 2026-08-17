namespace Wayfare.Tools;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

public interface ITool
{
    string Name { get; }
    string DisplayName { get; }
    string Description { get; }

    string GetInvocationMessage(string arguments);
    Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken);
}

public sealed record ToolExecutionResult(
    bool Success,
    string DisplayMessage,
    string Result,
    string Error,
    [property: JsonIgnore] Exception? Exception = null,
    string ToolId = "",
    string ToolName = "");

public interface IToolHelpers
{
    bool TryDeserializeArguments<T>(string arguments, [NotNullWhen(true)] out T? args, [NotNullWhen(false)] out string? errorMessage) where T : class;
    bool TryGetRequiredPath(string path, [NotNullWhen(true)] out string? resolvedPath, [NotNullWhen(false)] out string? errorMessage);
    void EnsureDirectoryExists(string filePath);
}

public interface IToolManager
{
    IReadOnlyList<ITool> Tools { get; }
    IReadOnlyList<Exception> Errors { get; }

    Task LoadToolsAsync(string directoryPath, string searchPattern, string compiledDirectoryPath, CancellationToken cancellationToken);
    ITool GetTool(string name);
}
