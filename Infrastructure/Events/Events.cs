namespace Wayfare.Infrastructure.Events;

using Wayfare.Session.Inspection;

public abstract record AgentEvent;

public interface IEventPublisher
{
    void Publish(AgentEvent @event);
}

public interface IEventBroker : IEventPublisher
{
    IAsyncEnumerable<AgentEvent> ReadAllAsync(CancellationToken cancellationToken);
    void Complete();
}

public sealed record StartupStartedEvent : AgentEvent;
public sealed record StartupCompletedEvent : AgentEvent;
public sealed record AgentStartedEvent : AgentEvent;

public sealed record ToolCompilationStartedEvent(string ToolName) : AgentEvent;
public sealed record ToolCompilationCompletedEvent : AgentEvent;
public sealed record ToolCompilationFailedEvent(string ToolName, string Error) : AgentEvent;

public sealed record ToolLoadingStartedEvent(string ToolName) : AgentEvent;
public sealed record ToolLoadingCompletedEvent : AgentEvent;
public sealed record ToolLoadingFailedEvent(string ToolName, string Error) : AgentEvent;

public sealed record ToolExecutionStartedEvent(string InvocationMessage) : AgentEvent;
public sealed record ToolExecutionCompletedEvent(bool Success, string DisplayMessage) : AgentEvent;

public sealed record ChatRequestStartedEvent(IReadOnlyList<string> ToolNames) : AgentEvent;
public sealed record ChatRequestCompletedEvent : AgentEvent;

public sealed record ThinkingChunkReceivedEvent(string Content) : AgentEvent;
public sealed record TokenChunkReceivedEvent(string Content) : AgentEvent;

public sealed record SquashingBranchEvent : AgentEvent;
public sealed record CycleCompletedEvent(
    IReadOnlyList<string> Milestones,
    SessionAuditReport AuditReport) : AgentEvent;
