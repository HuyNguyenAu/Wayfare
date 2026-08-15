using Spectre.Console;

namespace Wayfare.UI.Components;

public static class ProgressRenderer
{
    public static async Task RenderStartupStartedAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold white]WAYFARE INTERNATIONAL (C) 2026 // COGNITIVE AGENT DIVISION[/]");
        AnsiConsole.MarkupLine($"[cyan]SECURE LINK ESTABLISHED. STARTING BOOT SEQUENCE...[/]{Environment.NewLine}");

        Random random = new();

        AnsiConsole.Markup($"[cyan][[SYSTEM]][/] {Markup.Escape("Initialising hardware components...")}");
        await Task.Delay(random.Next(200, 550), cancellationToken);
        AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
        await Task.Delay(random.Next(100, 225), cancellationToken);
    }

    public static async Task RenderStartupCompletedAsync(CancellationToken cancellationToken)
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

    public static void RenderStartAgent()
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

    public static void RenderLoadingToolsStarted()
    {
        AnsiConsole.MarkupLine("[cyan][[SYSTEM]][/] Initialising system tools...");
    }

    public static void RenderToolCompilationStarted(string toolName)
    {
        AnsiConsole.Markup($"[cyan][[SYSTEM]][/] {Markup.Escape($"Compiling {toolName}...")}");
    }

    public static void RenderToolCompilationCompleted()
    {
        AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
    }

    public static void RenderToolCompilationFailed(string toolName, string error)
    {
        AnsiConsole.MarkupLine(" [bold red][[FAILED]][/]");
        AnsiConsole.MarkupLine($"[red]Error compiling {toolName}: {error}[/]");
    }

    public static void RenderToolLoadingStarted(string toolName)
    {
        AnsiConsole.Markup($"[cyan][[SYSTEM]][/] {Markup.Escape($"Loading {toolName}...")}");
    }

    public static void RenderToolLoadingCompleted()
    {
        AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
    }

    public static void RenderToolLoadingFailed(string toolName, string error)
    {
        AnsiConsole.MarkupLine(" [bold red][[FAILED]][/]");
        AnsiConsole.MarkupLine($"[red]Error loading {toolName}: {error}[/]");
    }

    public static void RenderChatRequestStarted(string description)
    {
        AnsiConsole.Markup($"[cyan][[SYSTEM]][/] {Markup.Escape(description)}");
    }

    public static void RenderToolExecutionStarted(string invocationMessage)
    {
        AnsiConsole.Markup($"[cyan][[SYSTEM]][/] Running {Markup.Escape(invocationMessage)}");
    }

    public static void RenderToolExecutionCompleted(bool success, string displayMessage)
    {
        if (success)
        {
            AnsiConsole.MarkupLine(" [bold green][[OK]][/]");
            AnsiConsole.MarkupLine($"[cyan][[SYSTEM]][/] {Markup.Escape(displayMessage)}");
        }
        else
        {
            AnsiConsole.MarkupLine(" [bold red][[FAILED]][/]");
            AnsiConsole.MarkupLine($"[cyan][[SYSTEM]][/] {Markup.Escape(displayMessage)}");
        }
    }
}
