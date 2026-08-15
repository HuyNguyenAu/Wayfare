using System.ClientModel;
using Wayfare.Core;
using Wayfare.Core.Events;
using Wayfare.Core.Models;
using Wayfare.Core.Prompts;
using Wayfare.Infrastructure.Clients;
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
        ToolManager toolManager = new(eventBroker);
        SessionStore sessionStore = new(settings.SessionsDirectory);

        eventBroker.Publish(new StartupStartedEvent());
        await toolManager.LoadToolsAsync(settings.ToolsPath, "*.cs", settings.CompiledDirectory, cancellationTokenSource.Token);
        eventBroker.Publish(new StartupCompletedEvent());
        eventBroker.Publish(new AgentStartedEvent());

        string systemPrompt = SystemPromptBuilder.Build(toolManager.Tools);
        Session session = new(systemPrompt);
        await sessionStore.AppendMessageAsync(session.Messages[0], cancellationTokenSource.Token);

        Orchestrator orchestrator = new(session, openAIClient, toolManager, sessionStore, eventBroker);

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