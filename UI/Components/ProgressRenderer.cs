using Spectre.Console;

namespace Wayfare.UI.Components;

public static class ProgressRenderer
{
    public static void RenderStartupStarted()
    {
        AnsiConsole.MarkupLine($"[{Palette.HexOrange} bold]WAYFARE // SOLAR BIOSPHERE v1.0[/]");
        AnsiConsole.MarkupLine($"[{Palette.HexDarkGreen}]PHOTOVOLTAIC ARRAY: OPTIMAL // ECOLOGICAL MESH: ONLINE[/]{Environment.NewLine}");
        AnsiConsole.Markup($"[{Palette.HexBlue}][[CANOPY]][/] {Markup.Escape("Calibrating micro-climate sensors...")}");
        AnsiConsole.MarkupLine($" [{Palette.HexDarkGreen} bold][[SYNCHRONISED]][/]");
    }

    public static void RenderStartupCompleted()
    {
        AnsiConsole.MarkupLine($"[{Palette.HexBlue}][[CANOPY]][/] {Markup.Escape("Interfacing with community mesh network...")} [{Palette.HexDarkGreen} bold][[SYNCHRONISED]][/]");
        AnsiConsole.MarkupLine($"[{Palette.HexBlue}][[CANOPY]][/] {Markup.Escape("Establishing regenerative workspace...")} [{Palette.HexDarkGreen} bold][[SYNCHRONISED]][/]");
    }

    public static void RenderStartAgent()
    {
        AnsiConsole.Clear();
        Grid grid = new();
        grid.Expand();
        grid.AddColumn();
        grid.AddColumn(new GridColumn().RightAligned());
        grid.AddRow(
            new Markup($"[{Palette.HexOrange} bold]◈ WAYFARE // BIOSPHERE COMPANION v1.0[/]"),
            new Markup($"[{Palette.HexDarkGreen} bold][[SOLAR RESERVE 100% // BALANCED]][/]")
        );

        AnsiConsole.Write(grid);
        AnsiConsole.Write(new Rule().RuleStyle(new Style(foreground: Palette.DarkGreen)));
    }

    public static void RenderLoadingToolsStarted()
    {
        AnsiConsole.MarkupLine($"[{Palette.HexBlue}][[CANOPY]][/] Cultivating toolchain modules...");
    }

    public static void RenderToolCompilationStarted(string toolName)
    {
        AnsiConsole.Markup($"[{Palette.HexBlue}][[CANOPY]][/] {Markup.Escape($"Compiling {toolName} interface...")}");
    }

    public static void RenderToolCompilationCompleted()
    {
        AnsiConsole.MarkupLine($" [{Palette.HexDarkGreen} bold][[SYNCHRONISED]][/]");
    }

    public static void RenderToolCompilationFailed(string toolName, string error)
    {
        AnsiConsole.MarkupLine($" [{Palette.HexRed} bold][[DEGRADED]][/]");
        AnsiConsole.MarkupLine($"[{Palette.HexRed}]Fault synthesizing {toolName}: {error}[/]");
    }

    public static void RenderToolLoadingStarted(string toolName)
    {
        AnsiConsole.Markup($"[{Palette.HexBlue}][[CANOPY]][/] {Markup.Escape($"Grafting module {toolName} into mesh...")}");
    }

    public static void RenderToolLoadingCompleted()
    {
        AnsiConsole.MarkupLine($" [{Palette.HexDarkGreen} bold][[SYNCHRONISED]][/]");
    }

    public static void RenderToolLoadingFailed(string toolName, string error)
    {
        AnsiConsole.MarkupLine($" [{Palette.HexRed} bold][[DEGRADED]][/]");
        AnsiConsole.MarkupLine($"[{Palette.HexRed}]Grafting failed for {toolName}: {error}[/]");
    }

    public static void RenderChatRequestStarted(IReadOnlyList<string> toolNames)
    {
        string statusDescription = toolNames.Count > 0
            ? $"Synthesising signals across [{string.Join(", ", toolNames)}]..."
            : "Channeling cognitive currents...";

        AnsiConsole.Markup($"[{Palette.HexBlue}][[CANOPY]][/] {Markup.Escape(statusDescription)}");
    }

    public static void RenderToolExecutionStarted(string invocationMessage)
    {
        AnsiConsole.Markup($"[{Palette.HexBlue}][[CANOPY]][/] Activating {Markup.Escape(invocationMessage)}");
    }

    public static void RenderToolExecutionCompleted(bool success, string displayMessage)
    {
        if (success)
        {
            AnsiConsole.MarkupLine($" [{Palette.HexDarkGreen} bold][[SYNCHRONISED]][/]");
            AnsiConsole.MarkupLine($"[{Palette.HexBlue}][[CANOPY]][/] [{Palette.HexWhite}]{Markup.Escape(displayMessage)}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($" [{Palette.HexRed} bold][[DEGRADED]][/]");
            AnsiConsole.MarkupLine($"[{Palette.HexBlue}][[CANOPY]][/] [{Palette.HexRed}]{Markup.Escape(displayMessage)}[/]");
        }
    }

    public static void RenderSquashingBranch()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{Palette.HexBlue}][[CANOPY]][/] {Markup.Escape("Compressing epoch turns into root milestone...")}");
    }

    public static void RenderObjectiveAndMilestones(string objective, IReadOnlyList<string> milestones)
    {
        AnsiConsole.WriteLine();

        Grid grid = new();
        grid.AddColumn();
        grid.AddRow(new Markup($"[{Palette.HexOrange} bold]// BIOSPHERE DIRECTIVE:[/] [{Palette.HexWhite}]{Markup.Escape(objective)}[/]"));
        grid.AddRow(new Markup($"[{Palette.HexBlue} bold]// HARVESTED MILESTONES [[{milestones.Count} CYCLES]]:[/]"));

        for (int i = 0; i < milestones.Count; i++)
        {
            grid.AddRow(new Markup($"  [{Palette.HexDarkGreen}]◈[/] [{Palette.HexLightPeach}]NODE-{i + 1:D2}[/] [{Palette.HexLightGray}]{Markup.Escape(milestones[i].Trim())}[/]"));
        }

        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();
    }
}