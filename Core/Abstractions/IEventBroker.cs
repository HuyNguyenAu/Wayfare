namespace Wayfare.Core.Abstractions;

public interface IEventBroker : IEventPublisher
{
    IAsyncEnumerable<IEvent> ReadAllAsync(CancellationToken cancellationToken);
    void Complete();
}
