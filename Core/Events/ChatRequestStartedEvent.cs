using Wayfare.Core.Abstractions;

namespace Wayfare.Core.Events;

public record ChatRequestStartedEvent(string Description) : IEvent;
