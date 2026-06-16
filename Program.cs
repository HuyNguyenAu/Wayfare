
using System.ClientModel;
using OpenAI;
using OpenAI.Chat;

namespace WayFare;

public class Program
{
    private static readonly string _modelNameKey = "WAYFARE_MODEL_NAME";
    private static readonly string _apiKeyKey = "WAYFARE_API_KEY";
    private static readonly string _endpointKey = "WAYFARE_ENDPOINT";
    private static readonly string _toolsPathKey = "WAYFARE_TOOLS_PATH";
    private static readonly string _compiledDirectoryKey = "WAYFARE_COMPILED_DIRECTORY";
    private static readonly string _sessionsDirectoryKey = "WAYFARE_SESSIONS_DIRECTORY";

    public static async Task Main()
    {
        DotNetEnv.Env.Load();

        string modelName = Environment.GetEnvironmentVariable(_modelNameKey)
            ?? throw new InvalidOperationException($"Model name must be specified in {_modelNameKey} environment variable.");
        string apiKey = Environment.GetEnvironmentVariable(_apiKeyKey)
            ?? throw new InvalidOperationException($"API key must be specified in {_apiKeyKey} environment variable.");
        string endpoint = Environment.GetEnvironmentVariable(_endpointKey)
            ?? throw new InvalidOperationException($"Endpoint must be specified in {_endpointKey} environment variable.");
        string toolsPath = Environment.GetEnvironmentVariable(_toolsPathKey)
            ?? throw new InvalidOperationException($"Tools path must be specified in {_toolsPathKey} environment variable.");
        string compiledDirectory = Environment.GetEnvironmentVariable(_compiledDirectoryKey)
            ?? throw new InvalidOperationException($"Compiled directory must be specified in {_compiledDirectoryKey} environment variable.");
        string sessionsDirectory = Environment.GetEnvironmentVariable(_sessionsDirectoryKey)
            ?? throw new InvalidOperationException($"Sessions directory must be specified in {_sessionsDirectoryKey} environment variable.");

        if (string.IsNullOrEmpty(toolsPath))
        {
            throw new InvalidOperationException("Tools path must be specified.");
        }

        if (string.IsNullOrEmpty(compiledDirectory))
        {
            throw new InvalidOperationException("Compiled directory must be specified.");
        }

        if (string.IsNullOrEmpty(sessionsDirectory))
        {
            throw new InvalidOperationException("Sessions directory must be specified.");
        }

        ChatClient chatClient = new(modelName, new ApiKeyCredential(apiKey), new OpenAIClientOptions()
        {
            Endpoint = new Uri(endpoint)
        });
        OpenAIClient openAIClient = new(chatClient);

        CancellationTokenSource cancellationTokenSource = new();
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        await using TerminalUI terminalUI = new(cancellationTokenSource.Token);
        AgentEventPublisher agentEventPublisher = new([terminalUI]);
        ToolManager toolManager = new(agentEventPublisher);
        Session session = new(toolManager, sessionsDirectory);
        Engine engine = new(session, openAIClient, agentEventPublisher);

        await agentEventPublisher.PublishAsync(new StartupStarted(), cancellationTokenSource.Token);
        await toolManager.LoadToolsAsync(toolsPath, "*.cs", compiledDirectory, cancellationTokenSource.Token);
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
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            await cancellationTokenSource.CancelAsync();
        }
    }
}