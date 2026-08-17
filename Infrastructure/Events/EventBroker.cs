namespace Wayfare.Infrastructure.Events;

using System.Threading.Channels;

public sealed class EventBroker : IEventBroker
{
    private readonly Channel<AgentEvent> _channel;

    public EventBroker()
    {
        _channel = Channel.CreateUnbounded<AgentEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public void Publish(AgentEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        _channel.Writer.TryWrite(@event);
    }

    public IAsyncEnumerable<AgentEvent> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public void Complete()
    {
        _channel.Writer.TryComplete();
    }
}
