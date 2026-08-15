using System.ClientModel;
using OpenAI;
using OpenAI.Chat;
using Wayfare.Core;
using Wayfare.Core.Abstractions;
using Wayfare.Core.Events;
using Wayfare.Core.Models;
using Wayfare.Core.Prompts;
using Wayfare.Infrastructure.Configuration;
using Wayfare.Infrastructure.Events;
using Wayfare.Persistence;
using Wayfare.Tools;
using Wayfare.UI;

namespace Wayfare;

public class Program
{
    public static async Task Main()
    {
        Settings settings = Settings.FromEnvironment();

        ChatClient innerChatClient = new(
            settings.ModelName,
            new ApiKeyCredential(settings.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) }
        );
        IChatClient chatClient = new Wayfare.Infrastructure.Clients.OpenAIClient(innerChatClient);

        CancellationTokenSource cancellationTokenSource = new();
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        IEventBroker eventBroker = new EventBroker();
        await using TerminalUI terminalUI = new(eventBroker, cancellationTokenSource.Token);
        IToolManager toolManager = new ToolManager(eventBroker);
        ISessionStore sessionStore = new SessionStore(settings.SessionsDirectory);

        eventBroker.Publish(new StartupStartedEvent());
        await toolManager.LoadToolsAsync(settings.ToolsPath, "*.cs", settings.CompiledDirectory, cancellationTokenSource.Token);
        eventBroker.Publish(new StartupCompletedEvent());
        eventBroker.Publish(new AgentStartedEvent());

        string systemPrompt = SystemPromptBuilder.Build(toolManager.Tools);
        ISession session = new Session(systemPrompt);
        await sessionStore.AppendMessageAsync(session.Messages[0], cancellationTokenSource.Token);

        IOrchestrator orchestrator = new Orchestrator(session, chatClient, toolManager, sessionStore, eventBroker);

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