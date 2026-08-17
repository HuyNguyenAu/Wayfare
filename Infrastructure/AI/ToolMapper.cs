namespace Wayfare.Infrastructure.AI;

using System.Text.Json;
using Microsoft.Extensions.AI;
using Wayfare.Tools;

public sealed class ToolAIFunction(ITool tool) : AIFunction
{
    private readonly ITool _tool = tool ?? throw new ArgumentNullException(nameof(tool));
    private readonly JsonElement _jsonSchema = ParseSchema(tool);

    public override string Name => _tool.Name;
    public override string Description => _tool.Description;
    public override JsonElement JsonSchema => _jsonSchema;

    private static JsonElement ParseSchema(ITool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        using JsonDocument jsonDocument = JsonDocument.Parse(tool.Parameters.ToBinaryData());
        return jsonDocument.RootElement.Clone();
    }

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        string serialisedArguments = JsonSerializer.Serialize(arguments);
        return await _tool.ExecuteAsync(serialisedArguments, cancellationToken);
    }
}

public static class ToolMapper
{
    public static AIFunction ToAIFunction(this ITool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        return new ToolAIFunction(tool);
    }

    public static IList<AITool> ToAITools(this IReadOnlyList<ITool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);
        return [.. tools.Select(tool => tool.ToAIFunction())];
    }
}
