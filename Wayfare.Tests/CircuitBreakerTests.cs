namespace Wayfare.Tests;

using Wayfare.Agent;
using Wayfare.Infrastructure.AI;
using Wayfare.Tools;
using Xunit;

public sealed class CircuitBreakerTests
{
    [Fact]
    public void TryAdvanceTurn_RespectsMaxTurnsLimit()
    {
        // Arrange
        CircuitBreaker circuitBreaker = new(maxTurns: 3);

        // Act & Assert
        Assert.True(circuitBreaker.TryAdvanceTurn(out _)); // Turn 1
        Assert.True(circuitBreaker.TryAdvanceTurn(out _)); // Turn 2
        Assert.True(circuitBreaker.TryAdvanceTurn(out _)); // Turn 3
        Assert.False(circuitBreaker.TryAdvanceTurn(out string? reason)); // Turn 4 exceeds limit
        Assert.NotNull(reason);
        Assert.Contains("limit of 3 turns exceeded", reason);
    }

    [Fact]
    public void CircuitBreaker_InterceptsRepeatedFailingToolCall()
    {
        // Arrange
        CircuitBreaker circuitBreaker = new(maxTurns: 10);
        ToolCall failingCall = new("call_1", "read", "{\"path\": \"non_existent.txt\"}");
        ToolExecutionResult failureResult = new(false, "File not found", "", "Error: File not found");

        // Record the failure
        circuitBreaker.RecordResults([failingCall], [failureResult]);

        // Attempt same call again
        bool intercepted = circuitBreaker.TryIntercept(failingCall, out ToolExecutionResult? interceptedResult);

        // Assert
        Assert.True(intercepted);
        Assert.NotNull(interceptedResult);
        Assert.False(interceptedResult.Success);
        Assert.Contains("LOOP DETECTED", interceptedResult.Error);
    }

    [Fact]
    public void CircuitBreaker_AllowsCallWithDifferentArguments()
    {
        // Arrange
        CircuitBreaker circuitBreaker = new(maxTurns: 10);
        ToolCall failingCall = new("call_1", "read", "{\"path\": \"missing.txt\"}");
        ToolExecutionResult failureResult = new(false, "File not found", "", "Error: File not found");

        circuitBreaker.RecordResults([failingCall], [failureResult]);

        // Different arguments
        ToolCall newCall = new("call_2", "read", "{\"path\": \"existing.txt\"}");
        bool intercepted = circuitBreaker.TryIntercept(newCall, out ToolExecutionResult? interceptedResult);

        // Assert
        Assert.False(intercepted);
        Assert.Null(interceptedResult);
    }
}
