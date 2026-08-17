namespace Wayfare.Core;

#region Startup & Agent Lifecycle Events

public record StartupStartedEvent : IEvent;

public record StartupCompletedEvent : IEvent;

public record AgentStartedEvent : IEvent;

#endregion

#region Dynamic Tool Pipeline Events

public record ToolCompilationStartedEvent(string ToolName) : IEvent;

public record ToolCompilationCompletedEvent : IEvent;

public record ToolCompilationFailedEvent(string ToolName, string Error) : IEvent;

public record ToolLoadingStartedEvent(string ToolName) : IEvent;

public record ToolLoadingCompletedEvent : IEvent;

public record ToolLoadingFailedEvent(string ToolName, string Error) : IEvent;

public record ToolExecutionStartedEvent(string InvocationMessage) : IEvent;

public record ToolExecutionCompletedEvent(
    bool Success,
    string ToolName,
    string DisplayMessage,
    string Result,
    string Error) : IEvent;

#endregion

#region Chat & LLM Streaming Events

public record ChatRequestStartedEvent(IReadOnlyList<string> ToolNames) : IEvent;

public record ChatRequestCompletedEvent : IEvent;

public record TokenChunkReceivedEvent(string Content) : IEvent;

#endregion

#region Session & Cycle Events

public record SquashingBranchEvent : IEvent;

public record CycleCompletedEvent(string Objective, IReadOnlyList<string> Milestones) : IEvent;

#endregion
