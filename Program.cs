using System.ClientModel;
using Wayfare.Core;
using Wayfare.Core.Events;
using Wayfare.Infrastructure.Clients;
using Wayfare.Infrastructure.Configuration;
using Wayfare.Infrastructure.Events;
using Wayfare.Persistence;
using Wayfare.Tools;
using Wayfare.UI;

using Wayfare.Core.Prompts;

namespace Wayfare;

public class Program
{
    public static async Task Main()
    {
        Settings settings = Settings.FromEnvironment();

        OpenAI.Chat.ChatClient chatClient = new(
            settings.ModelName,
            new ApiKeyCredential(settings.ApiKey),
            new OpenAI.OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) }
        );
        OpenAIClient openAIClient = new(chatClient);

        CancellationTokenSource cancellationTokenSource = new();
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        EventBroker eventBroker = new();
        await using TerminalUI terminalUI = new(eventBroker, cancellationTokenSource.Token);

        await using SessionStore sessionStore = new(settings.SessionsDirectory);
        ToolManager toolManager = new(eventBroker);

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

        BranchSquasher branchSquasher = new(openAIClient);
        MessagePromptBuilder messagePromptBuilder = new();
        Orchestrator orchestrator = new(openAIClient, toolManager, sessionStore, eventBroker, branchSquasher, messagePromptBuilder);

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
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            eventBroker.Complete();
            await terminalUI.WaitForCompletionAsync();
            await cancellationTokenSource.CancelAsync();
        }
    }
}