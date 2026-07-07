namespace WayFare.Tools;

public sealed record ToolExecutionResult(bool Success, string DisplayMessage, string Result, string Error, Exception? Exception = null);

public interface ITool
{
    string Name { get; }
    string DisplayName { get; }
    string Description { get; }

    string GetInvocationMessage(string arguments);
    Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken);
}

