using Spectre.Console;
using NTokenizers.Extensions.Spectre.Console.Styles;

namespace Wayfare.UI;

public static class Palette
{
    // Color definitions matching the 16 exact RGB specifications
    public static readonly Color Black = new(0, 0, 0);          // 0: Black
    public static readonly Color DarkBlue = new(29, 43, 83);      // 1: Dark Blue
    public static readonly Color DarkPurple = new(126, 37, 83);   // 2: Dark Purple
    public static readonly Color DarkGreen = new(0, 135, 81);     // 3: Dark Green
    public static readonly Color Brown = new(171, 82, 54);        // 4: Brown
    public static readonly Color DarkGray = new(95, 87, 79);      // 5: Dark Gray
    public static readonly Color LightGray = new(194, 195, 199);  // 6: Light Gray
    public static readonly Color White = new(255, 241, 232);     // 7: White
    public static readonly Color Red = new(255, 0, 77);          // 8: Red
    public static readonly Color Orange = new(255, 163, 0);      // 9: Orange
    public static readonly Color Yellow = new(255, 236, 39);     // 10: Yellow
    public static readonly Color Green = new(0, 228, 54);        // 11: Green
    public static readonly Color Blue = new(41, 173, 255);       // 12: Blue
    public static readonly Color Lavender = new(131, 118, 156);  // 13: Lavender
    public static readonly Color Pink = new(255, 119, 168);      // 14: Pink
    public static readonly Color LightPeach = new(255, 204, 170); // 15: Light Peach

    // Hex String markup formatters for Spectre.Console markup strings
    public const string HexBlack = "#000000";
    public const string HexDarkBlue = "#1D2B53";
    public const string HexDarkPurple = "#7E2553";
    public const string HexDarkGreen = "#008751";
    public const string HexBrown = "#AB5236";
    public const string HexDarkGray = "#5F574F";
    public const string HexLightGray = "#C2C3C7";
    public const string HexWhite = "#FFF1E8";
    public const string HexRed = "#FF004D";
    public const string HexOrange = "#FFA300";
    public const string HexYellow = "#FFEC27";
    public const string HexGreen = "#00E436";
    public const string HexBlue = "#29ADFF";
    public const string HexLavender = "#83769C";
    public const string HexPink = "#FF77A8";
    public const string HexLightPeach = "#FFCCAA";

    public static MarkdownStyles CreateMarkdownStyles()
    {
        MarkdownStyles styles = MarkdownStyles.Default;

        styles.Heading = new Style(foreground: Yellow, decoration: Decoration.Bold);
        styles.Bold = new Style(foreground: LightPeach, decoration: Decoration.Bold);
        styles.Italic = new Style(foreground: Lavender, decoration: Decoration.Italic);
        styles.CodeInline = new Style(foreground: Blue, background: DarkBlue);
        styles.CodeBlock = new Style(foreground: White, background: DarkBlue);
        styles.Blockquote = new Style(foreground: Lavender, decoration: Decoration.Italic);
        styles.Link = new Style(foreground: Blue, decoration: Decoration.Underline);
        styles.HorizontalRule = new Style(foreground: DarkGreen);
        styles.UnorderedListItem = new Style(foreground: Green);
        styles.OrderedListItem = new Style(foreground: Orange);
        styles.DefaultStyle = new Style(foreground: White);

        return styles;
    }
}
