namespace Wayfare.Tests;

using Wayfare.Agent;
using Xunit;

public sealed class PivotDetectorTests
{
    [Theory]
    [InlineData("actually, let's work on the tests instead")]
    [InlineData("nevermind, switch to refactoring")]
    [InlineData("forget that, let's create a new file")]
    [InlineData("scratch that, let's do something else")]
    [InlineData("wait, let's change direction")]
    [InlineData("no, switch to the other project")]
    [InlineData("stop, let's focus on Program.cs")]
    public void IsPivot_ReturnsTrue_ForPivotInputs(string input)
    {
        PivotDetector detector = new();
        Assert.True(detector.IsPivot(input));
    }

    [Theory]
    [InlineData("Please read Program.cs")]
    [InlineData("Can you fix the build errors?")]
    [InlineData("Add a new unit test for WriteShadowingRule")]
    [InlineData("Continue with the implementation")]
    public void IsPivot_ReturnsFalse_ForNonPivotInputs(string input)
    {
        PivotDetector detector = new();
        Assert.False(detector.IsPivot(input));
    }
}
