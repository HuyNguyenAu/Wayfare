namespace Wayfare.Tests;

using System.Text;
using Wayfare.Infrastructure.AI;
using Wayfare.Session;
using Wayfare.Session.Transformations;
using Wayfare.Tools;
using Xunit;

public sealed class TransformationPipelineTests
{
    [Fact]
    public void WriteShadowingRule_WrapsPriorObservations_WhenWriteOccurs()
    {
        // Arrange
        Session session = new();
        session.StartBranch();

        // 1. User asks to inspect a file
        session.AppendTurn(new UserMessage("Inspect Program.cs"));

        // 2. Read tool call & result
        session.AppendTurn(new ToolCallMessage([new ToolCall("call_1", "read", "{\"path\": \"Program.cs\"}")]));
        session.AppendTurn(new ToolResultMessage([
            new ToolExecutionResult(true, "Read 10 lines from 'Program.cs'.", "<observation tool=\"read_file\" path=\"Program.cs\">initial content</observation>", "", "call_1", "read")
        ]));

        // Assert: Read observation is active TurnNode
        Assert.IsType<TurnNode>(session.LinearTrunk[^1]);

        // 3. Write tool call & result for the same file
        session.AppendTurn(new ToolCallMessage([new ToolCall("call_2", "write", "{\"path\": \"Program.cs\", \"content\": \"new content\"}")]));
        session.AppendTurn(new ToolResultMessage([
            new ToolExecutionResult(true, "Successfully wrote content to file 'Program.cs'.", "Successfully wrote all content to file 'Program.cs'.", "", "call_2", "write")
        ]));

        // Assert: The prior read observation (index 2) is now wrapped in a SupersededStateNode
        HistoryNode priorReadNode = session.LinearTrunk[2];
        Assert.IsType<SupersededStateNode>(priorReadNode);

        SupersededStateNode superseded = (SupersededStateNode)priorReadNode;
        Assert.Equal("Program.cs", superseded.ResourceKey);

        ToolResultMessage projectedMsg = Assert.IsType<ToolResultMessage>(superseded.ToProjectedMessage());
        Assert.Contains("superseded", projectedMsg.Results[0].Result, StringComparison.OrdinalIgnoreCase);

        StringBuilder sb = new();
        projectedMsg.AppendTraceLines(sb);
        Assert.Contains("superseded", sb.ToString(), StringComparison.OrdinalIgnoreCase);

        // Verify that root turn is still preserved
        Assert.NotNull(superseded.GetRootTurn());
    }

    [Fact]
    public void DiagnosticCollapseRule_CollapsesContiguousExploratoryTurns_BeforeTerminalAction()
    {
        // Arrange
        Session session = new();
        session.StartBranch();

        session.AppendTurn(new UserMessage("Find and list files"));

        // Exploratory turn 1: list
        session.AppendTurn(new ToolCallMessage([new ToolCall("c1", "list", "{\"path\": \".\"}")]));
        session.AppendTurn(new ToolResultMessage([
            new ToolExecutionResult(true, "Listed 5 entries in '.'.", "<observation tool=\"list\">file1.cs\nfile2.cs</observation>", "", "c1", "list")
        ]));

        // Exploratory turn 2: find
        session.AppendTurn(new ToolCallMessage([new ToolCall("c2", "find", "{\"pattern\": \"Program\"}")]));
        session.AppendTurn(new ToolResultMessage([
            new ToolExecutionResult(true, "Found 1 match for 'Program'.", "<observation tool=\"find\" pattern=\"Program\">Program.cs</observation>", "", "c2", "find")
        ]));

        // Terminal mutation: write
        session.AppendTurn(new ToolCallMessage([new ToolCall("c3", "write", "{\"path\": \"output.txt\", \"content\": \"done\"}")]));
        session.AppendTurn(new ToolResultMessage([
            new ToolExecutionResult(true, "Successfully wrote content to file 'output.txt'.", "Successfully wrote all content to file 'output.txt'.", "", "c3", "write")
        ]));

        // Assert: The trunk should contain CollapsedExplorationNode
        bool hasCollapsedNode = session.LinearTrunk.Any(node => node is CollapsedExplorationNode);
        Assert.True(hasCollapsedNode, "Expected LinearTrunk to contain a CollapsedExplorationNode.");
    }
}
