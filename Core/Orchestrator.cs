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
    ISession session,
    IChatClient chatClient,
    IToolManager toolManager,
    ISessionStore sessionStore,
    IEventPublisher eventPublisher) : IOrchestrator
{
    public async Task RunCycleAsync(string userInput, CancellationToken cancellationToken)
    {
        session.BeginThinking(userInput);
        await sessionStore.AppendMessageAsync(session.Messages[^1], cancellationToken);
        eventPublisher.Publish(new SessionUpdatedEvent());

        while (session.State != SessionState.Done && !cancellationToken.IsCancellationRequested)
        {
            IReadOnlyList<string> toolNames = [];

            if (session.Messages[^1] is ToolResultMessage toolResultMessage)
            {
                List<string> names = new(toolResultMessage.Results.Count);

                for (int i = 0; i < toolResultMessage.Results.Count; i++)
                {
                    names.Add(toolResultMessage.Results[i].ToolName);
                }

                toolNames = names;
            }

            eventPublisher.Publish(new ChatRequestStartedEvent(toolNames));

            StringBuilder assembledContent = new();
            Dictionary<int, ToolCallBuilder> toolCallBuilders = [];
            AgentFinishReason? finishReason = null;

            await foreach (StreamingChatUpdate update in chatClient.StreamChatAsync(session.Messages, toolManager.Tools, cancellationToken))
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
            session.RecordThought(assembledContent.ToString());
            await sessionStore.AppendMessageAsync(session.Messages[^1], cancellationToken);
            eventPublisher.Publish(new SessionUpdatedEvent());

            IReadOnlyList<ToolCall> toolCalls = [.. toolCallBuilders.Values.Select(builder => new ToolCall(builder.ToolId.ToString(), builder.Name.ToString(), builder.Args.ToString()))];

            if (toolCalls.Count > 0)
            {
                session.RequestAction(toolCalls);
                await sessionStore.AppendMessageAsync(session.Messages[^1], cancellationToken);
                eventPublisher.Publish(new SessionUpdatedEvent());

                IReadOnlyList<Task<ToolExecutionResult>> executionTasks = [.. toolCalls.Select(tc => ExecuteToolAsync(tc, cancellationToken))];
                IReadOnlyList<ToolExecutionResult> toolResults = await Task.WhenAll(executionTasks);

                session.RecordObservation(toolResults);
                await sessionStore.AppendMessageAsync(session.Messages[^1], cancellationToken);
                session.ResumeThinking();
                eventPublisher.Publish(new SessionUpdatedEvent());
            }
            else if (finishReason == AgentFinishReason.Stop || finishReason == AgentFinishReason.Length)
            {
                session.Finish();
                eventPublisher.Publish(new SessionUpdatedEvent());
            }
        }

        session.Idle();
        eventPublisher.Publish(new SessionUpdatedEvent());
    }

    private async Task<ToolExecutionResult> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        bool started = false;
        string toolName = toolCall.Name;

        try
        {
            ITool tool = toolManager.GetTool(toolName);
            string invocationMsg = tool.GetInvocationMessage(toolCall.Arguments);

            eventPublisher.Publish(new ToolExecutionStartedEvent(invocationMsg));
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

    private sealed class ToolCallBuilder
    {
        public StringBuilder ToolId { get; } = new();
        public StringBuilder Name { get; } = new();
        public StringBuilder Args { get; } = new();
    }
}
