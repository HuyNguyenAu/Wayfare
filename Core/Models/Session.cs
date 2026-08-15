using Wayfare.Core.Abstractions;
using Wayfare.Core.Models.Messages;

namespace Wayfare.Core.Models;

public class Session : ISession
{
    private readonly List<SessionMessage> _messages;

    public SessionState State { get; private set; } = SessionState.Idle;
    public IReadOnlyList<SessionMessage> Messages => _messages.AsReadOnly();

    public Session(string systemPrompt)
    {
        _messages = [new SystemMessage(systemPrompt)];
    }

    public Session(IEnumerable<SessionMessage> initialMessages)
    {
        _messages = [.. initialMessages];
    }

    public void BeginThinking(string userInput)
    {
        GuardState(SessionState.Idle, SessionState.Observing);
        State = SessionState.Thinking;
        _messages.Add(new UserMessage(userInput));
    }

    public void RecordThought(string content)
    {
        GuardState(SessionState.Thinking);
        State = SessionState.Thinking;
        _messages.Add(new AssistantMessage(content));
    }

    public void RequestAction(IReadOnlyList<ToolCall> toolCalls)
    {
        GuardState(SessionState.Thinking);
        State = SessionState.Acting;
        _messages.Add(new ToolCallMessage(toolCalls));
    }

    public void RecordObservation(IReadOnlyList<ToolExecutionResult> toolResults)
    {
        GuardState(SessionState.Acting);
        State = SessionState.Observing;
        _messages.Add(new ToolResultMessage(toolResults));
    }

    public void ResumeThinking()
    {
        GuardState(SessionState.Observing);
        State = SessionState.Thinking;
    }

    public void Finish()
    {
        GuardState(SessionState.Thinking, SessionState.Observing);
        State = SessionState.Done;
    }

    public void Idle()
    {
        GuardState(SessionState.Done, SessionState.Thinking, SessionState.Observing);
        State = SessionState.Idle;
    }

    private void GuardState(params IReadOnlyList<SessionState> expected)
    {
        if (!expected.Contains(State))
        {
            // Transition recovery for resilience across model execution cycles
            State = expected[0];
        }
    }
}
