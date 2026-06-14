using WayFare.Tools;

namespace WayFare;

internal interface IEngine
{
    Task RunAsync(CancellationToken cancellationToken);
}

internal class Engine(ISession session, IChatClient chatClient) : IEngine
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        string userInput = await Console.In.ReadLineAsync(cancellationToken) ?? string.Empty;
        session.BeginThinking(userInput);

        while (session.State != State.Done && !cancellationToken.IsCancellationRequested)
        {
            ChatResponse chatResponse = await chatClient.ChatAsync([.. session.Messages], cancellationToken);
            session.RecordThought(chatResponse.Content);

            if (chatResponse.ToolCalls.Length > 0)
            {
                ToolCall[] toolCalls = [.. chatResponse.ToolCalls.Select(toolCall => new ToolCall(toolCall.ToolId, toolCall.Name, toolCall.Arguments))];
                session.RequestAction(toolCalls);

                List<ToolResult> toolResults = [];
    
                foreach (ToolCall toolCall in toolCalls)
                {
                    try
                    {
                        ITool tool = session.GetTool(toolCall.ToolId);
                        ToolExecutionResult result = await tool.ExecuteAsync(toolCall.Arguments, cancellationToken);

                        if (result.Success)
                        {
                            toolResults.Add(new ToolResult(toolCall.ToolId, toolCall.Name, result.Result));
                        }
                        else
                        {
                            toolResults.Add(new ToolResult(toolCall.ToolId, toolCall.Name, $"Failed to execute tool '{toolCall.Name}' because {result.Error}"));
                        }
                    }
                    catch (Exception ex)
                    {
                        toolResults.Add(new ToolResult(toolCall.ToolId, toolCall.Name, $"Exception occurred while executing tool '{toolCall.Name}' because {ex}"));
                    }
                }

                session.RecordObservation([.. toolResults]);
            }
            // Not all models return a tool calls reason. Some will use finish reason stop or length to hand back
            // control to the user. So the true stop condition is when there are no tool calls and the finish
            // reason is stop or length.
            else if (chatResponse.FinishReason == ChatFinishReason.Stop || chatResponse.FinishReason == ChatFinishReason.Length)
            {
                session.Finish();
            }
        }
    }
}