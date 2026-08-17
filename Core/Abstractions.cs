using System.Diagnostics.CodeAnalysis;
using Wayfare.Core.Models;
using Wayfare.Core.Models.Ast;
using Wayfare.Core.Models.Messages;

namespace Wayfare.Core.Abstractions;

#region Events & Messaging Contracts

/// <summary>
/// Marker interface for all system events.
/// </summary>
public interface IEvent;

/// <summary>
/// Interface for publishing events asynchronously or synchronously.
/// </summary>
public interface IEventPublisher
{
    void Publish(IEvent @event);
    ValueTask PublishAsync(IEvent @event, CancellationToken cancellationToken);
}

/// <summary>
/// Central event bus interface supporting publish/subscribe streaming.
/// </summary>
public interface IEventBroker : IEventPublisher
{
    IAsyncEnumerable<IEvent> ReadAllAsync(CancellationToken cancellationToken);
    void Complete();
}

#endregion

#region LLM & Model Contracts

/// <summary>
/// Client abstraction for communicating with language models.
/// </summary>
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

/// <summary>
/// In-memory session representation managing AST branches and state.
/// </summary>
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

/// <summary>
/// Persistent storage mechanism for session state.
/// </summary>
public interface ISessionStore : IAsyncDisposable
{
    ISession Session { get; }
    Task SaveAsync(CancellationToken cancellationToken);
}

#endregion

#region Tool Contracts

/// <summary>
/// Plugin contract for executable agent tools.
/// </summary>
public interface ITool
{
    string Name { get; }
    string DisplayName { get; }
    string Description { get; }

    string GetInvocationMessage(string arguments);
    Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken);
}

/// <summary>
/// Helper utilities provided to dynamic and built-in tools.
/// </summary>
public interface IToolHelpers
{
    bool TryDeserializeArguments<T>(string arguments, [NotNullWhen(true)] out T? args, [NotNullWhen(false)] out string? errorMessage) where T : class;
    bool TryGetRequiredPath(string path, [NotNullWhen(true)] out string? resolvedPath, [NotNullWhen(false)] out string? errorMessage);
    void EnsureDirectoryExists(string filePath);
}

/// <summary>
/// Registry and dynamic compilation manager for tools.
/// </summary>
public interface IToolManager
{
    IReadOnlyList<ITool> Tools { get; }
    IReadOnlyList<Exception> Errors { get; }

    Task LoadToolsAsync(string directoryPath, string searchPattern, string compiledDirectoryPath, CancellationToken cancellationToken);
    ITool GetTool(string name);
}

#endregion

#region Prompt & Pipeline Contracts

/// <summary>
/// Compresses completed session branches into STARL milestones.
/// </summary>
public interface IBranchSquasher
{
    Task<string> SquashAsync(ISession session, CancellationToken cancellationToken);
}

/// <summary>
/// Constructs LLM prompt messages from session history and active intent.
/// </summary>
public interface IMessagePromptBuilder
{
    IReadOnlyList<SessionMessage> BuildMessages(IReadOnlyList<ITool> tools, IReadOnlyList<HistoryNode> history, string intent);
}

/// <summary>
/// Resolves new user intents against existing session objectives.
/// </summary>
public interface IIntentResolver
{
    Task<string> ResolveAsync(string currentIntent, string userInput, CancellationToken cancellationToken);
}

/// <summary>
/// Detects topic switches or task pivots in user input.
/// </summary>
public interface IPivotDetector
{
    bool IsPivot(string userInput);
}

/// <summary>
/// Orchestrates the 5-phase agent lifecycle.
/// </summary>
public interface IOrchestrator
{
    Task RunCycleAsync(string userInput, CancellationToken cancellationToken);
}

#endregion
