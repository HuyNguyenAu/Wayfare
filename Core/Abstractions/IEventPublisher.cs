namespace Wayfare.Core.Abstractions;

public interface IEventPublisher
{
    void Publish(IEvent @event);
    ValueTask PublishAsync(IEvent @event, CancellationToken cancellationToken = default);
}
