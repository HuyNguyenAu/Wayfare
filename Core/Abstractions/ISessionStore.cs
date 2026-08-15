using Wayfare.Core.Models.Messages;

namespace Wayfare.Core.Abstractions;

public interface ISessionStore
{
    string SessionId { get; }

    Task AppendMessageAsync(SessionMessage message, CancellationToken cancellationToken);
    Task<IReadOnlyList<SessionMessage>> LoadMessagesAsync(CancellationToken cancellationToken);
}
