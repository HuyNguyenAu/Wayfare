namespace Wayfare.Core.Abstractions;

public interface ISessionStore : IAsyncDisposable
{
    ISession Session { get; }

    Task SaveAsync(CancellationToken cancellationToken);
}
