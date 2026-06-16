using WayFare.Tools;

namespace WayFare;

internal interface IEngine
{
    Task RunCycleAsync(string userInput, CancellationToken cancellationToken);
}

internal class Engine(ISession session, IChatClient chatClient, IAgentEventPublisher agentEventPublisher) : IEngine
{
    public async Task RunCycleAsync(string userInput, CancellationToken cancellationToken)
    {
        session.BeginThinking(userInput);

        while (session.State != State.Done && !cancellationToken.IsCancellationRequested)
        {
            string statusDescription = "Compiling decision...";
            
            if (session.Messages.LastOrDefault() is ToolResultMessage toolResultMessage)
            {
                string[] toolNames = [.. toolResultMessage.ToolResults.Select(r => r.ToolName).Distinct()];
                statusDescription = $"Processing results from {string.Join(", ", toolNames)}...";
            }

            await agentEventPublisher.PublishAsync(new ChatRequestStarted(statusDescription), cancellationToken);
            ChatResponse chatResponse = await chatClient.ChatAsync(
                [.. session.Messages],
                [.. session.GetTools()],
                content => agentEventPublisher.PublishAsync(new ThoughtChunkReceived(content), cancellationToken),
                cancellationToken
            );
            await agentEventPublisher.PublishAsync(new ChatRequestCompleted(), cancellationToken);
            session.RecordThought(chatResponse.Content);

            if (chatResponse.ToolCalls.Length > 0)
            {
                ToolCall[] toolCalls = [.. chatResponse.ToolCalls.Select(toolCall => new ToolCall(toolCall.ToolId, toolCall.Name, toolCall.Arguments))];
                session.RequestAction(toolCalls);

                Task<ToolResult>[] executionTasks = [.. toolCalls.Select(toolCall => ExecuteToolAsync(toolCall, cancellationToken))];
                ToolResult[] toolResults = await Task.WhenAll(executionTasks);
                session.RecordObservation([.. toolResults]);
                
                session.ResumeThinking();
            }
            // Not all models return a tool calls reason. Some will use finish reason stop or length to hand back
            // control to the user. So the true stop condition is when there are no tool calls and the finish
            // reason is stop or length.
            else if (chatResponse.FinishReason == ChatFinishReason.Stop || chatResponse.FinishReason == ChatFinishReason.Length)
            {
                session.Finish();
            }
        }

        session.Reset();
    }

    private async Task<ToolResult> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        bool started = false;
        string toolName = toolCall.Name;
     
        try
        {
            ITool tool = session.GetTool(toolName);
            await agentEventPublisher.PublishAsync(new ToolExecutionStarted(tool.GetInvocationMessage(toolCall.Arguments)), cancellationToken);
            started = true;
            
            ToolExecutionResult result = await tool.ExecuteAsync(toolCall.Arguments, cancellationToken);
            await agentEventPublisher.PublishAsync(new ToolExecutionCompleted(result.Success, toolName, result.DisplayMessage, result.Result, result.Error), cancellationToken);

            if (result.Success)
            {
                return new ToolResult(toolCall.ToolId, toolName, result.Result);
            }
            else
            {
                return new ToolResult(toolCall.ToolId, toolName, $"Failed to execute tool '{toolName}' because {result.Error}");
            }
        }
        catch (Exception ex)
        {
            if (!started)
            {
                await agentEventPublisher.PublishAsync(new ToolExecutionStarted($"[{toolName}] [{toolCall.Arguments}]"), cancellationToken);
            }
          
            await agentEventPublisher.PublishAsync(new ToolExecutionCompleted(false, toolName, $"An error occurred: {ex.Message}", string.Empty, ex.ToString()), cancellationToken);
            return new ToolResult(toolCall.ToolId, toolName, $"Exception occurred while executing tool '{toolName}' because {ex}");
        }
    }
}