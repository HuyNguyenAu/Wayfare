namespace Wayfare.Infrastructure.Events;

using System.Threading.Channels;

public sealed class EventBroker : IEventBroker
{
    private readonly Channel<IEvent> _channel;

    public EventBroker()
    {
        _channel = Channel.CreateUnbounded<IEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public void Publish(IEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        _channel.Writer.TryWrite(@event);
    }

    public IAsyncEnumerable<IEvent> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public void Complete()
    {
        _channel.Writer.TryComplete();
    }
}
