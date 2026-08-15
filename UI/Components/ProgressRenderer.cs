using Spectre.Console;

namespace Wayfare.UI.Components;

public static class ProgressRenderer
{
    public static async Task RenderStartupStartedAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine($"[{Palette.HexOrange}][bold]WAYFARE // ECO-COGNITIVE SYSTEM v1.0[/][/]");
        AnsiConsole.MarkupLine($"[{Palette.HexDarkGreen}]SOLAR ARRAY: ONLINE // COGNITIVE GRID: ACTIVE[/]{Environment.NewLine}");

        Random random = new();

        AnsiConsole.Markup($"[{Palette.HexBlue}][[SYSTEM]][/] {Markup.Escape("Initialising hardware components...")}");
        await Task.Delay(random.Next(200, 550), cancellationToken);
        AnsiConsole.MarkupLine($" [{Palette.HexDarkGreen}][bold][[OK]][/][/]");
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
            AnsiConsole.Markup($"[{Palette.HexBlue}][[SYSTEM]][/] {Markup.Escape(bootSequence)}");
            await Task.Delay(random.Next(200, 550), cancellationToken);
            AnsiConsole.MarkupLine($" [{Palette.HexDarkGreen}][bold][[OK]][/][/]");
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
            new Markup($"[{Palette.HexOrange}][bold]▲ WAYFARE AGENT v0.1[/][/]"),
            new Markup($"[{Palette.HexDarkGreen}][bold][[ONLINE // SOLAR POWER 100%]][/][/]")
        );

        AnsiConsole.Write(grid);
        AnsiConsole.Write(new Rule().RuleStyle(new Style(foreground: Palette.DarkGreen)));
    }

    public static void RenderLoadingToolsStarted()
    {
        AnsiConsole.MarkupLine($"[{Palette.HexBlue}][[SYSTEM]][/] Initialising system tools...");
    }

    public static void RenderToolCompilationStarted(string toolName)
    {
        AnsiConsole.Markup($"[{Palette.HexBlue}][[SYSTEM]][/] {Markup.Escape($"Compiling {toolName}...")}");
    }

    public static void RenderToolCompilationCompleted()
    {
        AnsiConsole.MarkupLine($" [{Palette.HexDarkGreen}][bold][[OK]][/][/]");
    }

    public static void RenderToolCompilationFailed(string toolName, string error)
    {
        AnsiConsole.MarkupLine($" [{Palette.HexRed}][bold][[FAILED]][/][/]");
        AnsiConsole.MarkupLine($"[{Palette.HexRed}]Error compiling {toolName}: {error}[/]");
    }

    public static void RenderToolLoadingStarted(string toolName)
    {
        AnsiConsole.Markup($"[{Palette.HexBlue}][[SYSTEM]][/] {Markup.Escape($"Loading {toolName}...")}");
    }

    public static void RenderToolLoadingCompleted()
    {
        AnsiConsole.MarkupLine($" [{Palette.HexDarkGreen}][bold][[OK]][/][/]");
    }

    public static void RenderToolLoadingFailed(string toolName, string error)
    {
        AnsiConsole.MarkupLine($" [{Palette.HexRed}][bold][[FAILED]][/][/]");
        AnsiConsole.MarkupLine($"[{Palette.HexRed}]Error loading {toolName}: {error}[/]");
    }

    public static void RenderChatRequestStarted(string description)
    {
        AnsiConsole.Markup($"[{Palette.HexBlue}][[SYSTEM]][/] {Markup.Escape(description)}");
    }

    public static void RenderToolExecutionStarted(string invocationMessage)
    {
        AnsiConsole.Markup($"[{Palette.HexBlue}][[SYSTEM]][/] Running {Markup.Escape(invocationMessage)}");
    }

    public static void RenderToolExecutionCompleted(bool success, string displayMessage)
    {
        if (success)
        {
            AnsiConsole.MarkupLine($" [{Palette.HexDarkGreen}][bold][[OK]][/][/]");
            AnsiConsole.MarkupLine($"[{Palette.HexBlue}][[SYSTEM]][/] [{Palette.HexWhite}]{Markup.Escape(displayMessage)}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($" [{Palette.HexRed}][bold][[FAILED]][/][/]");
            AnsiConsole.MarkupLine($"[{Palette.HexBlue}][[SYSTEM]][/] [{Palette.HexRed}]{Markup.Escape(displayMessage)}[/]");
        }
    }
}
