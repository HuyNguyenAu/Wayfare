using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using WayFare.Tools;

namespace WayFare;

internal class LoadToolException(string message, Exception? innerException = null) : Exception(message, innerException);

internal interface IToolManager
{
    public ITool[] Tools { get; }
    public Exception[] Errors { get; }

    Task LoadToolsAsync(string directoryPath, string searchPattern, string compiledDirectoryPath, CancellationToken cancellationToken);
}

internal sealed class ToolManager(IEventPublisher events) : IToolManager
{
    private readonly string[] _ignoreFiles = ["ITool.cs", "ToolHelpers.cs"];

    public ITool[] Tools { get; private set; } = [];
    public Exception[] Errors { get; private set; } = [];

    public async Task LoadToolsAsync(string directoryPath, string searchPattern, string compiledDirectoryPath, CancellationToken cancellationToken)
    {
        LoadToolsResult loadToolsResult = await LoadToolsFromDirectoryAsync(directoryPath, searchPattern, compiledDirectoryPath, cancellationToken);

        Tools = loadToolsResult.Tools;
        Errors = loadToolsResult.Errors;
    }

    private async Task<LoadToolsResult> LoadToolsFromDirectoryAsync(string directoryPath, string searchPattern, string compiledDirectoryPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("A valid tool directory path must be provided", nameof(directoryPath));
        }

        if (!Directory.Exists(directoryPath))
        {
            throw new ArgumentException($"Tool directory does not exist '{directoryPath}'", nameof(directoryPath));
        }

        if (string.IsNullOrWhiteSpace(compiledDirectoryPath))
        {
            throw new ArgumentException("A valid tool compiled directory path must be provided", nameof(compiledDirectoryPath));
        }

        Directory.CreateDirectory(compiledDirectoryPath);

        string[] toolFilePaths;

        try
        {
            toolFilePaths = Directory.GetFiles(directoryPath, searchPattern);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or PathTooLongException or DirectoryNotFoundException)
        {
            throw new LoadToolException($"Failed to access tool directory '{directoryPath}' with search pattern '{searchPattern}'", ex);
        }

        events.Publish(new LoadingToolsStarted());

        HashSet<string?> activeToolNames = toolFilePaths
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && !_ignoreFiles.Contains($"{name}.cs"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Delete stale DLL files for tools that no longer exist.
        if (Directory.Exists(compiledDirectoryPath))
        {
            foreach (string dllFilePath in Directory.GetFiles(compiledDirectoryPath, "*.dll"))
            {
                string toolName = Path.GetFileNameWithoutExtension(dllFilePath);
                if (!activeToolNames.Contains(toolName))
                {
                    try
                    {
                        File.Delete(dllFilePath);
                    }
                    catch
                    {
                        // Ignore deletion errors (e.g. file locked).
                    }
                }
            }
        }

        List<Exception> errors = [];

        foreach (string toolFilePath in toolFilePaths)
        {
            if (_ignoreFiles.Contains(Path.GetFileName(toolFilePath)))
            {
                continue;
            }

            string toolName = Path.GetFileNameWithoutExtension(toolFilePath);
            string dllPath = Path.Combine(compiledDirectoryPath, $"{toolName}.dll");

            if (!File.Exists(dllPath) || File.GetLastWriteTime(dllPath) < File.GetLastWriteTime(toolFilePath))
            {
                try
                {
                    events.Publish(new ToolCompilationStarted(toolName));
                    await CompileToolAsync(toolFilePath, dllPath, cancellationToken);
                    events.Publish(new ToolCompilationCompleted());
                }
                catch (Exception ex)
                {
                    events.Publish(new ToolCompilationFailed(toolName, ex.Message));
                    errors.Add(ex);
                   
                    try
                    {
                        if (File.Exists(dllPath))
                        {
                            File.Delete(dllPath);
                        }
                    }
                    catch
                    {
                        // Ignore.
                    }
                }
            }
        }

        List<ITool> tools = [];

        foreach (string dllFilePath in Directory.GetFiles(compiledDirectoryPath, "*.dll"))
        {
            string toolName = Path.GetFileNameWithoutExtension(dllFilePath);
        
            try
            {
                events.Publish(new ToolLoadingStarted(toolName));

                ITool tool = LoadTool(dllFilePath);
                tools.Add(tool);

                events.Publish(new ToolLoadingCompleted());
            }
            catch (Exception ex)
            {
                events.Publish(new ToolLoadingFailed(toolName, ex.Message));
                errors.Add(ex);
            }
        }

        return new LoadToolsResult([.. tools], [.. errors]);
    }

    private async static Task CompileToolAsync(string toolPath, string dllPath, CancellationToken cancellationToken)
    {
        string sourceCode = await File.ReadAllTextAsync(toolPath, cancellationToken);

        string[] usings = [
            "using System;",
            "using System.IO;",
            "using System.Linq;",
            "using System.Collections.Generic;",
            "using System.Text.Json;",
            "using System.Diagnostics;",
            "using System.Threading;",
            "using System.Threading.Tasks;",
            "using WayFare.Tools;",
        ];
        string sourceWithUsings = $"{string.Join(Environment.NewLine, usings)}{Environment.NewLine}{sourceCode}";
        string assemblyDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)
            ?? throw new LoadToolException("Unable to determine assembly directory for compilation references");

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceWithUsings, cancellationToken: cancellationToken);

        HashSet<string> locations =
        [
            typeof(object).Assembly.Location,
            typeof(Enumerable).Assembly.Location,
            typeof(ITool).Assembly.Location,
            typeof(ToolHelpers).Assembly.Location,
            typeof(System.Text.Json.JsonSerializer).Assembly.Location,
            typeof(Process).Assembly.Location,
            typeof(Console).Assembly.Location,
            typeof(Task).Assembly.Location,
            typeof(CancellationToken).Assembly.Location,
            typeof(File).Assembly.Location,
            Path.Combine(assemblyDirectory, "System.Runtime.dll"),
            Path.Combine(assemblyDirectory, "netstandard.dll")
        ];

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic || string.IsNullOrWhiteSpace(assembly.Location))
            {
                continue;
            }

            locations.Add(assembly.Location);
        }

        List<MetadataReference> references = [];

        foreach (string location in locations)
        {
            try
            {
                references.Add(MetadataReference.CreateFromFile(location));
            }
            catch (Exception ex)
            {
                throw new LoadToolException($"Failed to create metadata reference from '{location}'", ex);
            }
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(toolPath),
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release)
        );

        using MemoryStream compiledToolMemoryStream = new();
        EmitResult result = compilation.Emit(compiledToolMemoryStream, cancellationToken: cancellationToken);

        if (result.Success)
        {
            await File.WriteAllBytesAsync(dllPath, compiledToolMemoryStream.ToArray(), cancellationToken);
            return;
        }

        StringBuilder errors = new();

        foreach (Diagnostic diagnostic in result.Diagnostics)
        {
            if (diagnostic.Severity != DiagnosticSeverity.Error)
            {
                continue;
            }

            errors.AppendLine(diagnostic.ToString());
        }

        throw new LoadToolException($"Failed to compile tool '{toolPath}' because {errors}");
    }

    private static ITool LoadTool(string toolPath)
    {
        IToolHelpers helpers = new ToolHelpers();

        try
        {
            using FileStream toolFileStream = new(toolPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(toolFileStream);

            Type toolType = assembly.GetTypes().FirstOrDefault(t => typeof(ITool).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                ?? throw new LoadToolException($"No valid implementation of ITool found in assembly '{toolPath}'.");

            if (Activator.CreateInstance(toolType, helpers) is not ITool toolInstance)
            {
                throw new LoadToolException($"Failed to load tool from '{toolPath}' because the type '{toolType.FullName}' does not implement ITool or could not be instantiated.");
            }

            return toolInstance;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or BadImageFormatException or ReflectionTypeLoadException)
        {
            throw new LoadToolException($"Failed to load tool from '{toolPath}'", ex);
        }
    }

    internal record LoadToolsResult(ITool[] Tools, Exception[] Errors);
}