using Wayfare.Core.Abstractions;

namespace Wayfare.Core.Events;

public record ToolExecutionStartedEvent(string InvocationMessage) : IEvent;
