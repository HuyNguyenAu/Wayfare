using System.Collections.Concurrent;

namespace WayFare;

// Tool loading events
internal record LoadingToolsStarted();
internal record ToolCompilationStarted(string ToolName);
internal record ToolCompilationCompleted();
internal record ToolLoadingStarted(string ToolName);
internal record ToolLoadingCompleted();

// Agent execution events
internal record ChatRequestStarted(string Description);
internal record ChatRequestCompleted();
internal record ThoughtChunkReceived(string Message);
internal record ToolExecutionStarted(string InvocationMessage);
internal record ToolExecutionCompleted(bool Success, string ToolName, string DisplayMessage, string Result, string Error);

internal interface IEventPublisher
{
    void Publish<T>(T @event);
}

internal interface IEventSubscriber
{
    void Subscribe<T>(Action<T> handler);
}

internal class AgentEventHub : IEventPublisher, IEventSubscriber
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public void Publish<T>(T @event)
    {
        if (@event == null) return;

        if (_handlers.TryGetValue(typeof(T), out var handlers))
        {
            List<Delegate> targets;
            lock (handlers)
            {
                targets = [.. handlers];
            }

            foreach (var target in targets)
            {
                if (target is Action<T> action)
                {
                    action(@event);
                }
            }
        }
    }

    public void Subscribe<T>(Action<T> handler)
    {
        var handlers = _handlers.GetOrAdd(typeof(T), _ => []);
        lock (handlers)
        {
            handlers.Add(handler);
        }
    }
}
