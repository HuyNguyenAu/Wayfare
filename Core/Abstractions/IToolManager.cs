namespace Wayfare.Core.Abstractions;

public interface IToolManager
{
    IReadOnlyList<ITool> Tools { get; }
    IReadOnlyList<Exception> Errors { get; }

    Task LoadToolsAsync(string directoryPath, string searchPattern, string compiledDirectoryPath, CancellationToken cancellationToken);
    ITool GetTool(string name);
}
