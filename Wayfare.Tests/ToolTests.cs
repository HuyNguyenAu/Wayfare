namespace Wayfare.Tests;

using Wayfare.Session;
using Wayfare.Tools;
using Wayfare.Tools.Implementations;
using Xunit;

public sealed class ToolTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly ToolHelpers _toolHelpers;

    public ToolTests()
    {
        _tempDirectory = Path.Combine(Directory.GetCurrentDirectory(), "test_sandbox_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _toolHelpers = new ToolHelpers([".git", "bin", "obj"]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public async Task WriteFileTool_And_ReadFileTool_WorkTogether()
    {
        // Arrange
        string testFile = Path.Combine(_tempDirectory, "test.txt");
        string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), testFile);
        WriteFileTool writeTool = new(_toolHelpers);
        ReadFileTool readTool = new(_toolHelpers);

        // Act - Write
        string writeJson = $"{{\"path\": \"{relativePath}\", \"content\": \"Line 1\\nLine 2\\nLine 3\"}}";
        ToolExecutionResult writeResult = await writeTool.ExecuteAsync(writeJson, CancellationToken.None);

        // Assert - Write
        Assert.True(writeResult.Success);
        Assert.True(File.Exists(testFile));

        // Act - Read
        string readJson = $"{{\"path\": \"{relativePath}\", \"offset\": 0, \"limit\": 10}}";
        ToolExecutionResult readResult = await readTool.ExecuteAsync(readJson, CancellationToken.None);

        // Assert - Read
        Assert.True(readResult.Success);
        Assert.Contains("Line 1", readResult.Result);
        Assert.Contains("Line 2", readResult.Result);
        Assert.Contains("Line 3", readResult.Result);
    }

    [Fact]
    public async Task ReplaceTool_ReplacesUniqueSnippet()
    {
        // Arrange
        string testFile = Path.Combine(_tempDirectory, "replace_test.txt");
        string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), testFile);
        await File.WriteAllTextAsync(testFile, "Hello World!\nThis is a test.\nGoodbye World!");

        ReplaceTool replaceTool = new(_toolHelpers);

        // Act
        string replaceJson = $"{{\"path\": \"{relativePath}\", \"oldText\": \"This is a test.\", \"newText\": \"This is an edited test.\"}}";
        ToolExecutionResult replaceResult = await replaceTool.ExecuteAsync(replaceJson, CancellationToken.None);

        // Assert
        Assert.True(replaceResult.Success);
        string newContent = await File.ReadAllTextAsync(testFile);
        Assert.Contains("This is an edited test.", newContent);
        Assert.DoesNotContain("This is a test.", newContent);
    }

    [Fact]
    public async Task ListTool_And_FindTool_LocateFiles()
    {
        // Arrange
        string testFile1 = Path.Combine(_tempDirectory, "module_alpha.cs");
        string testFile2 = Path.Combine(_tempDirectory, "module_beta.cs");
        await File.WriteAllTextAsync(testFile1, "// alpha");
        await File.WriteAllTextAsync(testFile2, "// beta");

        string relativeDir = Path.GetRelativePath(Directory.GetCurrentDirectory(), _tempDirectory);
        ListTool listTool = new(_toolHelpers);
        FindTool findTool = new(_toolHelpers);

        // Act - List
        ToolExecutionResult listResult = await listTool.ExecuteAsync($"{{\"path\": \"{relativeDir}\"}}", CancellationToken.None);
        Assert.True(listResult.Success);
        Assert.Contains("module_alpha.cs", listResult.Result);
        Assert.Contains("module_beta.cs", listResult.Result);

        // Act - Find
        ToolExecutionResult findResult = await findTool.ExecuteAsync($"{{\"path\": \"{relativeDir}\", \"pattern\": \"alpha\"}}", CancellationToken.None);
        Assert.True(findResult.Success);
        Assert.Contains("module_alpha.cs", findResult.Result);
        Assert.DoesNotContain("module_beta.cs", findResult.Result);
    }

    [Fact]
    public async Task InspectMilestoneTool_ReturnsMilestoneDetails()
    {
        // Arrange
        Session session = new();
        session.StartBranch();
        session.AppendTurn(new UserMessage("Do something"));
        session.SquashBranch("Situation: Needed refactor. Task: Clean code. Action: Refactored. Result: Success. Learnings: None.\nKey Artifacts:\n- Files: [Program.cs]", BranchStatus.Completed);

        string milestoneId = session.History[0].Id;
        InspectMilestoneTool inspectTool = new(session);

        // Act
        ToolExecutionResult result = await inspectTool.ExecuteAsync($"{{\"id\": \"{milestoneId}\"}}", CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Contains(milestoneId, result.Result);
        Assert.Contains("Completed", result.Result);
        Assert.Contains("Needed refactor", result.Result);
    }
}
