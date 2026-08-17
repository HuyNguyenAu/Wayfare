namespace Wayfare;

using System.ClientModel;
using Microsoft.Extensions.AI;
using Wayfare.Agent;
using Wayfare.Infrastructure.Clients;
using Wayfare.Infrastructure.Configuration;
using Wayfare.Infrastructure.Events;
using Wayfare.Session;
using Wayfare.Tools;
using Wayfare.Tools.Implementations;
using Wayfare.UI;


public class Program
{
    public static async Task Main()
    {
        Settings settings = Settings.FromEnvironment();

        OpenAI.Chat.ChatClient openAIChatClient = new(
            settings.ModelName,
            new ApiKeyCredential(settings.ApiKey),
            new OpenAI.OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) }
        );
        IChatClient chatClient = openAIChatClient.AsIChatClient()
            .AsBuilder()
            .UseReasoningExtraction()
            .Build();

        CancellationTokenSource cancellationTokenSource = new();
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        EventBroker eventBroker = new();
        await using TerminalUI terminalUI = new(eventBroker, cancellationTokenSource.Token);

        await using SessionStore sessionStore = new(settings.SessionsDirectory);
        InspectMilestoneTool inspectMilestoneTool = new(sessionStore.Session);
        ToolManager toolManager = new(eventBroker, [inspectMilestoneTool]);

        eventBroker.Publish(new StartupStartedEvent());
        await toolManager.LoadToolsAsync(settings.ToolsPath, "*.cs", settings.CompiledDirectory, cancellationTokenSource.Token);

        if (toolManager.Errors.Count > 0)
        {
            foreach (Exception error in toolManager.Errors)
            {
                Console.Error.WriteLine($"Tool initialisation failed: {error.Message}");
            }

            Environment.ExitCode = 1;
            return;
        }

        eventBroker.Publish(new StartupCompletedEvent());
        eventBroker.Publish(new AgentStartedEvent());

        BranchSquasher branchSquasher = new(chatClient);
        MessagePromptBuilder messagePromptBuilder = new();
        IntentResolver intentResolver = new(chatClient);
        PivotDetector pivotDetector = new();
        CircuitBreaker circuitBreaker = new(settings.MaxTurns);
        Orchestrator orchestrator = new(chatClient, toolManager, sessionStore, eventBroker, branchSquasher, messagePromptBuilder, intentResolver, pivotDetector, circuitBreaker);

        try
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                string userInput = await terminalUI.GetUserInputAsync(cancellationTokenSource.Token);
                await orchestrator.RunCycleAsync(userInput, cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"An error occurred: {exception.Message}");
        }
        finally
        {
            eventBroker.Complete();
            await terminalUI.WaitForCompletionAsync();
            await cancellationTokenSource.CancelAsync();
        }
    }
}