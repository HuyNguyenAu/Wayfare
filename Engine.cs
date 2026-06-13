using OpenAI.Chat;

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

            if (chatResponse.ToolCalls.Count > 0)
            {
                foreach (ToolCall toolCall in chatResponse.ToolCalls)
                {
                    session.RequestAction(toolCall.ToolId, toolCall.Name, toolCall.Args);

                    string toolResult = $"Executed {toolCall.Name} with args {toolCall.Args}";
                    session.RecordObservation(toolCall.ToolId, toolCall.Name, toolResult);
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