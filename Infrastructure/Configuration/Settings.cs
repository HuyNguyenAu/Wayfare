namespace Wayfare.Infrastructure.Configuration;

public sealed record Settings
{
    public static readonly IReadOnlyList<string> DefaultExcludedDirectories = [".git", "bin", "obj", "node_modules", ".vs"];

    public required string ModelName { get; init; }
    public required string ApiKey { get; init; }
    public required string Endpoint { get; init; }
    public required string ToolsPath { get; init; }
    public required string SessionsDirectory { get; init; }
    public int MaxTurns { get; init; } = 15;
    public IReadOnlyList<string> ExcludedDirectories { get; init; } = DefaultExcludedDirectories;

    public static Settings FromEnvironment()
    {
        DotNetEnv.Env.Load();

        return new Settings
        {
            ModelName = GetValue("MODEL_NAME"),
            ApiKey = GetValue("API_KEY"),
            Endpoint = GetValue("ENDPOINT"),
            ToolsPath = GetValue("TOOLS_PATH"),
            SessionsDirectory = GetValue("SESSIONS_DIRECTORY"),
            MaxTurns = GetOptionalInt("MAX_TURNS", 15),
            ExcludedDirectories = GetOptionalStringList("EXCLUDED_DIRECTORIES", DefaultExcludedDirectories)
        };
    }

    private static string GetValue(string key)
    {
        string? value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Environment variable '{key}' must be set.");
        }

        return value;
    }

    private static int GetOptionalInt(string key, int defaultValue, bool allowZero = false)
    {
        string? value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (int.TryParse(value, out int parsedInt) && (allowZero ? parsedInt >= 0 : parsedInt > 0))
        {
            return parsedInt;
        }

        throw new InvalidOperationException($"Environment variable '{key}' must be a {(allowZero ? "non-negative" : "positive")} integer.");
    }

    private static IReadOnlyList<string> GetOptionalStringList(string key, IReadOnlyList<string> defaultValue)
    {
        string? value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return [.. value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }
}
