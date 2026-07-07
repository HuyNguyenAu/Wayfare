using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WayFare.Tools;

namespace WayFare;

internal enum State
{
    Idle,
    Thinking,
    Acting,
    Observing,
    Done,
}

internal record ToolCall(string ToolId, string Name, string Arguments);
internal record ToolResult(string ToolId, string ToolName, string Result);

[JsonDerivedType(typeof(SystemMessage), typeDiscriminator: "system")]
[JsonDerivedType(typeof(UserMessage), typeDiscriminator: "user")]
[JsonDerivedType(typeof(AssistantMessage), typeDiscriminator: "assistant")]
[JsonDerivedType(typeof(ToolCallMessage), typeDiscriminator: "tool_call")]
[JsonDerivedType(typeof(ToolResultMessage), typeDiscriminator: "tool_result")]
internal interface ISessionMessage;
internal record SystemMessage(string Content) : ISessionMessage;
internal record UserMessage(string Content) : ISessionMessage;
internal record AssistantMessage(string Content) : ISessionMessage;
internal record ToolCallMessage(ToolCall[] ToolCalls) : ISessionMessage;
internal record ToolResultMessage(ToolResult[] ToolResults) : ISessionMessage;


internal interface ISession
{
    State State { get; }
    List<ISessionMessage> Messages { get; }

    void BeginThinking(string userInput);
    void RecordThought(string content);
    void RequestAction(ToolCall[] toolCalls);
    void RecordObservation(ToolResult[] toolResults);
    void ResumeThinking();
    void Finish();
    void Idle();

    ITool GetTool(string name);
    ITool[] GetTools();
}

internal class Session : ISession
{
    public State State { get; private set; } = State.Idle;
    public List<ISessionMessage> Messages { get; private set; }

    private readonly IToolManager _toolManager;
    private readonly string _sessionsDirectory;
    private readonly string _filePath;

    public Session(IToolManager toolManager, string sessionsDirectory)
    {
        _toolManager = toolManager;
        _sessionsDirectory = sessionsDirectory;
        _filePath = Path.Combine(_sessionsDirectory, $"session_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jsonl");

        if (string.IsNullOrEmpty(sessionsDirectory))
        {
            throw new InvalidOperationException("Sessions directory must be specified.");
        }

        Directory.CreateDirectory(_sessionsDirectory);

        Messages = [new SystemMessage(SystemPrompt())];
    }

    private void UpdateSessionFile()
    {
        try
        {
            StringBuilder stringBuilder = new();

            foreach (ISessionMessage message in Messages)
            {
                string json = JsonSerializer.Serialize(message);
                stringBuilder.AppendLine(json);
            }
            
            File.WriteAllText(_filePath, stringBuilder.ToString());
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to rewrite session messages: {ex.Message}");
        }
    }

    public void BeginThinking(string userInput)
    {
        EnsureState(State.Idle, State.Observing);
        State = State.Thinking;

        UserMessage userMessage = new(userInput);
        Messages.Add(userMessage);
        UpdateSessionFile();
    }

    public void RecordThought(string content)
    {
        EnsureState(State.Thinking);

        AssistantMessage assistantMessage = new(content);
        Messages.Add(assistantMessage);
        UpdateSessionFile();
    }

    public void RequestAction(ToolCall[] toolCalls)
    {
        EnsureState(State.Thinking);
        State = State.Acting;

        ToolCallMessage toolCallMessage = new(toolCalls);
        Messages.Add(toolCallMessage);
        UpdateSessionFile();
    }

    public void RecordObservation(ToolResult[] toolResults)
    {
        EnsureState(State.Acting);
        State = State.Observing;

        ToolResultMessage toolResultMessage = new(toolResults);
        Messages.Add(toolResultMessage);
        UpdateSessionFile();
    }

    public void ResumeThinking()
    {
        EnsureState(State.Observing);
        State = State.Thinking;
    }

    public void Finish()
    {
        EnsureState(State.Thinking, State.Observing);
        State = State.Done;
    }

    public void Idle()
    {
        EnsureState(State.Done);
        State = State.Idle;

        SystemMessage newSystemMessage = new(SystemPrompt());
        Messages[0] = newSystemMessage;
        UpdateSessionFile();
    }

    public ITool GetTool(string name)
    {
        ITool? tool = _toolManager.Tools.FirstOrDefault(tool => tool.Name == name)
            ?? throw new KeyNotFoundException($"No tool found with name '{name}'");

        return tool;
    }

    public ITool[] GetTools()
    {
        return _toolManager.Tools;
    }

    private void EnsureState(params State[] expected)
    {
        if (expected.Contains(State))
        {
            return;
        }

        throw new InvalidOperationException($"Expected one of the states {string.Join(" or ", expected)}, but was {State}");
    }

    private string SystemPrompt()
    {
        StringBuilder promptBuilder = new();

        promptBuilder.AppendLine("You are a coding agent harness. You help users by reading and editing files.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Available tools:");

        foreach (ITool tool in _toolManager.Tools)
        {
            promptBuilder.AppendLine($"- {tool.Name}: {tool.Description}");
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Rules:");
        promptBuilder.AppendLine("- To see what files exist, use list or find.");
        promptBuilder.AppendLine("- To read a file, use read.");
        promptBuilder.AppendLine("- To edit a file, use replace. The oldText must match exactly what is in the file, including whitespace.");
        promptBuilder.AppendLine("- The oldText in replace must appear exactly once in the file. If it appears more than once, add more surrounding lines to make it unique.");
        promptBuilder.AppendLine("- To create a new file or completely overwrite one, use write.");
        promptBuilder.AppendLine("- Always read a file before editing it.");
        promptBuilder.AppendLine("- Keep responses short. Show file paths when working with files.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"Current date: {DateTime.UtcNow:yyyy-MM-dd HH:mm UTC}");
        promptBuilder.AppendLine($"Current operating system: {Environment.OSVersion}");
        promptBuilder.AppendLine($"Current working directory: {Directory.GetCurrentDirectory()}");

        return promptBuilder.ToString();
    }
}