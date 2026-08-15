using Wayfare.Core.Abstractions;

namespace Wayfare.Core.Events;

public record ToolCompilationStartedEvent(string ToolName) : IEvent;
