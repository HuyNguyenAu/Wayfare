namespace Wayfare.UI.Components;

using System.IO.Pipelines;
using System.Text;
using NTokenizers.Extensions.Spectre.Console;
using Spectre.Console;

public sealed class MarkdownStreamRenderer
{
    private Pipe? _markdownPipe;
    private Task? _markdownTask;
    private bool _isFirstChunk = true;
    public bool HeaderCompleted { get; set; }

    public void StartStream(CancellationToken cancellationToken)
    {
        _isFirstChunk = true;
        HeaderCompleted = false;
        _markdownPipe = new Pipe();
        _markdownTask = Task.Run(() => AnsiConsole.Console.WriteMarkdownAsync(
            _markdownPipe.Reader.AsStream(),
            ColourPalette.CreateMarkdownStyles(),
            Encoding.UTF8,
            cancellationToken
        ), cancellationToken);
    }

    public async Task AppendChunkAsync(string text, CancellationToken cancellationToken)
    {
        if (_isFirstChunk)
        {
            if (!HeaderCompleted)
            {
                AnsiConsole.MarkupLine($" [{ColourPalette.HexAlgaeLumens} bold]{Markup.Escape("[ ❦ ]")}[/]");
                HeaderCompleted = true;
            }

            _isFirstChunk = false;
        }

        if (_markdownPipe is not null)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            await _markdownPipe.Writer.WriteAsync(bytes, cancellationToken);
            await _markdownPipe.Writer.FlushAsync(cancellationToken);
        }
    }

    public async Task CompleteStreamAsync()
    {
        if (_isFirstChunk)
        {
            if (!HeaderCompleted)
            {
                AnsiConsole.MarkupLine($" [{ColourPalette.HexAlgaeLumens} bold]{Markup.Escape("[ ❦ ]")}[/]");
                HeaderCompleted = true;
            }

            _isFirstChunk = false;
        }

        if (_markdownPipe is not null)
        {
            await _markdownPipe.Writer.CompleteAsync();

            if (_markdownTask is not null)
            {
                try
                {
                    await _markdownTask;
                }
                catch (Exception)
                {
                    // Ignore cleanup exceptions.
                }
            }

            _markdownPipe = null;
            _markdownTask = null;
            AnsiConsole.WriteLine();
        }
    }

    public void ForceDisposePipe()
    {
        if (_markdownPipe is not null)
        {
            _markdownPipe.Writer.Complete();
            _markdownPipe.Reader.Complete();
        }

        if (_markdownTask is not null)
        {
            try
            {
                _markdownTask.Dispose();
            }
            catch (Exception)
            {
                // Ignore cleanup exceptions.
            }
        }
    }
}
