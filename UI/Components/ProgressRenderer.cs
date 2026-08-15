using Spectre.Console;

namespace Wayfare.UI.Components;

public static class ProgressRenderer
{
    public static void RenderStartupStarted()
    {
        AnsiConsole.MarkupLine($"[{Palette.HexOrange}][bold]WAYFARE // SOLAR PUNK SANCTUARY v1.0[/][/]");
        AnsiConsole.MarkupLine($"[{Palette.HexDarkGreen}]SOLAR ARRAY: CHARGED // HARMONIC GRID: ONLINE[/]{Environment.NewLine}");
        AnsiConsole.Markup($"[{Palette.HexBlue}][[SANCTUARY]][/] {Markup.Escape("Nurturing local environment...")}");
        AnsiConsole.MarkupLine($" [{Palette.HexDarkGreen}][bold][[HARMONIZED]][/][/]");
    }

    public static void RenderStartupCompleted()
    {
        AnsiConsole.MarkupLine($"[{Palette.HexBlue}][[SANCTUARY]][/] {Markup.Escape("Synchronising sanctuary network...")} [{Palette.HexDarkGreen}][bold][[HARMONIZED]][/][/]");
        AnsiConsole.MarkupLine($"[{Palette.HexBlue}][[SANCTUARY]][/] {Markup.Escape("Opening safe & collaborative space...")} [{Palette.HexDarkGreen}][bold][[HARMONIZED]][/][/]");
    }

    public static void RenderStartAgent()
    {
        AnsiConsole.Clear();
        Grid grid = new();
        grid.Expand();
        grid.AddColumn();
        grid.AddColumn(new GridColumn().RightAligned());
        grid.AddRow(
            new Markup($"[{Palette.HexOrange}][bold]▲ WAYFARE // SANCTUARY COMPANION v1.0[/][/]"),
            new Markup($"[{Palette.HexDarkGreen}][bold][[SUSTAINABLE ENERGY 100% // READY]][/][/]")
        );

        AnsiConsole.Write(grid);
        AnsiConsole.Write(new Rule().RuleStyle(new Style(foreground: Palette.DarkGreen)));
    }

    public static void RenderLoadingToolsStarted()
    {
        AnsiConsole.MarkupLine($"[{Palette.HexBlue}][[SANCTUARY]][/] Gathering sanctuary tools...");
    }

    public static void RenderToolCompilationStarted(string toolName)
    {
        AnsiConsole.Markup($"[{Palette.HexBlue}][[SANCTUARY]][/] {Markup.Escape($"Preparing {toolName}...")}");
    }

    public static void RenderToolCompilationCompleted()
    {
        AnsiConsole.MarkupLine($" [{Palette.HexDarkGreen}][bold][[HARMONIZED]][/][/]");
    }

    public static void RenderToolCompilationFailed(string toolName, string error)
    {
        AnsiConsole.MarkupLine($" [{Palette.HexRed}][bold][[WILTED]][/][/]");
        AnsiConsole.MarkupLine($"[{Palette.HexRed}]Issue preparing {toolName}: {error}[/]");
    }

    public static void RenderToolLoadingStarted(string toolName)
    {
        AnsiConsole.Markup($"[{Palette.HexBlue}][[SANCTUARY]][/] {Markup.Escape($"Welcoming {toolName}...")}");
    }

    public static void RenderToolLoadingCompleted()
    {
        AnsiConsole.MarkupLine($" [{Palette.HexDarkGreen}][bold][[HARMONIZED]][/][/]");
    }

    public static void RenderToolLoadingFailed(string toolName, string error)
    {
        AnsiConsole.MarkupLine($" [{Palette.HexRed}][bold][[WILTED]][/][/]");
        AnsiConsole.MarkupLine($"[{Palette.HexRed}]Issue welcoming {toolName}: {error}[/]");
    }

    public static void RenderChatRequestStarted(string description)
    {
        AnsiConsole.Markup($"[{Palette.HexBlue}][[SANCTUARY]][/] {Markup.Escape(description)}");
    }

    public static void RenderToolExecutionStarted(string invocationMessage)
    {
        AnsiConsole.Markup($"[{Palette.HexBlue}][[SANCTUARY]][/] Engaging {Markup.Escape(invocationMessage)}");
    }

    public static void RenderToolExecutionCompleted(bool success, string displayMessage)
    {
        if (success)
        {
            AnsiConsole.MarkupLine($" [{Palette.HexDarkGreen}][bold][[HARMONIZED]][/][/]");
            AnsiConsole.MarkupLine($"[{Palette.HexBlue}][[SANCTUARY]][/] [{Palette.HexWhite}]{Markup.Escape(displayMessage)}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($" [{Palette.HexRed}][bold][[WILTED]][/][/]");
            AnsiConsole.MarkupLine($"[{Palette.HexBlue}][[SANCTUARY]][/] [{Palette.HexRed}]{Markup.Escape(displayMessage)}[/]");
        }
    }
}
