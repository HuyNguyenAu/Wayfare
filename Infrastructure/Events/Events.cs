namespace Wayfare.Infrastructure.Events;

using Wayfare.Session.Inspection;

public interface IEvent
{
    Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken);
}

public interface IEventSink
{
    Task HandleAsync(StartupStartedEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(StartupCompletedEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(AgentStartedEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(ToolCompilationStartedEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(ToolCompilationCompletedEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(ToolCompilationFailedEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(ToolLoadingStartedEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(ToolLoadingCompletedEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(ToolLoadingFailedEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(ChatRequestStartedEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(ChatRequestCompletedEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(ThinkingChunkReceivedEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(TokenChunkReceivedEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(ToolExecutionStartedEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(ToolExecutionCompletedEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(SquashingBranchEvent @event, CancellationToken cancellationToken);
    Task HandleAsync(CycleCompletedEvent @event, CancellationToken cancellationToken);
}

public interface IEventPublisher
{
    void Publish(IEvent @event);
}

public interface IEventBroker : IEventPublisher
{
    IAsyncEnumerable<IEvent> ReadAllAsync(CancellationToken cancellationToken);
    void Complete();
}

public sealed record StartupStartedEvent : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record StartupCompletedEvent : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record AgentStartedEvent : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record ToolCompilationStartedEvent(string ToolName) : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record ToolCompilationCompletedEvent : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record ToolCompilationFailedEvent(string ToolName, string Error) : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record ToolLoadingStartedEvent(string ToolName) : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record ToolLoadingCompletedEvent : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record ToolLoadingFailedEvent(string ToolName, string Error) : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record ToolExecutionStartedEvent(string InvocationMessage) : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record ToolExecutionCompletedEvent(
    bool Success,
    string DisplayMessage) : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record ChatRequestStartedEvent(IReadOnlyList<string> ToolNames) : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record ChatRequestCompletedEvent : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record ThinkingChunkReceivedEvent(string Content) : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record TokenChunkReceivedEvent(string Content) : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record SquashingBranchEvent : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}

public sealed record CycleCompletedEvent(
    IReadOnlyList<string> Milestones,
    SessionAuditReport AuditReport) : IEvent
{
    public Task DispatchAsync(IEventSink sink, CancellationToken cancellationToken) => sink.HandleAsync(this, cancellationToken);
}
