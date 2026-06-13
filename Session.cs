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
}

internal class Session : ISession
{
    public State State { get; private set; } = State.Idle;
    public List<ISessionMessage> Messages { get; } = [];

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

    private void EnsureState(params State[] expected)
    {
        if (expected.Contains(State))
        {
            return;
        }

        throw new InvalidOperationException($"Expected states {string.Join(", ", expected)}, but was {State}");
    }
}