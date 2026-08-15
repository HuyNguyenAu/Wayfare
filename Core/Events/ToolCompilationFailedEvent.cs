using Wayfare.Core.Abstractions;

namespace Wayfare.Core.Events;

public record ToolCompilationFailedEvent(string ToolName, string Error) : IEvent;
