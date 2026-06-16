namespace WayFare;

internal interface IAgentEvent;

// Agent startup events.
internal record StartupStarted() : IAgentEvent;
internal record StartupCompleted() : IAgentEvent;
internal record StartAgent() : IAgentEvent;

// Tool loading events.
internal record LoadingToolsStarted() : IAgentEvent;
internal record ToolCompilationStarted(string ToolName) : IAgentEvent;
internal record ToolCompilationCompleted() : IAgentEvent;
internal record ToolLoadingStarted(string ToolName) : IAgentEvent;
internal record ToolLoadingCompleted() : IAgentEvent;

// Agent execution events.
internal record ChatRequestStarted(string Description) : IAgentEvent;
internal record ChatRequestCompleted() : IAgentEvent;
internal record ThoughtChunkReceived(string Message) : IAgentEvent;
internal record ToolExecutionStarted(string InvocationMessage) : IAgentEvent;
internal record ToolExecutionCompleted(bool Success, string ToolName, string DisplayMessage, string Result, string Error) : IAgentEvent;

internal interface IAgentEventPublisher
{
    Task PublishAsync(IAgentEvent @event, CancellationToken cancellationToken);
}

internal interface IAgentEventSubscriber
{
   Task OnMessageAsync(IAgentEvent @event, CancellationToken cancellationToken);
}

internal sealed class AgentEventPublisher(IAgentEventSubscriber[] subscribers) : IAgentEventPublisher
{
    public async Task PublishAsync(IAgentEvent @event, CancellationToken cancellationToken)
    {
        foreach (IAgentEventSubscriber subscriber in subscribers)
        {
            await subscriber.OnMessageAsync(@event, cancellationToken);
        }
    }
}