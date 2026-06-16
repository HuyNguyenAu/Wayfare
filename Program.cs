
using System.ClientModel;
using OpenAI;
using OpenAI.Chat;

namespace WayFare;

public class Program
{
    public static async Task Main(string[] args)
    {
        ChatClient chatClient = new("MODEL_NAME", new ApiKeyCredential("local-no-key-needed"), new OpenAIClientOptions()
        {
            Endpoint = new Uri("http://127.0.0.1:8080/")
        });
        OpenAIClient openAIClient = new(chatClient);

        TerminalUI terminalUI = new();
        AgentEventPublisher agentEventPublisher = new([terminalUI]);
        ToolManager toolManager = new(agentEventPublisher);
        Session session = new(toolManager);
        Engine engine = new(session, openAIClient, agentEventPublisher);

        CancellationTokenSource cancellationTokenSource = new();
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        await agentEventPublisher.PublishAsync(new StartupStarted(), cancellationTokenSource.Token);
        await toolManager.LoadToolsAsync(@"C:\Users\Kaze\source\repos\Wayfare\Tools", "*.cs", @"C:\Users\Kaze\source\repos\Wayfare\compiled", cancellationTokenSource.Token);
        await agentEventPublisher.PublishAsync(new StartupCompleted(), cancellationTokenSource.Token);
        await agentEventPublisher.PublishAsync(new StartAgent(), cancellationTokenSource.Token);

        try
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                string userInput = await terminalUI.GetUserInputAsync(cancellationTokenSource.Token);
                await engine.RunCycleAsync(userInput, cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Application is shutting down...");
        }
    }
}