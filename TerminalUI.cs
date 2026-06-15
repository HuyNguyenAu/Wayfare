using Spectre.Console;

namespace WayFare;

internal interface ITerminalUI
{
    Task Startup(CancellationToken cancellationToken);
    Task FinaliseStartup(CancellationToken cancellationToken);
    void StartAgent();
    Task<string> GetUserInputAsync(CancellationToken cancellationToken);
}

internal class TerminalUI : ITerminalUI
{
    private bool _hasPrompted = false;
    private bool _isFirstThoughtChunk = true;
    private readonly Lock _consoleLock = new();

    public TerminalUI(IEventSubscriber events)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        AnsiConsole.Clear();

        events.Subscribe<LoadingToolsStarted>(_ => OnLoadingToolsStarted());
        events.Subscribe<ToolCompilationStarted>(e => OnToolCompilationStarted(e.ToolName));
        events.Subscribe<ToolCompilationCompleted>(_ => OnToolCompilationCompleted());
        events.Subscribe<ToolLoadingStarted>(e => OnToolLoadingStarted(e.ToolName));
        events.Subscribe<ToolLoadingCompleted>(_ => OnToolLoadingCompleted());

        events.Subscribe<ChatRequestStarted>(e => OnChatRequestStarted(e));
        events.Subscribe<ChatRequestCompleted>(_ => OnChatRequestCompleted());
        events.Subscribe<ThoughtChunkReceived>(e => OnThoughtChunkReceived(e.Message));
        events.Subscribe<ToolExecutionStarted>(e => OnToolExecutionStarted(e.InvocationMessage));
        events.Subscribe<ToolExecutionCompleted>(OnToolExecutionCompleted);
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

    private void OnChatRequestStarted(ChatRequestStarted e)
    {
        lock (_consoleLock)
        {
            AnsiConsole.Markup($"[cyan][[SYSTEM]][/] {Markup.Escape(e.Description)}");
            _isFirstThoughtChunk = true;
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
            AnsiConsole.Markup(Markup.Escape(message));
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
        AnsiConsole.Clear();
        Grid grid = new();
        grid.Expand();
        grid.AddColumn();
        grid.AddColumn(new GridColumn().RightAligned());
        grid.AddRow(
            new Markup("[yellow]▲ WAYFARE AGENT v0.1[/]")
        );

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
}
