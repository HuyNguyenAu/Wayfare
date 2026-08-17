namespace Wayfare.Tools;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

public interface ITool
{
    string Name { get; }
    string DisplayName { get; }
    string Description { get; }
    ToolSchema Parameters { get; }

    string GetInvocationMessage(string arguments);
    Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken);
}

public sealed record ToolPropertySchema(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("description")] string Description)
{
    public static ToolPropertySchema String(string description) => new("string", description);
    public static ToolPropertySchema Integer(string description) => new("integer", description);
    public static ToolPropertySchema Boolean(string description) => new("boolean", description);
    public static ToolPropertySchema Number(string description) => new("number", description);
}

public sealed record ToolSchema(
    [property: JsonPropertyName("type")] string Type = "object",
    [property: JsonPropertyName("properties")] IReadOnlyDictionary<string, ToolPropertySchema>? Properties = null,
    [property: JsonPropertyName("required")] IReadOnlyList<string>? Required = null,
    [property: JsonPropertyName("additionalProperties")] bool AdditionalProperties = false)
{
    public static ToolSchema Object(
        IReadOnlyDictionary<string, ToolPropertySchema> properties,
        IReadOnlyList<string>? required = null)
    {
        return new ToolSchema(
            Type: "object",
            Properties: properties,
            Required: required ?? [.. properties.Keys],
            AdditionalProperties: false
        );
    }

    public BinaryData ToBinaryData() => BinaryData.FromObjectAsJson(this);
}

public sealed record ToolExecutionResult(
    bool Success,
    string DisplayMessage,
    string Result,
    string Error,
    [property: JsonIgnore] Exception? Exception = null,
    string ToolId = "",
    string ToolName = "");

public interface IToolHelpers
{
    bool TryDeserialiseArguments<T>(string arguments, [NotNullWhen(true)] out T? deserialisedArguments, [NotNullWhen(false)] out string? errorMessage) where T : class;
    bool TryGetRequiredPath(string path, [NotNullWhen(true)] out string? resolvedPath, [NotNullWhen(false)] out string? errorMessage);
    void EnsureDirectoryExists(string filePath);
}

public interface IToolManager
{
    IReadOnlyList<ITool> Tools { get; }
    IReadOnlyList<Exception> Errors { get; }

    Task LoadToolsAsync(string directoryPath, string searchPattern, string compiledDirectoryPath, CancellationToken cancellationToken);
    ITool GetTool(string name);
}
