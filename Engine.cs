using OpenAI.Chat;
using WayFare.Tools;

namespace WayFare;

internal interface IEngine
{
    Task RunAsync(CancellationToken cancellationToken);
}

internal class Engine(ISession session, IToolManager toolManager, IChatClient chatClient) : IEngine
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        string userInput = await Console.In.ReadLineAsync(cancellationToken) ?? string.Empty;
        session.BeginThinking(userInput);

        while (session.State != State.Done && !cancellationToken.IsCancellationRequested)
        {
            ChatResponse chatResponse = await chatClient.ChatAsync([.. session.Messages], cancellationToken);
            session.RecordThought(chatResponse.Content);

            if (chatResponse.ToolCalls.Count > 0)
            {
                foreach (ToolCall toolCall in chatResponse.ToolCalls)
                {
                    session.RequestAction(toolCall.ToolId, toolCall.Name, toolCall.Arguments);

                    try
                    {
                        ITool tool = toolManager.GetTool(toolCall.ToolId);
                        ToolExecutionResult result = await tool.ExecuteAsync(toolCall.Arguments, cancellationToken);

                        if (result.Success)
                        {
                            session.RecordObservation(toolCall.ToolId, toolCall.Name, result.Result);
                        }
                        else
                        {
                            session.RecordObservation(toolCall.ToolId, toolCall.Name, $"Failed to execute tool '{toolCall.Name}' because {result.Error}");
                        }
                    }
                    catch (Exception ex)
                    {
                        session.RecordObservation(toolCall.ToolId, toolCall.Name, $"Exception occurred while executing tool '{toolCall.Name}' because {ex.Message}");
                    }
                }
            }
            // Not all models return a tool calls reason. Some will use finish reason stop or length to hand back
            // control to the user. So the true stop condition is when there are no tool calls and the finish
            // reason is stop or length.
            else if (chatResponse.FinishReason == FinishReason.Stop || chatResponse.FinishReason == FinishReason.Length)
            {
                session.Finish();
            }
        }
    }
}