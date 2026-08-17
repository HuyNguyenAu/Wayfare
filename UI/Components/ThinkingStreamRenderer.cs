namespace Wayfare.UI.Components;

using Spectre.Console;

public class ThinkingStreamRenderer
{
    private bool _isFirstChunk = true;
    private bool _hasRendered;

    public bool HasRendered => _hasRendered;

    public void StartStream()
    {
        _isFirstChunk = true;
        _hasRendered = false;
    }

    public Task AppendChunkAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(text) || cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        if (_isFirstChunk)
        {
            AnsiConsole.MarkupLine($" [{ColourPalette.HexAlgaeLumens} bold]{Markup.Escape("[ ❦ ]")}[/]");
            AnsiConsole.Markup($"  [{ColourPalette.HexSunlitOchre}]⁖ {Markup.Escape("[THOUGHT CANOPY]")}[/] ");
            _isFirstChunk = false;
            _hasRendered = true;
        }

        AnsiConsole.Console.Write(new Text(text, new Style(foreground: ColourPalette.SporeDust, decoration: Decoration.Italic)));

        return Task.CompletedTask;
    }

    public Task CompleteStreamAsync()
    {
        if (_hasRendered)
        {
            AnsiConsole.WriteLine();
            _hasRendered = false;
        }

        return Task.CompletedTask;
    }
}
