namespace Wayfare.Tools;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

public sealed class ToolHelpers(IReadOnlyList<string> excludedDirectories) : IToolHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HashSet<string> _excludedDirectories = new(
        excludedDirectories ?? throw new ArgumentNullException(nameof(excludedDirectories)),
        StringComparer.OrdinalIgnoreCase);

    public bool IsPathIgnored(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        string[] segments = relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

        return segments.Any(_excludedDirectories.Contains);
    }

    public bool TryDeserialiseArguments<T>(string arguments, [NotNullWhen(true)] out T? deserialisedArguments, [NotNullWhen(false)] out string? errorMessage) where T : class
    {
        ArgumentNullException.ThrowIfNull(arguments);

        try
        {
            T? parsedObject = JsonSerializer.Deserialize<T>(arguments, JsonOptions);

            if (parsedObject is null)
            {
                deserialisedArguments = null;
                errorMessage = "Arguments are missing. Provide the required parameters as JSON.";

                return false;
            }

            deserialisedArguments = parsedObject;
            errorMessage = null;

            return true;
        }
        catch (JsonException)
        {
            deserialisedArguments = default;
            errorMessage = "Arguments are not valid JSON.";

            return false;
        }
        catch (Exception exception)
        {
            deserialisedArguments = default;
            errorMessage = $"Failed to parse arguments: {exception.Message}";

            return false;
        }
    }

    public bool TryGetRequiredPath(string path, [NotNullWhen(true)] out string? resolvedPath, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            resolvedPath = null;
            errorMessage = "'path' is required.";

            return false;
        }

        string baseDirectory = Directory.GetCurrentDirectory();
        string fullPath = Path.GetFullPath(path, baseDirectory);

        bool isSubPath = fullPath.StartsWith($"{baseDirectory}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
        bool isSamePath = string.Equals(fullPath, baseDirectory, StringComparison.OrdinalIgnoreCase);

        if (isSubPath || isSamePath)
        {
            resolvedPath = fullPath;
            errorMessage = null;

            return true;
        }

        resolvedPath = null;
        errorMessage = $"Access denied: '{path}' is outside the current working directory.";

        return false;
    }

    public void EnsureDirectoryExists(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
