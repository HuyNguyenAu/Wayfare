using Wayfare.Core.Abstractions;

namespace Wayfare.Core.Events;

public record CycleCompletedEvent(string Objective, IReadOnlyList<string> Milestones) : IEvent;
