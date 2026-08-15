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

        string modelName = GetValue("MODEL_NAME", "MODEL_NAME")
            ?? throw new InvalidOperationException("Model name must be specified in MODEL_NAME or MODEL_NAME environment variable.");

        string apiKey = GetValue("API_KEY", "API_KEY")
            ?? throw new InvalidOperationException("API key must be specified in API_KEY or API_KEY environment variable.");

        string endpoint = GetValue("ENDPOINT", "ENDPOINT")
            ?? throw new InvalidOperationException("Endpoint must be specified in ENDPOINT or ENDPOINT environment variable.");

        string toolsPath = GetValue("TOOLS_PATH", "TOOLS_PATH")
            ?? throw new InvalidOperationException("Tools path must be specified in TOOLS_PATH or TOOLS_PATH environment variable.");

        string compiledDirectory = GetValue("COMPILED_DIRECTORY", "COMPILED_DIRECTORY")
            ?? throw new InvalidOperationException("Compiled directory must be specified in COMPILED_DIRECTORY or COMPILED_DIRECTORY environment variable.");

        string sessionsDirectory = GetValue("SESSIONS_DIRECTORY", "SESSIONS_DIRECTORY")
            ?? throw new InvalidOperationException("Sessions directory must be specified in SESSIONS_DIRECTORY or SESSIONS_DIRECTORY environment variable.");

        return new Settings
        {
            ModelName = modelName,
            ApiKey = apiKey,
            Endpoint = endpoint,
            ToolsPath = toolsPath,
            CompiledDirectory = compiledDirectory,
            SessionsDirectory = sessionsDirectory
        };
    }

    private static string? GetValue(string primaryKey, string fallbackKey)
    {
        string? value = Environment.GetEnvironmentVariable(primaryKey);

        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        value = Environment.GetEnvironmentVariable(fallbackKey);

        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"Environment variable '{primaryKey}' or '{fallbackKey}' must be set.");
    }
}
