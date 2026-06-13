namespace WayFare.Tools;

public sealed record ToolExecutionResult(bool Success, string Result, string Error, Exception? Exception = null);

public interface ITool
{
    string Name { get; }
    string Description { get; }

    Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken);
}

