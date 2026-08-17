namespace Wayfare.Agent;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Wayfare.Infrastructure.AI;
using Wayfare.Infrastructure.Events;
using Wayfare.Session;
using Wayfare.Session.Inspection;
using Wayfare.Tools;

public sealed class Orchestrator(
    IChatClient chatClient,
    IToolManager toolManager,
    ISessionStore sessionStore,
    IEventPublisher eventPublisher,
    IBranchSquasher branchSquasher,
    ICircuitBreaker circuitBreaker,
    IMessagePromptBuilder? messagePromptBuilder = null,
    IPivotDetector? pivotDetector = null,
    ISessionInspector? sessionInspector = null) : IOrchestrator
{
    private readonly IChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    private readonly IToolManager _toolManager = toolManager ?? throw new ArgumentNullException(nameof(toolManager));
    private readonly ISessionStore _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
    private readonly IEventPublisher _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    private readonly IBranchSquasher _branchSquasher = branchSquasher ?? throw new ArgumentNullException(nameof(branchSquasher));
    private readonly ICircuitBreaker _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
    private readonly IMessagePromptBuilder _messagePromptBuilder = messagePromptBuilder ?? new MessagePromptBuilder();
    private readonly IPivotDetector _pivotDetector = pivotDetector ?? new PivotDetector();
    private readonly ISessionInspector _sessionInspector = sessionInspector ?? new SessionInspector();
    private readonly ISession _session = (sessionStore ?? throw new ArgumentNullException(nameof(sessionStore))).Session;

    public async Task RunCycleAsync(string userInput, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userInput);

        // Stage 1: Initialise
        await InitialiseCycleAsync(userInput, cancellationToken);
        _circuitBreaker.Reset();

        // 5-Stage ReAct execution loop
        while (_session.State != SessionState.Done && !cancellationToken.IsCancellationRequested)
        {
            if (!_circuitBreaker.TryAdvanceTurn(out string? limitExceededReason))
            {
                _eventPublisher.Publish(new ToolExecutionCompletedEvent(false, limitExceededReason));
                await CompleteCycleAsync(cancellationToken);
                break;
            }

            // Stage 2: Think
            ThinkingPhaseResult thinkingResult = await ExecuteThinkingPhaseAsync(cancellationToken);

            if (thinkingResult.HasToolCalls)
            {
                // Stage 3: Act
                IReadOnlyList<ToolExecutionResult> toolResults = await ExecuteActingPhaseAsync(thinkingResult.ToolCalls, cancellationToken);

                // Stage 4: Observe
                await ExecuteObservingPhaseAsync(toolResults, cancellationToken);
            }
            else if (thinkingResult.IsCompleted)
            {
                await CompleteCycleAsync(cancellationToken);
            }
        }

        // Stage 5: Finalise
        await FinaliseCycleAsync(cancellationToken);
    }

    private async Task InitialiseCycleAsync(string userInput, CancellationToken cancellationToken)
    {
        if (_pivotDetector.IsPivot(userInput) && HasActiveUnsquashedBranch(_session))
        {
            _session.SquashBranch($"Abandoned. Reason: User pivoted to '{userInput}'.", BranchStatus.Abandoned);
        }

        _session.StartBranch();
        _session.TransitionTo(SessionState.Thinking);

        UserMessage userMessage = new(userInput);
        _session.AppendTurn(userMessage);
        await _sessionStore.SaveAsync(cancellationToken);
    }

    private static bool HasActiveUnsquashedBranch(ISession session)
    {
        return session.History.Count > 0 &&
               session.History[^1] is BranchContainerNode branch &&
               string.IsNullOrWhiteSpace(branch.Summary) &&
               branch.Turns.Count > 0;
    }

    private async Task<ThinkingPhaseResult> ExecuteThinkingPhaseAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<string> toolNames = GetPreviousToolNames();
        _eventPublisher.Publish(new ChatRequestStartedEvent(toolNames));

        StringBuilder assembledContent = new();
        Dictionary<string, ToolCallBuilder> toolCallBuilders = [];
        ChatFinishReason? finishReason = null;
        string? activeCallKey = null;

        IReadOnlyList<SessionMessage> sessionMessages = _messagePromptBuilder.BuildMessages(_session.History);
        List<ChatMessage> chatMessages = SessionMessageMapper.ToChatMessages(sessionMessages);
        ChatOptions chatOptions = new()
        {
            Tools = _toolManager.Tools.ToAITools()
        };

        await foreach (ChatResponseUpdate update in _chatClient.GetStreamingResponseAsync(chatMessages, chatOptions, cancellationToken))
        {
            foreach (AIContent content in update.Contents)
            {
                if (content is ReasoningContent reasoning)
                {
                    _eventPublisher.Publish(new ThinkingChunkReceivedEvent(reasoning.Text));
                }
                else if (content is FunctionCallContent functionCall)
                {
                    string key = !string.IsNullOrEmpty(functionCall.CallId) ? functionCall.CallId : (activeCallKey ?? Guid.NewGuid().ToString());

                    if (!toolCallBuilders.TryGetValue(key, out ToolCallBuilder? builder))
                    {
                        builder = new ToolCallBuilder();
                        toolCallBuilders[key] = builder;
                        activeCallKey = key;
                    }

                    if (!string.IsNullOrEmpty(functionCall.CallId))
                    {
                        builder.ToolId.Append(functionCall.CallId);
                    }

                    if (!string.IsNullOrEmpty(functionCall.Name))
                    {
                        builder.Name.Append(functionCall.Name);
                    }

                    if (functionCall.Arguments is not null)
                    {
                        builder.SerialisedArguments = JsonSerializer.Serialize(functionCall.Arguments);
                    }
                    else if (functionCall.RawRepresentation is OpenAI.Chat.StreamingChatToolCallUpdate streamingToolCallChunk && !string.IsNullOrEmpty(streamingToolCallChunk.FunctionArgumentsUpdate?.ToString()))
                    {
                        builder.RawArguments.Append(streamingToolCallChunk.FunctionArgumentsUpdate.ToString());
                    }
                }
            }

            if (!string.IsNullOrEmpty(update.Text))
            {
                assembledContent.Append(update.Text);
                _eventPublisher.Publish(new TokenChunkReceivedEvent(update.Text));
            }

            if (update.FinishReason is not null)
            {
                finishReason = update.FinishReason;
            }
        }

        _eventPublisher.Publish(new ChatRequestCompletedEvent());

        AssistantMessage assistantMessage = new(assembledContent.ToString());
        _session.AppendTurn(assistantMessage);
        await _sessionStore.SaveAsync(cancellationToken);

        IReadOnlyList<ToolCall> toolCalls = [.. toolCallBuilders.Values.Select(builder => new ToolCall(
            builder.ToolId.ToString(),
            builder.Name.ToString(),
            !string.IsNullOrEmpty(builder.SerialisedArguments) ? builder.SerialisedArguments : builder.RawArguments.ToString()
        ))];

        return new ThinkingPhaseResult(toolCalls, finishReason);
    }

    private async Task<IReadOnlyList<ToolExecutionResult>> ExecuteActingPhaseAsync(IReadOnlyList<ToolCall> toolCalls, CancellationToken cancellationToken)
    {
        _session.TransitionTo(SessionState.Acting);
        _session.AppendTurn(new ToolCallMessage(toolCalls));
        await _sessionStore.SaveAsync(cancellationToken);

        IReadOnlyList<Task<ToolExecutionResult>> executionTasks = [.. toolCalls.Select(toolCall => ExecuteToolAsync(toolCall, cancellationToken))];
        ToolExecutionResult[] toolResults = await Task.WhenAll(executionTasks);

        _circuitBreaker.RecordResults(toolCalls, toolResults);

        return toolResults;
    }

    private async Task<ToolExecutionResult> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        if (_circuitBreaker.TryIntercept(toolCall, out ToolExecutionResult? interceptedResult))
        {
            _eventPublisher.Publish(new ToolExecutionStartedEvent(toolCall.Arguments));
            _eventPublisher.Publish(new ToolExecutionCompletedEvent(false, interceptedResult.DisplayMessage));
            return interceptedResult;
        }

        bool started = false;
        string toolName = toolCall.Name;

        try
        {
            ITool tool = _toolManager.GetTool(toolName);
            string invocationMessage = tool.GetInvocationMessage(toolCall.Arguments);

            _eventPublisher.Publish(new ToolExecutionStartedEvent(invocationMessage));
            started = true;

            ToolExecutionResult result = await tool.ExecuteAsync(toolCall.Arguments, cancellationToken);

            _eventPublisher.Publish(new ToolExecutionCompletedEvent(result.Success, result.DisplayMessage));

            return result with { ToolId = toolCall.ToolId, ToolName = toolName };
        }
        catch (Exception exception)
        {
            if (!started)
            {
                string message = $"[{toolName}] [{toolCall.Arguments}]";
                _eventPublisher.Publish(new ToolExecutionStartedEvent(message));
            }

            string descriptiveError = $"Exception occurred while executing tool '{toolName}': {exception.Message}";
            _eventPublisher.Publish(new ToolExecutionCompletedEvent(false, $"An error occurred: {exception.Message}"));

            return new ToolExecutionResult(
                Success: false,
                DisplayMessage: $"An error occurred: {exception.Message}",
                Result: string.Empty,
                Error: descriptiveError,
                ToolId: toolCall.ToolId,
                ToolName: toolName);
        }
    }

    private async Task ExecuteObservingPhaseAsync(IReadOnlyList<ToolExecutionResult> toolResults, CancellationToken cancellationToken)
    {
        _session.TransitionTo(SessionState.Observing);
        _session.AppendTurn(new ToolResultMessage(toolResults));
        await _sessionStore.SaveAsync(cancellationToken);
        _session.TransitionTo(SessionState.Thinking);
    }

    private async Task CompleteCycleAsync(CancellationToken cancellationToken)
    {
        _session.TransitionTo(SessionState.Done);
        await _sessionStore.SaveAsync(cancellationToken);
    }

    private async Task FinaliseCycleAsync(CancellationToken cancellationToken)
    {
        _session.TransitionTo(SessionState.Idle);

        _eventPublisher.Publish(new SquashingBranchEvent());

        string summary = await _branchSquasher.SquashAsync(_session, cancellationToken);
        _session.SquashBranch(summary, BranchStatus.Completed);
        await _sessionStore.SaveAsync(cancellationToken);

        SessionAuditReport auditReport = _sessionInspector.GenerateAuditReport(_session);
        SessionProgress progress = _session.GetProgress();
        _eventPublisher.Publish(new CycleCompletedEvent(progress.Milestones, auditReport));
    }

    private IReadOnlyList<string> GetPreviousToolNames() =>
        _session.GetLastMessage() is ToolResultMessage toolResult
            ? [.. toolResult.Results.Select(r => r.ToolName)]
            : [];

    private sealed record ThinkingPhaseResult(
        IReadOnlyList<ToolCall> ToolCalls,
        ChatFinishReason? FinishReason)
    {
        public bool HasToolCalls => ToolCalls.Count > 0;
        public bool IsCompleted => FinishReason == ChatFinishReason.Stop || FinishReason == ChatFinishReason.Length;
    }

    private sealed class ToolCallBuilder
    {
        public StringBuilder ToolId { get; } = new();
        public StringBuilder Name { get; } = new();
        public string? SerialisedArguments { get; set; }
        public StringBuilder RawArguments { get; } = new();
    }
}
