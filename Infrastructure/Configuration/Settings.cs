namespace Wayfare.Infrastructure.Configuration;

public sealed record Settings
{
    public required string ModelName { get; init; }
    public required string ApiKey { get; init; }
    public required string Endpoint { get; init; }
    public required string ToolsPath { get; init; }
    public required string CompiledDirectory { get; init; }
    public required string SessionsDirectory { get; init; }

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
            SessionsDirectory = GetValue("SESSIONS_DIRECTORY")
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
}
