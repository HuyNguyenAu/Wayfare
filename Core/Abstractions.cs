using System.Diagnostics.CodeAnalysis;
using Wayfare.Core.Models;

namespace Wayfare.Core;

#region Events & Messaging Contracts

public interface IEvent;

public interface IEventPublisher
{
    void Publish(IEvent @event);
    ValueTask PublishAsync(IEvent @event, CancellationToken cancellationToken);
}

public interface IEventBroker : IEventPublisher
{
    IAsyncEnumerable<IEvent> ReadAllAsync(CancellationToken cancellationToken);
    void Complete();
}

#endregion

#region LLM & Model Contracts

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

#endregion

#region Session & Storage Contracts

public interface ISession
{
    SessionState State { get; }
    string Intent { get; }
    IReadOnlyList<HistoryNode> History { get; }

    void StartBranch();
    void AppendTurn(SessionMessage message);
    void SquashBranch(string summary, BranchStatus status);
    void UpdateIntent(string intent);
    SessionMessage GetLastMessage();
    SessionProgress GetProgress();
    void TransitionTo(SessionState newState);
}

public interface ISessionStore : IAsyncDisposable
{
    ISession Session { get; }
    Task SaveAsync(CancellationToken cancellationToken);
}

#endregion

#region Tool Contracts

public interface ITool
{
    string Name { get; }
    string DisplayName { get; }
    string Description { get; }

    string GetInvocationMessage(string arguments);
    Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken);
}

public interface IToolHelpers
{
    bool TryDeserializeArguments<T>(string arguments, [NotNullWhen(true)] out T? args, [NotNullWhen(false)] out string? errorMessage) where T : class;
    bool TryGetRequiredPath(string path, [NotNullWhen(true)] out string? resolvedPath, [NotNullWhen(false)] out string? errorMessage);
    void EnsureDirectoryExists(string filePath);
}

public interface IToolManager
{
    IReadOnlyList<ITool> Tools { get; }
    IReadOnlyList<Exception> Errors { get; }

    Task LoadToolsAsync(string directoryPath, string searchPattern, string compiledDirectoryPath, CancellationToken cancellationToken);
    ITool GetTool(string name);
}

#endregion

#region Prompt & Pipeline Contracts

public interface IBranchSquasher
{
    Task<string> SquashAsync(ISession session, CancellationToken cancellationToken);
}

public interface IMessagePromptBuilder
{
    IReadOnlyList<SessionMessage> BuildMessages(IReadOnlyList<ITool> tools, IReadOnlyList<HistoryNode> history, string intent);
}

public interface IIntentResolver
{
    Task<string> ResolveAsync(string currentIntent, string userInput, CancellationToken cancellationToken);
}

public interface IPivotDetector
{
    bool IsPivot(string userInput);
}

public interface IOrchestrator
{
    Task RunCycleAsync(string userInput, CancellationToken cancellationToken);
}

#endregion
