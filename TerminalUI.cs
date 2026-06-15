using System.IO.Pipelines;
using System.Text;
using System.Threading.Channels;
using NTokenizers.Extensions.Spectre.Console;
using NTokenizers.Extensions.Spectre.Console.Styles;
using Spectre.Console;

namespace WayFare;

internal interface ITerminalUI
{
    Task Startup(CancellationToken cancellationToken);
    Task FinaliseStartup(CancellationToken cancellationToken);
    void StartAgent();
    Task<string> GetUserInputAsync(CancellationToken cancellationToken);
}

internal class TerminalUI : ITerminalUI, IDisposable
{
    private bool _hasPrompted = false;
    private bool _isFirstThoughtChunk = true;
  
    private readonly Lock _consoleLock = new();
  
    private Pipe? _thoughtChunkPipe;
    private Stream? _thoughtChunkStream;
    private Task? _thoughtChunkTask;

    public TerminalUI(AgentEventHub eventHub, CancellationToken cancellationToken)
    {
        Console.OutputEncoding = Encoding.UTF8;
        AnsiConsole.Clear();

        _ = ProcessEventsAsync(eventHub.Reader, cancellationToken);
    }

    private async Task ProcessEventsAsync(ChannelReader<IAgentEvent> reader, CancellationToken cancellationToken)
    {
        try
        {
            while (await reader.WaitToReadAsync(cancellationToken))
            {
                while (reader.TryRead(out var @event))
                {
                    await HandleEventAsync(@event, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation exceptions during shutdown.
        }
    }

    private async Task HandleEventAsync(IAgentEvent @event, CancellationToken cancellationToken)
    {
        switch (@event)
        {
            case LoadingToolsStarted:
                OnLoadingToolsStarted();
                break;
            case ToolCompilationStarted e:
                OnToolCompilationStarted(e.ToolName);
                break;
            case ToolCompilationCompleted:
                OnToolCompilationCompleted();
                break;
            case ToolCompilationFailed e:
                OnToolCompilationFailed(e.ToolName, e.Error);
                break;
            case ToolLoadingStarted e:
                OnToolLoadingStarted(e.ToolName);
                break;
            case ToolLoadingCompleted:
                OnToolLoadingCompleted();
                break;
            case ToolLoadingFailed e:
                OnToolLoadingFailed(e.ToolName, e.Error);
                break;
            case ChatRequestStarted e:
                OnChatRequestStarted(e, cancellationToken);
                break;
            case ChatRequestCompleted:
                OnChatRequestCompleted();
                break;
            case ThoughtChunkReceived e:
                OnThoughtChunkReceived(e.Message);
                break;
            case ToolExecutionStarted e:
                OnToolExecutionStarted(e.InvocationMessage);
                break;
            case ToolExecutionCompleted e:
                OnToolExecutionCompleted(e);
                break;
        }
    }

    private void OnLoadingToolsStarted()
    {
        lock (_consoleLock)
        {
            AnsiConsole.MarkupLine("[cyan][[SYSTEM]][/] Initialising system tools...");
        }
    }

    private void OnToolCompilationStarted(string toolName)
    {
        lock (_consoleLock)
        {
            AnsiConsole.Markup($"[cyan][[SYSTEM]][/] {Markup.Escape($"Compiling {toolName}...")}");
        }
    }

    private void OnToolCompilationCompleted()
    {
        lock (_consoleLock)
        {
            AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
        }
    }

    private void OnToolLoadingStarted(string toolName)
    {
        lock (_consoleLock)
        {
            AnsiConsole.Markup($"[cyan][[SYSTEM]][/] {Markup.Escape($"Loading {toolName}...")}");
        }
    }

    private void OnToolLoadingCompleted()
    {
        lock (_consoleLock)
        {
            AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
        }
    }

    private void OnToolCompilationFailed(string toolName, string error)
    {
        lock (_consoleLock)
        {
            AnsiConsole.MarkupLine(" [bold red][[FAILED]][/]");
            AnsiConsole.MarkupLine($"[red]Error compiling {toolName}: {error}[/]");
        }
    }

    private void OnToolLoadingFailed(string toolName, string error)
    {
        lock (_consoleLock)
        {
            AnsiConsole.MarkupLine(" [bold red][[FAILED]][/]");
            AnsiConsole.MarkupLine($"[red]Error loading {toolName}: {error}[/]");
        }
    }

    private void CloseThoughtChunkStream()
    {
        _thoughtChunkPipe?.Writer.Complete();
        _thoughtChunkPipe = null;

        _thoughtChunkStream?.Dispose();
        _thoughtChunkStream = null;

        _thoughtChunkTask?.Dispose();
        _thoughtChunkTask = null;
    }

    private void OnChatRequestStarted(ChatRequestStarted e, CancellationToken cancellationToken)
    {
        lock (_consoleLock)
        {
            CloseThoughtChunkStream();

            AnsiConsole.Markup($"[cyan][[SYSTEM]][/] {Markup.Escape(e.Description)}");
            _isFirstThoughtChunk = true;

            _thoughtChunkPipe = new Pipe();
            _thoughtChunkStream = _thoughtChunkPipe.Writer.AsStream();
            _thoughtChunkTask = ProcessThoughtChunksAsync(_thoughtChunkPipe.Reader.AsStream(), cancellationToken);
        }
    }

    private static async Task ProcessThoughtChunksAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            await AnsiConsole.Console.WriteMarkdownAsync(stream, MarkdownStyles.Default, Encoding.UTF8, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation exceptions during shutdown.
        }
    }

    private void OnChatRequestCompleted()
    {
        lock (_consoleLock)
        {
            if (_isFirstThoughtChunk)
            {
                AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
                _isFirstThoughtChunk = false;
            }

            CloseThoughtChunkStream();
        }
    }

    private void OnThoughtChunkReceived(string message)
    {
        lock (_consoleLock)
        {
            if (_isFirstThoughtChunk)
            {
                AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
                _isFirstThoughtChunk = false;
            }

            if (_thoughtChunkStream is not null)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                _thoughtChunkStream.Write(bytes, 0, bytes.Length);
                _thoughtChunkStream.Flush();
            }
        }
    }

    private void OnToolExecutionStarted(string invocationMessage)
    {
        lock (_consoleLock)
        {
            AnsiConsole.Markup($"[cyan][[SYSTEM]][/] Running {Markup.Escape(invocationMessage)}");
        }
    }

    private void OnToolExecutionCompleted(ToolExecutionCompleted e)
    {
        lock (_consoleLock)
        {
            if (e.Success)
            {
                AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
                AnsiConsole.MarkupLine($"[cyan][[SYSTEM]][/] {Markup.Escape(e.DisplayMessage)}");
            }
            else
            {
                AnsiConsole.MarkupLine(" [bold red][[FAILED]][/]");
                AnsiConsole.MarkupLine($"[cyan][[SYSTEM]][/] {Markup.Escape(e.DisplayMessage)}");
            }

            _isFirstThoughtChunk = true;
        }
    }

    public async Task Startup(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold white]WAYFARE INTERNATIONAL (C) 2026 // COGNITIVE AGENT DIVISION[/]");
        AnsiConsole.MarkupLine($"[cyan]SECURE LINK ESTABLISHED. STARTING BOOT SEQUENCE...[/]{Environment.NewLine}");

        Random random = new();

        AnsiConsole.Markup($"[cyan][[SYSTEM]][/] {Markup.Escape("Initialising hardware components...")}");
        await Task.Delay(random.Next(200, 550), cancellationToken);
        AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
        await Task.Delay(random.Next(100, 225), cancellationToken);
    }

    public async Task FinaliseStartup(CancellationToken cancellationToken)
    {
        List<string> bootSequences =
        [
            "Finalising system checks...",
            "Establishing secure environment..."
        ];

        Random random = new();

        foreach (string bootSequence in bootSequences)
        {
            AnsiConsole.Markup($"[cyan][[SYSTEM]][/] {Markup.Escape(bootSequence)}");
            await Task.Delay(random.Next(200, 550), cancellationToken);
            AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
        }

        await Task.Delay(random.Next(100, 225), cancellationToken);
    }

    public void StartAgent()
    {
        Grid grid = new();
        grid.Expand();
        grid.AddColumn();
        grid.AddColumn(new GridColumn().RightAligned());
        grid.AddRow(
            new Markup("[yellow]▲ WAYFARE AGENT v0.1[/]")
        );

        AnsiConsole.Clear();
        AnsiConsole.Write(grid);
        AnsiConsole.Write(new Rule().RuleStyle("yellow"));
    }

    public Task<string> GetUserInputAsync(CancellationToken cancellationToken)
    {
        _isFirstThoughtChunk = true;

        if (_hasPrompted)
        {
            AnsiConsole.WriteLine(Environment.NewLine);
        }
        else
        {
            _hasPrompted = true;
        }

        return AnsiConsole.PromptAsync(
            new TextPrompt<string>($"[bold red]{Environment.UserName}[/][bold white]://>[/]")
                .PromptStyle("white")
                .AllowEmpty(),
            cancellationToken
        );
    }

    public void Dispose()
    {
        lock (_consoleLock)
        {
            CloseThoughtChunkStream();
        }
    }
}
