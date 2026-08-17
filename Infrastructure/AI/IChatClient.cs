namespace Wayfare.Infrastructure.AI;

using Wayfare.Session;
using Wayfare.Tools;

public interface IChatClient
{
    IAsyncEnumerable<StreamingChatUpdate> StreamChatAsync(
        IReadOnlyList<SessionMessage> messages,
        IReadOnlyList<ITool> tools,
        CancellationToken cancellationToken);

    Task<ChatCompletionResult> CompleteChatAsync(
        IReadOnlyList<SessionMessage> messages,
        IReadOnlyList<ITool> tools,
        CancellationToken cancellationToken);
}
