namespace Wayfare.Tools;

using Wayfare.Infrastructure.Events;
using Wayfare.Tools.Implementations;

public sealed class ToolManager : IToolManager
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Exception> _errors = [];
    private readonly IEventPublisher _eventPublisher;
    private readonly IToolHelpers _toolHelpers;

    public ToolManager(
        IEventPublisher eventPublisher,
        IReadOnlyList<ITool> additionalTools,
        IToolHelpers toolHelpers)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _toolHelpers = toolHelpers ?? throw new ArgumentNullException(nameof(toolHelpers));

        RegisterDefaultTools();

        if (additionalTools is not null)
        {
            foreach (ITool tool in additionalTools)
            {
                RegisterTool(tool);
            }
        }
    }

    public IReadOnlyList<ITool> Tools => _tools.Values.ToList().AsReadOnly();
    public IReadOnlyList<Exception> Errors => _errors.AsReadOnly();

    public Task LoadToolsAsync(string directoryPath, string searchPattern, CancellationToken cancellationToken)
    {
        // Built-in tools are registered directly at construction.
        // Publish events to notify UI of cultivated toolchain modules.
        foreach (ITool tool in _tools.Values)
        {
            _eventPublisher.Publish(new ToolLoadingStartedEvent(tool.DisplayName));
            _eventPublisher.Publish(new ToolLoadingCompletedEvent());
        }

        return Task.CompletedTask;
    }

    public void RegisterTool(ITool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        _tools[tool.Name] = tool;
    }

    public ITool GetTool(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _tools.TryGetValue(name, out ITool? tool)
            ? tool
            : throw new KeyNotFoundException($"No tool found with name '{name}'");
    }

    private void RegisterDefaultTools()
    {
        RegisterTool(new ReadFileTool(_toolHelpers));
        RegisterTool(new WriteFileTool(_toolHelpers));
        RegisterTool(new ReplaceTool(_toolHelpers));
        RegisterTool(new FindTool(_toolHelpers));
        RegisterTool(new ListTool(_toolHelpers));
        RegisterTool(new ExecuteCommandTool(_toolHelpers));
    }
}
