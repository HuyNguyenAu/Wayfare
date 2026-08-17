namespace Wayfare.UI.Components;

using Spectre.Console;

public static class ProgressRenderer
{
    public static void RenderStartupStarted()
    {
        string theme = ColourPalette.GetCircadianThemeName();
        AnsiConsole.MarkupLine($"[{ColourPalette.HexTerracottaSol} bold]☼ WAYFARE // CHLOROPHYLL OS v3.5[/] [{ColourPalette.HexSporeDust}]({theme})[/]");
        AnsiConsole.MarkupLine($"[{ColourPalette.HexLivingCanopy}]☵ PHOTOVOLTAIC ARRAY: OPTIMAL  •  ⇋ LOCAL MESH: ONLINE[/]{Environment.NewLine}");
        AnsiConsole.Markup($"[{ColourPalette.HexBiolumAzure}]☵ {Markup.Escape("[CANOPY]")}[/] {Markup.Escape("Calibrating micro-climate sensors...")}");
        AnsiConsole.MarkupLine($" [{ColourPalette.HexAlgaeLumens} bold]{Markup.Escape("[ ❦ SYNCHRONISED ]")}[/]");
    }

    public static void RenderStartupCompleted()
    {
        AnsiConsole.MarkupLine($"[{ColourPalette.HexBiolumAzure}]☵ {Markup.Escape("[CANOPY]")}[/] {Markup.Escape("Interfacing with community mesh network...")} [{ColourPalette.HexAlgaeLumens} bold]{Markup.Escape("[ ❦ SYNCHRONISED ]")}[/]");
        AnsiConsole.MarkupLine($"[{ColourPalette.HexBiolumAzure}]☵ {Markup.Escape("[CANOPY]")}[/] {Markup.Escape("Establishing regenerative workspace...")} [{ColourPalette.HexAlgaeLumens} bold]{Markup.Escape("[ ❦ SYNCHRONISED ]")}[/]");
    }

    public static void RenderStartAgent()
    {
        AnsiConsole.Clear();
        Grid grid = new();
        grid.Expand();
        grid.AddColumn();
        grid.AddColumn(new GridColumn().RightAligned());
        grid.AddRow(
            new Markup($"[{ColourPalette.HexTerracottaSol} bold]☵ WAYFARE // VERDANT AGENT v3.5[/]"),
            new Markup($"[{ColourPalette.HexAlgaeLumens} bold]{Markup.Escape("[ ☼ SOLAR RESERVE 100% // BALANCED ]")}[/]")
        );

        AnsiConsole.Write(grid);
        AnsiConsole.Write(new Rule().RuleStyle(new Style(foreground: ColourPalette.LivingCanopy)));
    }

    public static void RenderLoadingToolsStarted()
    {
        AnsiConsole.MarkupLine($"[{ColourPalette.HexBiolumAzure}]⌕ {Markup.Escape("[SEEDLINGS]")}[/] Cultivating toolchain modules...");
    }

    public static void RenderToolCompilationStarted(string toolName)
    {
        AnsiConsole.Markup($"[{ColourPalette.HexBiolumAzure}]⑂ {Markup.Escape("[GRAFTING]")}[/] {Markup.Escape($"Compiling {toolName} interface...")}");
    }

    public static void RenderToolCompilationCompleted()
    {
        AnsiConsole.MarkupLine($" [{ColourPalette.HexAlgaeLumens} bold]{Markup.Escape("[ ❦ SYNCHRONISED ]")}[/]");
    }

    public static void RenderToolCompilationFailed(string toolName, string error)
    {
        AnsiConsole.MarkupLine($" [{ColourPalette.HexClayEmber} bold]{Markup.Escape("[ ⌁ DEGRADED ]")}[/]");
        AnsiConsole.MarkupLine($"[{ColourPalette.HexClayEmber}]⚠ Fault synthesising {Markup.Escape(toolName)}: {Markup.Escape(error)}[/]");
    }

    public static void RenderToolLoadingStarted(string toolName)
    {
        AnsiConsole.Markup($"[{ColourPalette.HexBiolumAzure}]⑂ {Markup.Escape("[GRAFTING]")}[/] {Markup.Escape($"Grafting module {toolName} into mesh...")}");
    }

    public static void RenderToolLoadingCompleted()
    {
        AnsiConsole.MarkupLine($" [{ColourPalette.HexAlgaeLumens} bold]{Markup.Escape("[ ❦ SYNCHRONISED ]")}[/]");
    }

    public static void RenderToolLoadingFailed(string toolName, string error)
    {
        AnsiConsole.MarkupLine($" [{ColourPalette.HexClayEmber} bold]{Markup.Escape("[ ⌁ DEGRADED ]")}[/]");
        AnsiConsole.MarkupLine($"[{ColourPalette.HexClayEmber}]⚠ Grafting failed for {Markup.Escape(toolName)}: {Markup.Escape(error)}[/]");
    }

    public static void RenderChatRequestStarted(IReadOnlyList<string> toolNames)
    {
        string statusDescription = toolNames.Count > 0
            ? $"Synthesising signals across [{string.Join(", ", toolNames)}]..."
            : "Channelling cognitive currents...";

        AnsiConsole.Markup($"[{ColourPalette.HexBiolumAzure}]⁖ {Markup.Escape("[DELIBERATING]")}[/] {Markup.Escape(statusDescription)}");
    }

    public static void RenderToolExecutionStarted(string invocationMessage)
    {
        AnsiConsole.Markup($"[{ColourPalette.HexBiolumAzure}]⑂ {Markup.Escape("[ACTIVATING]")}[/] {Markup.Escape(invocationMessage)}");
    }

    public static void RenderToolExecutionCompleted(bool success, string displayMessage)
    {
        if (success)
        {
            AnsiConsole.MarkupLine($" [{ColourPalette.HexAlgaeLumens} bold]{Markup.Escape("[ ❦ SYNCHRONISED ]")}[/]");
            AnsiConsole.MarkupLine($"[{ColourPalette.HexAlgaeLumens}]❦ {Markup.Escape("[OBSERVATION]")}[/] [{ColourPalette.HexMyceliumLinen}]{Markup.Escape(displayMessage)}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($" [{ColourPalette.HexClayEmber} bold]{Markup.Escape("[ ⌁ DEGRADED ]")}[/]");
            AnsiConsole.MarkupLine($"[{ColourPalette.HexClayEmber}]⌁ {Markup.Escape("[DEGRADED]")}[/] [{ColourPalette.HexClayEmber}]⚠ {Markup.Escape(displayMessage)}[/]");
        }
    }

    public static void RenderSquashingBranch()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{ColourPalette.HexBiolumAzure}]✁ {Markup.Escape("[PRUNING]")}[/] {Markup.Escape("Compressing epoch turns into root milestone...")}");
    }

    public static void RenderObjectiveAndMilestones(string objective, IReadOnlyList<string> milestones)
    {
        AnsiConsole.WriteLine();

        Grid grid = new();
        grid.AddColumn();
        grid.AddRow(new Markup($"[{ColourPalette.HexTerracottaSol} bold]⑂ BIOSPHERE DIRECTIVE:[/] [{ColourPalette.HexMyceliumLinen}]{Markup.Escape(objective)}[/]"));
        grid.AddRow(new Markup($"[{ColourPalette.HexBiolumAzure} bold]{Markup.Escape($"❦ HARVESTED MILESTONES [{milestones.Count} CYCLES]:")}[/]"));

        for (int milestoneIndex = 0; milestoneIndex < milestones.Count; milestoneIndex++)
        {
            grid.AddRow(new Markup($"  [{ColourPalette.HexAlgaeLumens}]❦[/] [{ColourPalette.HexTerracottaSol}]NODE-{milestoneIndex + 1:D2}[/] [{ColourPalette.HexSporeDust}]{Markup.Escape(milestones[milestoneIndex].Trim())}[/]"));
        }

        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();
    }
}