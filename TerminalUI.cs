using Spectre.Console;

namespace WayFare;

internal interface ITerminalUI
{
    Task<string> GetUserInputAsync(CancellationToken cancellationToken);
}

internal class TerminalUI : ITerminalUI, IAgentEventSubscriber
{
    private bool _hasPrompted = false;
    private bool _isFirstThoughtChunk = true;

    public TerminalUI()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        AnsiConsole.Clear();
    }

    public async Task OnMessageAsync(IAgentEvent @event, CancellationToken cancellationToken)
    {
        switch (@event)
        {
            case StartupStarted:
                await OnStartupStartedAsync(cancellationToken);
                break;
            case StartupCompleted:
                await OnStartupCompletedAsync(cancellationToken);
                break;
            case StartAgent:
                OnStartAgent();
                break;
            case LoadingToolsStarted:
                OnLoadingToolsStarted();
                break;
            case ToolCompilationStarted toolCompilationStarted:
                OnToolCompilationStarted(toolCompilationStarted.ToolName);
                break;
            case ToolCompilationCompleted:
                OnToolCompilationCompleted();
                break;
            case ToolLoadingStarted toolLoadingStarted:
                OnToolLoadingStarted(toolLoadingStarted.ToolName);
                break;
            case ToolLoadingCompleted:
                OnToolLoadingCompleted();
                break;
            case ChatRequestStarted chatRequestStarted:
                OnChatRequestStarted(chatRequestStarted);
                break;
            case ChatRequestCompleted:
                OnChatRequestCompleted();
                break;
            case ThoughtChunkReceived thoughtChunkReceived:
                OnThoughtChunkReceived(thoughtChunkReceived.Message);
                break;
            case ToolExecutionStarted toolExecutionStarted:
                OnToolExecutionStarted(toolExecutionStarted.InvocationMessage);
                break;
            case ToolExecutionCompleted toolExecutionCompleted:
                OnToolExecutionCompleted(toolExecutionCompleted);
                break;
        }
    }

    private static async Task OnStartupStartedAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold white]WAYFARE INTERNATIONAL (C) 2026 // COGNITIVE AGENT DIVISION[/]");
        AnsiConsole.MarkupLine($"[cyan]SECURE LINK ESTABLISHED. STARTING BOOT SEQUENCE...[/]{Environment.NewLine}");

        Random random = new();

        AnsiConsole.Markup($"[cyan][[SYSTEM]][/] {Markup.Escape("Initialising hardware components...")}");
        await Task.Delay(random.Next(200, 550), cancellationToken);
        AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
        await Task.Delay(random.Next(100, 225), cancellationToken);
    }

    private static async Task OnStartupCompletedAsync(CancellationToken cancellationToken)
    {
        string[] bootSequences =
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

    private static void OnStartAgent()
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

    private void OnChatRequestStarted(ChatRequestStarted e)
    {
        AnsiConsole.Markup($"[cyan][[SYSTEM]][/] {Markup.Escape(e.Description)}");
        _isFirstThoughtChunk = true;
    }

    private void OnChatRequestCompleted()
    {
        if (_isFirstThoughtChunk)
        {
            AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
            _isFirstThoughtChunk = false;
        }
    }

    private void OnThoughtChunkReceived(string message)
    {
        if (_isFirstThoughtChunk)
        {
            AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
            _isFirstThoughtChunk = false;
        }
        AnsiConsole.Markup(Markup.Escape(message));
    }

    private static void OnToolExecutionStarted(string invocationMessage)
    {
        AnsiConsole.Markup($"[cyan][[SYSTEM]][/] Running {Markup.Escape(invocationMessage)}");
    }

    private void OnToolExecutionCompleted(ToolExecutionCompleted e)
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
