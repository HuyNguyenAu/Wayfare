using Wayfare.Core.Models;
using Wayfare.Core.Models.Messages;

namespace Wayfare.Core.Abstractions;

public interface ISession
{
    SessionState State { get; }
    IReadOnlyList<SessionMessage> Messages { get; }

    void BeginThinking(string userInput);
    void RecordThought(string content);
    void RequestAction(IReadOnlyList<ToolCall> toolCalls);
    void RecordObservation(IReadOnlyList<ToolExecutionResult> toolResults);
    void ResumeThinking();
    void Finish();
    void Idle();
}
