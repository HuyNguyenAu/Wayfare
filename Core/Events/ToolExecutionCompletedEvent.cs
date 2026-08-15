using Wayfare.Core.Abstractions;

namespace Wayfare.Core.Events;

public record ToolExecutionCompletedEvent(bool Success, string ToolName, string DisplayMessage, string Result, string Error) : IEvent;
