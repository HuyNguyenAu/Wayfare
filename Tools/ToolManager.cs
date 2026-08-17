namespace Wayfare.Tools;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Wayfare.Infrastructure.Events;

public sealed class LoadToolException(string message, Exception? innerException = null) : Exception(message, innerException);

public sealed class ToolManager(
    IEventPublisher eventPublisher,
    IReadOnlyList<ITool> builtInTools,
    IToolHelpers toolHelpers) : IToolManager
{
    private static readonly HashSet<string> _ignoreFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "ITool.cs",
        "ToolHelpers.cs",
        "InspectMilestoneTool.cs"
    };

    private readonly IEventPublisher _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    private readonly IReadOnlyList<ITool> _builtInTools = builtInTools ?? throw new ArgumentNullException(nameof(builtInTools));
    private readonly IToolHelpers _toolHelpers = toolHelpers ?? throw new ArgumentNullException(nameof(toolHelpers));

    public IReadOnlyList<ITool> Tools { get; private set; } = [];
    public IReadOnlyList<Exception> Errors { get; private set; } = [];

    public async Task LoadToolsAsync(string directoryPath, string searchPattern, string compiledDirectoryPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);
        ArgumentException.ThrowIfNullOrWhiteSpace(compiledDirectoryPath);

        LoadToolsResult loadToolsResult = await LoadToolsFromDirectoryAsync(directoryPath, searchPattern, compiledDirectoryPath, cancellationToken);

        Tools = [.. _builtInTools, .. loadToolsResult.Tools];
        Errors = loadToolsResult.Errors;
    }

    public ITool GetTool(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        ITool? tool = Tools.FirstOrDefault(toolCandidate => toolCandidate.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"No tool found with name '{name}'");

        return tool;
    }

    private async Task<LoadToolsResult> LoadToolsFromDirectoryAsync(string directoryPath, string searchPattern, string compiledDirectoryPath, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new ArgumentException($"Tool directory does not exist '{directoryPath}'", nameof(directoryPath));
        }

        Directory.CreateDirectory(compiledDirectoryPath);

        string[] toolFilePaths;

        try
        {
            toolFilePaths = Directory.GetFiles(directoryPath, searchPattern);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or PathTooLongException or DirectoryNotFoundException)
        {
            throw new LoadToolException($"Failed to access tool directory '{directoryPath}' with search pattern '{searchPattern}'", exception);
        }

        HashSet<string> activeToolNames = toolFilePaths
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name) && !_ignoreFiles.Contains($"{name}.cs"))
            .Select(name => name!)
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
                    catch (Exception exception)
                    {
                        throw new LoadToolException($"Failed to delete stale compiled tool '{dllFilePath}' for tool '{toolName}'", exception);
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
                    _eventPublisher.Publish(new ToolCompilationStartedEvent(toolName));
                    await CompileToolAsync(toolFilePath, dllPath, cancellationToken);
                    _eventPublisher.Publish(new ToolCompilationCompletedEvent());
                }
                catch (Exception exception)
                {
                    errors.Add(exception);

                    _eventPublisher.Publish(new ToolCompilationFailedEvent(toolName, exception.Message));

                    if (File.Exists(dllPath))
                    {
                        try
                        {
                            File.Delete(dllPath);
                        }
                        catch
                        {
                            // Best effort cleanup after compilation failure.
                        }
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
                _eventPublisher.Publish(new ToolLoadingStartedEvent(toolName));

                ITool tool = LoadTool(dllFilePath);
                tools.Add(tool);

                _eventPublisher.Publish(new ToolLoadingCompletedEvent());
            }
            catch (Exception exception)
            {
                errors.Add(exception);
                _eventPublisher.Publish(new ToolLoadingFailedEvent(toolName, exception.Message));
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
            "using System.Threading;",
            "using System.Threading.Tasks;",
            "using System.Text.Json;",
            "using System.Text.Json.Serialization;",
            "using Wayfare.Tools;",
            "using Wayfare.Session;",
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
            typeof(BinaryData).Assembly.Location,
            typeof(System.Text.Json.JsonSerializer).Assembly.Location,
            typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).Assembly.Location,
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
            catch (Exception exception)
            {
                throw new LoadToolException($"Failed to create metadata reference from '{location}'", exception);
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

    private ITool LoadTool(string toolPath)
    {
        try
        {
            using FileStream toolFileStream = new(toolPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(toolFileStream);

            Type toolType = assembly.GetTypes().FirstOrDefault(typeCandidate => typeof(ITool).IsAssignableFrom(typeCandidate) && !typeCandidate.IsInterface && !typeCandidate.IsAbstract)
                ?? throw new LoadToolException($"No valid implementation of ITool found in assembly '{toolPath}'.");

            if (Activator.CreateInstance(toolType, _toolHelpers) is not ITool toolInstance)
            {
                throw new LoadToolException($"Failed to load tool from '{toolPath}' because the type '{toolType.FullName}' does not implement ITool or could not be instantiated.");
            }

            return toolInstance;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or BadImageFormatException or ReflectionTypeLoadException)
        {
            throw new LoadToolException($"Failed to load tool from '{toolPath}'", exception);
        }
    }

    internal record LoadToolsResult(IReadOnlyList<ITool> Tools, IReadOnlyList<Exception> Errors);
}
