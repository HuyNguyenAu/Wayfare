using Wayfare.Core.Models;
using Wayfare.Core.Models.Messages;

namespace Wayfare.Core.Abstractions;

public interface IChatClient
{
    IAsyncEnumerable<StreamingChatUpdate> StreamChatAsync(
        IReadOnlyList<SessionMessage> messages,
        IReadOnlyList<ITool> tools,
        CancellationToken cancellationToken = default);
}
