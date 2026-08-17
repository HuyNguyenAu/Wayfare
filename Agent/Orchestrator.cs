namespace Wayfare.Agent;

using System.Text;
using Wayfare.Infrastructure.AI;
using Wayfare.Infrastructure.Events;
using Wayfare.Session;
using Wayfare.Tools;

public class Orchestrator(
    IChatClient chatClient,
    IToolManager toolManager,
    ISessionStore sessionStore,
    IEventPublisher eventPublisher,
    IBranchSquasher branchSquasher,
    IMessagePromptBuilder messagePromptBuilder,
    IIntentResolver intentResolver,
    IPivotDetector pivotDetector,
    ICircuitBreaker circuitBreaker) : IOrchestrator
{
    private readonly ISession _session = sessionStore.Session;

    #region Pipeline Entry Point

    public async Task RunCycleAsync(string userInput, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userInput);

        await InitialiseCycleAsync(userInput, cancellationToken);

        circuitBreaker.Reset();

        while (_session.State != SessionState.Done && !cancellationToken.IsCancellationRequested)
        {
            if (!circuitBreaker.TryAdvanceTurn(out string? limitExceededReason))
            {
                eventPublisher.Publish(new ToolExecutionCompletedEvent(false, "System", "Max turns reached", string.Empty, limitExceededReason));
                await CompleteCycleAsync(cancellationToken);
                break;
            }

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

    #endregion

    #region Phase 1: Initialise

    private async Task InitialiseCycleAsync(string userInput, CancellationToken cancellationToken)
    {
        if (pivotDetector.IsPivot(userInput) && HasActiveUnsquashedBranch(_session))
        {
            _session.SquashBranch($"Abandoned: {_session.Intent}. Reason: User pivoted to '{userInput}'.", BranchStatus.Abandoned);
        }

        string resolvedIntent = await intentResolver.ResolveAsync(_session.Intent, userInput, cancellationToken);
        _session.UpdateIntent(resolvedIntent);
        _session.StartBranch();
        _session.TransitionTo(SessionState.Thinking);

        UserMessage userMessage = new(userInput);
        _session.AppendTurn(userMessage);
        await sessionStore.SaveAsync(cancellationToken);
    }

    private static bool HasActiveUnsquashedBranch(ISession session)
    {
        return session.History.Count > 0 &&
               session.History[^1] is BranchNode branch &&
               string.IsNullOrWhiteSpace(branch.Summary) &&
               branch.Turns.Count > 0;
    }

    #endregion

    #region Phase 2: Thinking

    private async Task<ThinkingPhaseResult> ExecuteThinkingPhaseAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<string> toolNames = GetPreviousToolNames();
        eventPublisher.Publish(new ChatRequestStartedEvent(toolNames));

        StringBuilder assembledContent = new();
        Dictionary<int, ToolCallBuilder> toolCallBuilders = [];
        AgentFinishReason? finishReason = null;

        IReadOnlyList<SessionMessage> messages = messagePromptBuilder.BuildMessages(toolManager.Tools, _session.History, _session.Intent);

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

    #endregion

    #region Phase 3: Acting

    private async Task<IReadOnlyList<ToolExecutionResult>> ExecuteActingPhaseAsync(IReadOnlyList<ToolCall> toolCalls, CancellationToken cancellationToken)
    {
        _session.TransitionTo(SessionState.Acting);
        _session.AppendTurn(new ToolCallMessage(toolCalls));
        await sessionStore.SaveAsync(cancellationToken);

        IReadOnlyList<Task<ToolExecutionResult>> executionTasks = [.. toolCalls.Select(toolCall => ExecuteToolAsync(toolCall, cancellationToken))];
        ToolExecutionResult[] toolResults = await Task.WhenAll(executionTasks);

        circuitBreaker.RecordResults(toolCalls, toolResults);

        return toolResults;
    }

    private async Task<ToolExecutionResult> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        if (circuitBreaker.TryIntercept(toolCall, out ToolExecutionResult? interceptedResult))
        {
            eventPublisher.Publish(new ToolExecutionStartedEvent(toolCall.Arguments));
            eventPublisher.Publish(new ToolExecutionCompletedEvent(false, toolCall.Name, interceptedResult.DisplayMessage, string.Empty, interceptedResult.Error));
            return interceptedResult;
        }

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

            string descriptiveError = $"Exception occurred while executing tool '{toolName}': {ex.Message}";
            eventPublisher.Publish(new ToolExecutionCompletedEvent(false, toolName, $"An error occurred: {ex.Message}", string.Empty, descriptiveError));

            return new ToolExecutionResult(
                Success: false,
                DisplayMessage: $"An error occurred: {ex.Message}",
                Result: string.Empty,
                Error: descriptiveError,
                ToolId: toolCall.ToolId,
                ToolName: toolName,
                Exception: ex);
        }
    }

    #endregion

    #region Phase 4: Observing

    private async Task ExecuteObservingPhaseAsync(IReadOnlyList<ToolExecutionResult> toolResults, CancellationToken cancellationToken)
    {
        _session.TransitionTo(SessionState.Observing);
        _session.AppendTurn(new ToolResultMessage(toolResults));
        await sessionStore.SaveAsync(cancellationToken);
        _session.TransitionTo(SessionState.Thinking);
    }

    #endregion

    #region Phase 5: Finalise

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
        _session.SquashBranch(summary, BranchStatus.Completed);
        _session.UpdateIntent(string.Empty);
        await sessionStore.SaveAsync(cancellationToken);

        SessionProgress progress = _session.GetProgress();
        eventPublisher.Publish(new CycleCompletedEvent(progress.Objective, progress.Milestones));
    }

    #endregion

    #region Helpers & Inner Types

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

    #endregion
}
