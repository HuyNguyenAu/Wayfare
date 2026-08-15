using Wayfare.Core.Abstractions;

namespace Wayfare.Core.Events;

public record ToolLoadingStartedEvent(string ToolName) : IEvent;
