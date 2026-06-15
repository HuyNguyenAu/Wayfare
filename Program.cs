
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

        AgentEventHub agentEventHub = new();
        TerminalUI terminalUI = new(agentEventHub);
        ToolManager toolManager = new(agentEventHub);
        Session session = new(toolManager);
        Engine engine = new(session, openAIClient, agentEventHub);

        CancellationTokenSource cancellationTokenSource = new();
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        await terminalUI.Startup(cancellationTokenSource.Token);
        await toolManager.LoadToolsAsync(@"C:\Users\Kaze\source\repos\Wayfare\Tools", "*.cs", @"C:\Users\Kaze\source\repos\Wayfare\compiled", cancellationTokenSource.Token);
        await terminalUI.FinaliseStartup(cancellationTokenSource.Token);

        terminalUI.StartAgent();

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