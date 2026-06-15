using System.Threading.Channels;

namespace WayFare;

internal interface IAgentEvent;

// Tool loading events
internal record LoadingToolsStarted() : IAgentEvent;
internal record ToolCompilationStarted(string ToolName) : IAgentEvent;
internal record ToolCompilationCompleted() : IAgentEvent;
internal record ToolCompilationFailed(string ToolName, string Error) : IAgentEvent;
internal record ToolLoadingStarted(string ToolName) : IAgentEvent;
internal record ToolLoadingCompleted() : IAgentEvent;
internal record ToolLoadingFailed(string ToolName, string Error) : IAgentEvent;

// Agent execution events
internal record ChatRequestStarted(string Description) : IAgentEvent;
internal record ChatRequestCompleted() : IAgentEvent;
internal record ThoughtChunkReceived(string Message) : IAgentEvent;
internal record ToolExecutionStarted(string InvocationMessage) : IAgentEvent;
internal record ToolExecutionCompleted(bool Success, string ToolName, string DisplayMessage, string Result, string Error) : IAgentEvent;

internal interface IEventPublisher
{
    void Publish<T>(T @event) where T : IAgentEvent;
}

internal class AgentEventHub : IEventPublisher
{
    private readonly Channel<IAgentEvent> _channel = Channel.CreateUnbounded<IAgentEvent>();

    public ChannelReader<IAgentEvent> Reader => _channel.Reader;

    public void Publish<T>(T @event) where T : IAgentEvent
    {
        _channel.Writer.TryWrite(@event);
    }
}
