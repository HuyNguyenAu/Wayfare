namespace Wayfare.Tests;

using Wayfare.Infrastructure.Events;
using Xunit;

public sealed class EventBrokerTests
{
    [Fact]
    public async Task EventBroker_PublishesAndReadsEvents()
    {
        // Arrange
        EventBroker broker = new();
        List<AgentEvent> receivedEvents = [];

        // Act
        broker.Publish(new StartupStartedEvent());
        broker.Publish(new ToolExecutionStartedEvent("read Program.cs"));
        broker.Complete();

        await foreach (AgentEvent @event in broker.ReadAllAsync(CancellationToken.None))
        {
            receivedEvents.Add(@event);
        }

        // Assert
        Assert.Equal(2, receivedEvents.Count);
        Assert.IsType<StartupStartedEvent>(receivedEvents[0]);
        Assert.IsType<ToolExecutionStartedEvent>(receivedEvents[1]);
    }
}
