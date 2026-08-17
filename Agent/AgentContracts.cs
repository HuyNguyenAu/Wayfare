namespace Wayfare.Agent;

using Wayfare.Infrastructure.AI;
using Wayfare.Session;
using Wayfare.Tools;

public interface IOrchestrator
{
    Task RunCycleAsync(string userInput, CancellationToken cancellationToken);
}

public interface IBranchSquasher
{
    Task<string> SquashAsync(ISession session, CancellationToken cancellationToken);
}

public interface IIntentResolver
{
    Task<string> ResolveAsync(string currentIntent, string userInput, CancellationToken cancellationToken);
}

public interface IPivotDetector
{
    bool IsPivot(string userInput);
}

public interface IMessagePromptBuilder
{
    IReadOnlyList<SessionMessage> BuildMessages(IReadOnlyList<ITool> tools, IReadOnlyList<HistoryNode> history, string intent);
}
