namespace Wayfare.Infrastructure.Events;

public interface IEvent;

public interface IEventPublisher
{
    void Publish(IEvent @event);
}

public interface IEventBroker : IEventPublisher
{
    IAsyncEnumerable<IEvent> ReadAllAsync(CancellationToken cancellationToken);
    void Complete();
}

public sealed record StartupStartedEvent : IEvent;

public sealed record StartupCompletedEvent : IEvent;

public sealed record AgentStartedEvent : IEvent;

public sealed record ToolCompilationStartedEvent(string ToolName) : IEvent;

public sealed record ToolCompilationCompletedEvent : IEvent;

public sealed record ToolCompilationFailedEvent(string ToolName, string Error) : IEvent;

public sealed record ToolLoadingStartedEvent(string ToolName) : IEvent;

public sealed record ToolLoadingCompletedEvent : IEvent;

public sealed record ToolLoadingFailedEvent(string ToolName, string Error) : IEvent;

public sealed record ToolExecutionStartedEvent(string InvocationMessage) : IEvent;

public sealed record ToolExecutionCompletedEvent(
    bool Success,
    string DisplayMessage) : IEvent;

public sealed record ChatRequestStartedEvent(IReadOnlyList<string> ToolNames) : IEvent;

public sealed record ChatRequestCompletedEvent : IEvent;

public sealed record ThinkingChunkReceivedEvent(string Content) : IEvent;

public sealed record TokenChunkReceivedEvent(string Content) : IEvent;

public sealed record SquashingBranchEvent : IEvent;

public sealed record CycleCompletedEvent(string Objective, IReadOnlyList<string> Milestones) : IEvent;
