namespace Wayfare.Core.Abstractions;

public interface ISessionStore
{
    ISession Session { get; }

    Task SaveAsync(CancellationToken cancellationToken);
}
