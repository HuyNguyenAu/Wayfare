using System.Text;
using Wayfare.Core.Abstractions;
using Wayfare.Core.Events;
using Wayfare.Core.Models;
using Wayfare.Core.Models.Messages;

namespace Wayfare.Core;

public interface IOrchestrator
{
    Task RunCycleAsync(string userInput, CancellationToken cancellationToken);
}

public class Orchestrator(
    IChatClient chatClient,
    IToolManager toolManager,
    ISessionStore sessionStore,
    IEventPublisher eventPublisher,
    IBranchSquasher branchSquasher,
    IMessagePromptBuilder messagePromptBuilder) : IOrchestrator
{
    private readonly ISession _session = sessionStore.Session;

    public async Task RunCycleAsync(string userInput, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userInput);

        await InitialiseCycleAsync(userInput, cancellationToken);

        while (_session.State != SessionState.Done && !cancellationToken.IsCancellationRequested)
        {
            ThinkingPhaseResult thinkingResult = await ExecuteThinkingPhaseAsync(cancellationToken);

            if (thinkingResult.HasToolCalls)
            {
                IReadOnlyList<ToolExecutionResult> toolResults = await ExecuteActingPhaseAsync(thinkingResult.ToolCalls, cancellationToken);
                await ExecuteObservingPhaseAsync(toolResults, cancellationToken);
            }
            else if (thinkingResult.IsCompleted)
            {
                await CompleteCycleAsync(cancellationToken);
            }
        }

        await FinaliseCycleAsync(cancellationToken);
    }

    private async Task InitialiseCycleAsync(string userInput, CancellationToken cancellationToken)
    {
        _session.StartBranch();
        _session.TransitionTo(SessionState.Thinking);

        UserMessage userMessage = new(userInput);
        _session.AppendTurn(userMessage);
        await sessionStore.SaveAsync(cancellationToken);
    }

    private async Task<ThinkingPhaseResult> ExecuteThinkingPhaseAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<string> toolNames = GetPreviousToolNames();
        eventPublisher.Publish(new ChatRequestStartedEvent(toolNames));

        StringBuilder assembledContent = new();
        Dictionary<int, ToolCallBuilder> toolCallBuilders = [];
        AgentFinishReason? finishReason = null;

        IReadOnlyList<SessionMessage> messages = messagePromptBuilder.BuildMessages(toolManager.Tools, _session.History);

        await foreach (StreamingChatUpdate update in chatClient.StreamChatAsync(messages, toolManager.Tools, cancellationToken))
        {
            if (update.ContentUpdate is not null)
            {
                assembledContent.Append(update.ContentUpdate);
                eventPublisher.Publish(new TokenChunkReceivedEvent(update.ContentUpdate));
            }

            if (update.ToolCallUpdate is not null)
            {
                StreamingToolCallChunk toolCallUpdate = update.ToolCallUpdate;

                if (!toolCallBuilders.TryGetValue(toolCallUpdate.Index, out ToolCallBuilder? builder))
                {
                    builder = new ToolCallBuilder();
                    toolCallBuilders[toolCallUpdate.Index] = builder;
                }

                if (!string.IsNullOrEmpty(toolCallUpdate.ToolId))
                {
                    builder.ToolId.Append(toolCallUpdate.ToolId);
                }

                if (!string.IsNullOrEmpty(toolCallUpdate.FunctionName))
                {
                    builder.Name.Append(toolCallUpdate.FunctionName);
                }

                if (!string.IsNullOrEmpty(toolCallUpdate.FunctionArgumentsUpdate))
                {
                    builder.Args.Append(toolCallUpdate.FunctionArgumentsUpdate);
                }
            }

            if (update.FinishReason is not null)
            {
                finishReason = update.FinishReason;
            }
        }

        eventPublisher.Publish(new ChatRequestCompletedEvent());

        AssistantMessage assistantMessage = new(assembledContent.ToString());
        _session.AppendTurn(assistantMessage);
        await sessionStore.SaveAsync(cancellationToken);

        IReadOnlyList<ToolCall> toolCalls = [.. toolCallBuilders.Values.Select(builder => new ToolCall(builder.ToolId.ToString(), builder.Name.ToString(), builder.Args.ToString()))];

        return new ThinkingPhaseResult(toolCalls, finishReason);
    }

    private async Task<IReadOnlyList<ToolExecutionResult>> ExecuteActingPhaseAsync(IReadOnlyList<ToolCall> toolCalls, CancellationToken cancellationToken)
    {
        _session.TransitionTo(SessionState.Acting);
        _session.AppendTurn(new ToolCallMessage(toolCalls));
        await sessionStore.SaveAsync(cancellationToken);

        IReadOnlyList<Task<ToolExecutionResult>> executionTasks = [.. toolCalls.Select(toolCall => ExecuteToolAsync(toolCall, cancellationToken))];
        return await Task.WhenAll(executionTasks);
    }

    private async Task ExecuteObservingPhaseAsync(IReadOnlyList<ToolExecutionResult> toolResults, CancellationToken cancellationToken)
    {
        _session.TransitionTo(SessionState.Observing);
        _session.AppendTurn(new ToolResultMessage(toolResults));
        await sessionStore.SaveAsync(cancellationToken);
        _session.TransitionTo(SessionState.Thinking);
    }

    private async Task CompleteCycleAsync(CancellationToken cancellationToken)
    {
        _session.TransitionTo(SessionState.Done);
        await sessionStore.SaveAsync(cancellationToken);
    }

    private async Task FinaliseCycleAsync(CancellationToken cancellationToken)
    {
        _session.TransitionTo(SessionState.Idle);

        eventPublisher.Publish(new SquashingBranchEvent());

        string summary = await branchSquasher.SquashAsync(_session, cancellationToken);
        _session.SquashBranch(summary);
        await sessionStore.SaveAsync(cancellationToken);

        SessionProgress progress = _session.GetProgress();
        eventPublisher.Publish(new CycleCompletedEvent(progress.Objective, progress.Milestones));
    }

    private IReadOnlyList<string> GetPreviousToolNames()
    {
        if (_session.GetLastMessage() is ToolResultMessage previousToolResultMessage)
        {
            List<string> names = new(previousToolResultMessage.Results.Count);

            for (int i = 0; i < previousToolResultMessage.Results.Count; i++)
            {
                names.Add(previousToolResultMessage.Results[i].ToolName);
            }

            return names;
        }

        return [];
    }

    private async Task<ToolExecutionResult> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        bool started = false;
        string toolName = toolCall.Name;

        try
        {
            ITool tool = toolManager.GetTool(toolName);
            string invocationMessage = tool.GetInvocationMessage(toolCall.Arguments);

            eventPublisher.Publish(new ToolExecutionStartedEvent(invocationMessage));
            started = true;

            ToolExecutionResult result = await tool.ExecuteAsync(toolCall.Arguments, cancellationToken);

            eventPublisher.Publish(new ToolExecutionCompletedEvent(result.Success, toolName, result.DisplayMessage, result.Result, result.Error));

            return result with { ToolId = toolCall.ToolId, ToolName = toolName };
        }
        catch (Exception ex)
        {
            if (!started)
            {
                string message = $"[{toolName}] [{toolCall.Arguments}]";
                eventPublisher.Publish(new ToolExecutionStartedEvent(message));
            }

            eventPublisher.Publish(new ToolExecutionCompletedEvent(false, toolName, $"An error occurred: {ex.Message}", string.Empty, ex.ToString()));

            return new ToolExecutionResult(
                Success: false,
                DisplayMessage: $"An error occurred: {ex.Message}",
                Result: string.Empty,
                Error: $"Exception occurred while executing tool '{toolName}' because {ex}",
                ToolId: toolCall.ToolId,
                ToolName: toolName,
                Exception: ex);
        }
    }

    private sealed record ThinkingPhaseResult(
        IReadOnlyList<ToolCall> ToolCalls,
        AgentFinishReason? FinishReason)
    {
        public bool HasToolCalls => ToolCalls.Count > 0;
        public bool IsCompleted => FinishReason is AgentFinishReason.Stop or AgentFinishReason.Length;
    }

    private sealed class ToolCallBuilder
    {
        public StringBuilder ToolId { get; } = new();
        public StringBuilder Name { get; } = new();
        public StringBuilder Args { get; } = new();
    }
}
