using System.Text;
using WayFare.Tools;

namespace WayFare;

internal interface ISessionMessage;
internal record SystemMessage(string Content) : ISessionMessage;
internal record UserMessage(string Content) : ISessionMessage;
internal record AssistantMessage(string Content) : ISessionMessage;
internal record ToolCallMessage(string ToolId, string ToolName, string Arguments) : ISessionMessage;
internal record ToolResultMessage(string ToolId, string ToolName, string Result) : ISessionMessage;

internal interface ISession
{
    State State { get; }
    List<ISessionMessage> Messages { get; }

    void BeginThinking(string userInput);
    void RecordThought(string content);
    void RequestAction(string ToolId, string Name, string Args);
    void RecordObservation(string ToolId, string Name, string Result);
    void Finish();

    ITool GetTool(string name);
}

internal class Session : ISession
{
    public State State { get; private set; } = State.Idle;
    public List<ISessionMessage> Messages { get; private set; }

    private readonly IToolManager _toolManager;

    public Session(IToolManager toolManager)
    {
        _toolManager = toolManager;
        Messages = [new SystemMessage(SystemPrompt())];
    }

    public void BeginThinking(string userInput)
    {
        EnsureState(State.Idle, State.Observing);
        State = State.Thinking;

        Messages.Add(new UserMessage(userInput));
    }

    public void RecordThought(string content)
    {
        EnsureState(State.Thinking);

        Messages.Add(new AssistantMessage(content));
    }

    public void RequestAction(string toolId, string name, string args)
    {
        EnsureState(State.Thinking);
        State = State.Acting;

        Messages.Add(new ToolCallMessage(toolId, name, args));
    }

    public void RecordObservation(string toolId, string name, string result)
    {
        EnsureState(State.Acting);
        State = State.Observing;

        Messages.Add(new ToolResultMessage(toolId, name, result));
    }

    public void Finish()
    {
        EnsureState(State.Thinking, State.Observing);
        State = State.Done;
    }

    public ITool GetTool(string name)
    {
        ITool? tool = _toolManager.Tools.FirstOrDefault(tool => tool.Name == name)
            ?? throw new KeyNotFoundException($"No tool found with name '{name}'");

        return tool;
    }

    private void EnsureState(params State[] expected)
    {
        if (expected.Contains(State))
        {
            return;
        }

        throw new InvalidOperationException($"Expected states {string.Join(", ", expected)}, but was {State}");
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
        promptBuilder.AppendLine("- To read a file, use read_file.");
        promptBuilder.AppendLine("- To edit a file, use replace. The oldText must match exactly what is in the file, including whitespace.");
        promptBuilder.AppendLine("- The oldText in replace must appear exactly once in the file. If it appears more than once, add more surrounding lines to make it unique.");
        promptBuilder.AppendLine("- To create a new file or completely overwrite one, use write_file.");
        promptBuilder.AppendLine("- Always read a file before editing it.");
        promptBuilder.AppendLine("- Keep responses short. Show file paths when working with files.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"Current date: {DateTime.UtcNow:yyyy-MM-dd HH:mm UTC}");
        promptBuilder.AppendLine($"Current operating system: {Environment.OSVersion}");
        promptBuilder.AppendLine($"Current working directory: {Directory.GetCurrentDirectory()}");

        return promptBuilder.ToString();
    }
}