using System.Diagnostics.CodeAnalysis;
using Wayfare.Core.Models;

namespace Wayfare.Core.Abstractions;

public interface ITool
{
    string Name { get; }
    string DisplayName { get; }
    string Description { get; }

    string GetInvocationMessage(string arguments);
    Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken);
}

public interface IToolHelpers
{
    bool TryDeserializeArguments<T>(string arguments, [NotNullWhen(true)] out T? args, [NotNullWhen(false)] out string? errorMessage) where T : class;
    bool TryGetRequiredPath(string? path, [NotNullWhen(true)] out string? resolvedPath, [NotNullWhen(false)] out string? errorMessage);
    void EnsureDirectoryExists(string filePath);
}
