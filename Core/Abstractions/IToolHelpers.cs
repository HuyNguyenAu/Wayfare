using System.Diagnostics.CodeAnalysis;

namespace Wayfare.Core.Abstractions;

public interface IToolHelpers
{
    bool TryDeserializeArguments<T>(string arguments, [NotNullWhen(true)] out T? args, [NotNullWhen(false)] out string? errorMessage) where T : class;
    bool TryGetRequiredPath(string path, [NotNullWhen(true)] out string? resolvedPath, [NotNullWhen(false)] out string? errorMessage);
    void EnsureDirectoryExists(string filePath);
}
