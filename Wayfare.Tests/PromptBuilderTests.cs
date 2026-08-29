namespace Wayfare.Tests;

using Wayfare.Agent;
using Wayfare.Session;
using Xunit;

public sealed class PromptBuilderTests
{
    [Fact]
    public void MessagePromptBuilder_BuildsSystemAndBranchMessages()
    {
        // Arrange
        Session session = new();
        session.StartBranch();
        session.AppendTurn(new UserMessage("Hello"));
        session.AppendTurn(new AssistantMessage("Hi there!"));

        MessagePromptBuilder builder = new();

        // Act
        IReadOnlyList<SessionMessage> messages = builder.BuildMessages(session.History);

        // Assert
        Assert.Equal(3, messages.Count);
        Assert.IsType<SystemMessage>(messages[0]);
        Assert.IsType<UserMessage>(messages[1]);
        Assert.IsType<AssistantMessage>(messages[2]);
    }
}
