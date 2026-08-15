using Wayfare.Core.Abstractions;

namespace Wayfare.Core.Events;

public record ToolLoadingFailedEvent(string ToolName, string Error) : IEvent;
