namespace Wayfare.Infrastructure.Configuration;

public sealed record Settings
{
    public required string ModelName { get; init; }
    public required string ApiKey { get; init; }
    public required string Endpoint { get; init; }
    public required string ToolsPath { get; init; }
    public required string CompiledDirectory { get; init; }
    public required string SessionsDirectory { get; init; }
    public int MaxTurns { get; init; } = 15;

    public static Settings FromEnvironment()
    {
        DotNetEnv.Env.Load();

        return new Settings
        {
            ModelName = GetValue("MODEL_NAME"),
            ApiKey = GetValue("API_KEY"),
            Endpoint = GetValue("ENDPOINT"),
            ToolsPath = GetValue("TOOLS_PATH"),
            CompiledDirectory = GetValue("COMPILED_DIRECTORY"),
            SessionsDirectory = GetValue("SESSIONS_DIRECTORY"),
            MaxTurns = GetOptionalInt("MAX_TURNS", 15)
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

    private static int GetOptionalInt(string key, int defaultValue)
    {
        string? value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (int.TryParse(value, out int result) && result > 0)
        {
            return result;
        }

        throw new InvalidOperationException($"Environment variable '{key}' must be a positive integer.");
    }
}
