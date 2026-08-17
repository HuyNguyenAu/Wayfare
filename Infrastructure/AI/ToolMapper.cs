namespace Wayfare.Infrastructure.AI;

using System.Text.Json;
using Microsoft.Extensions.AI;
using Wayfare.Tools;

public sealed class ToolAIFunction : AIFunction
{
    private readonly ITool _tool;
    private readonly JsonElement _jsonSchema;

    public ToolAIFunction(ITool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        _tool = tool;
        using JsonDocument jsonDocument = JsonDocument.Parse(tool.Parameters.ToBinaryData());
        _jsonSchema = jsonDocument.RootElement.Clone();
    }

    public override string Name => _tool.Name;
    public override string Description => _tool.Description;
    public override JsonElement JsonSchema => _jsonSchema;

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        string serializedArguments = JsonSerializer.Serialize(arguments);
        return await _tool.ExecuteAsync(serializedArguments, cancellationToken);
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
