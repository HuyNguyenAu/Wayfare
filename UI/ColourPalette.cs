namespace Wayfare.UI;

using NTokenizers.Extensions.Spectre.Console.Styles;
using Spectre.Console;

public static class ColourPalette
{
    // Solarpunk TrueColour definitions (DESIGN.md Section 3.A).
    public static readonly Color TerracottaSol = new(217, 107, 67);  // #D96B43 - Active selections, solar harvest.
    public static readonly Color LivingCanopy = new(45, 90, 63);     // #2D5A3F - Background panels, tree hierarchy.
    public static readonly Color AlgaeLumens = new(115, 201, 145);   // #73C991 - Validated builds, clean tests.
    public static readonly Color SunlitOchre = new(229, 169, 60);    // #E5A93C - Grid alerts, branch divergences.
    public static readonly Color MyceliumLinen = new(246, 243, 235); // #F6F3EB - High legibility foreground text.
    public static readonly Color SporeDust = new(184, 163, 136);    // #B8A388 - Muted labels, secondary chrome.
    public static readonly Color BiolumAzure = new(56, 145, 166);    // #3891A6 - Local mesh connections.
    public static readonly Color ClayEmber = new(139, 58, 43);      // #8B3A2B - Runtime exceptions, broken links.
    public static readonly Color PeatMoss = new(19, 29, 23);         // #131D17 - Bioluminescent Night background.

    // Hex String markup formatters for Spectre.Console markup strings.
    public const string HexTerracottaSol = "#D96B43";
    public const string HexLivingCanopy = "#2D5A3F";
    public const string HexAlgaeLumens = "#73C991";
    public const string HexSunlitOchre = "#E5A93C";
    public const string HexMyceliumLinen = "#F6F3EB";
    public const string HexSporeDust = "#B8A388";
    public const string HexBiolumAzure = "#3891A6";
    public const string HexClayEmber = "#8B3A2B";
    public const string HexPeatMoss = "#131D17";

    private static readonly (TimeSpan Start, TimeSpan End, string Theme)[] CircadianThemes =
    [
        (new TimeSpan(5, 0, 0), new TimeSpan(8, 30, 0), "DAWN // AMBER MIST"),
        (new TimeSpan(8, 30, 0), new TimeSpan(17, 0, 0), "ZENITH // HIGH CANOPY"),
        (new TimeSpan(17, 0, 0), new TimeSpan(20, 0, 0), "GOLDEN HOUR // SAFFRON HARVEST"),
    ];

    public static string GetCircadianThemeName()
    {
        TimeSpan time = DateTime.Now.TimeOfDay;

        foreach (var (start, end, theme) in CircadianThemes)
        {
            if (time >= start && time < end)
            {
                return theme;
            }
        }

        return "BIOLUMINESCENT NIGHT // FIREFLY PEAT";
    }

    public static MarkdownStyles CreateMarkdownStyles()
    {
        MarkdownStyles styles = MarkdownStyles.Default;

        styles.Heading = new Style(foreground: TerracottaSol, decoration: Decoration.Bold);
        styles.Bold = new Style(foreground: SunlitOchre, decoration: Decoration.Bold);
        styles.Italic = new Style(foreground: SporeDust, decoration: Decoration.Italic);
        styles.CodeInline = new Style(foreground: BiolumAzure, background: LivingCanopy);
        styles.CodeBlock = new Style(foreground: MyceliumLinen, background: LivingCanopy);
        styles.Blockquote = new Style(foreground: SporeDust, decoration: Decoration.Italic);
        styles.Link = new Style(foreground: BiolumAzure, decoration: Decoration.Underline);
        styles.HorizontalRule = new Style(foreground: LivingCanopy);
        styles.UnorderedListItem = new Style(foreground: AlgaeLumens);
        styles.OrderedListItem = new Style(foreground: TerracottaSol);
        styles.DefaultStyle = new Style(foreground: MyceliumLinen);

        return styles;
    }
}
