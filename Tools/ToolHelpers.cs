using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Wayfare.Core;

namespace Wayfare.Tools;

public class ToolHelpers : IToolHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public bool TryDeserializeArguments<T>(string arguments, [NotNullWhen(true)] out T? args, [NotNullWhen(false)] out string? errorMessage) where T : class
    {
        ArgumentNullException.ThrowIfNull(arguments);

        try
        {
            T? result = JsonSerializer.Deserialize<T>(arguments, JsonOptions);

            if (result is null)
            {
                args = null;
                errorMessage = "Arguments are missing. Provide the required parameters as JSON.";

                return false;
            }

            args = result;
            errorMessage = null;

            return true;
        }
        catch (JsonException)
        {
            args = default;
            errorMessage = "Arguments are not valid JSON.";

            return false;
        }
        catch (Exception ex)
        {
            args = default;
            errorMessage = $"Failed to parse arguments: {ex}";

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
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
