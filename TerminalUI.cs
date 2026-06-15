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
    private bool _hasPrompted;
    private bool _isFirstThoughtChunk = true;

    public TerminalUI(IEventSubscriber events)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        AnsiConsole.Clear();

        events.Subscribe<LoadingToolsStarted>(_ => OnLoadingToolsStarted());
        events.Subscribe<ToolCompilationStarted>(e => OnToolCompilationStarted(e.ToolName));
        events.Subscribe<ToolCompilationCompleted>(_ => OnToolCompilationCompleted());
        events.Subscribe<ToolLoadingStarted>(e => OnToolLoadingStarted(e.ToolName));
        events.Subscribe<ToolLoadingCompleted>(_ => OnToolLoadingCompleted());

        events.Subscribe<ThoughtChunkReceived>(e => OnThoughtChunkReceived(e.Message));
        events.Subscribe<ToolExecutionStarted>(e => OnToolExecutionStarted(e.InvocationMessage));
        events.Subscribe<ToolExecutionCompleted>(e => OnToolExecutionCompleted(e.ToolName, e.Result));
    }

    private static void OnLoadingToolsStarted()
    {
        AnsiConsole.MarkupLine("[cyan][[SYSTEM]][/] Initialising system tools...");
    }

    private static void OnToolCompilationStarted(string toolName)
    {
        AnsiConsole.Markup($"[cyan][[SYSTEM]][/] {Markup.Escape($"Compiling {toolName}...")}");
    }

    private static void OnToolCompilationCompleted()
    {
        AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
    }

    private static void OnToolLoadingStarted(string toolName)
    {
        AnsiConsole.Markup($"[cyan][[SYSTEM]][/] {Markup.Escape($"Loading {toolName}...")}");
    }

    private static void OnToolLoadingCompleted()
    {
        AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
    }

    private void OnThoughtChunkReceived(string message)
    {
        if (_isFirstThoughtChunk)
        {
            AnsiConsole.Markup("[cyan][[SYSTEM]] [/]");
            _isFirstThoughtChunk = false;
        }
        AnsiConsole.Markup(Markup.Escape(message));
    }

    private static void OnToolExecutionStarted(string invocationMessage)
    {
        AnsiConsole.Markup($"[cyan][[SYSTEM]][/] Running {Markup.Escape(invocationMessage)}");
    }

    private void OnToolExecutionCompleted(string toolName, string result)
    {
        AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
        AnsiConsole.MarkupLine($"[cyan][[SYSTEM]][/] [white]{Markup.Escape(result)}[/]");
        _isFirstThoughtChunk = true;
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
            AnsiConsole.WriteLine();
        }
        else
        {
            _hasPrompted = true;
        }

        return AnsiConsole.PromptAsync(
            new TextPrompt<string>("[bold red]You[/][bold white]://>[/]")
                .PromptStyle("white")
                .AllowEmpty(),
            cancellationToken
        );
    }
}
