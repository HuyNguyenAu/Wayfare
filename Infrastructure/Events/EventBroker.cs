using System.Threading.Channels;
using Wayfare.Core.Abstractions;

namespace Wayfare.Infrastructure.Events;

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
        _channel.Writer.TryWrite(@event);
    }

    public ValueTask PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(@event, cancellationToken);
    }

    public IAsyncEnumerable<IEvent> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public void Complete()
    {
        _channel.Writer.TryComplete();
    }
}
