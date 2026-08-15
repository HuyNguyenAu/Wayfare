using Wayfare.Core.Abstractions;

namespace Wayfare.Core.Events;

public record ChatRequestStartedEvent(IReadOnlyList<string> ToolNames) : IEvent;
