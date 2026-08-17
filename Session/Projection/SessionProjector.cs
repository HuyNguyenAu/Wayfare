namespace Wayfare.Session.Projection;

using Wayfare.Session;

public interface ISessionProjector
{
    IReadOnlyList<SessionMessage> ProjectMessages(ISession session);
    IReadOnlyList<SessionMessage> ProjectMessages(IReadOnlyList<HistoryNode> nodes);
}

public sealed class SessionProjector : ISessionProjector
{
    public IReadOnlyList<SessionMessage> ProjectMessages(ISession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return ProjectMessages(session.LinearTrunk);
    }

    public IReadOnlyList<SessionMessage> ProjectMessages(IReadOnlyList<HistoryNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        List<SessionMessage> messages = new(nodes.Count);

        for (int i = 0; i < nodes.Count; i++)
        {
            messages.Add(nodes[i].ToProjectedMessage());
        }

        return messages.AsReadOnly();
    }
}
