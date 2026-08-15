using System.Threading.Channels;
using System.Text;
using Spectre.Console;
using Wayfare.Core.Abstractions;
using Wayfare.Core.Events;
using Wayfare.UI.Components;

namespace Wayfare.UI;

public interface ITerminalUI
{
    Task<string> GetUserInputAsync(CancellationToken cancellationToken);
    Task WaitForCompletionAsync();
}

public class TerminalUI : ITerminalUI, IAsyncDisposable
{
    private bool _disposed;
    private bool _hasPrompted;
    private bool _hasRenderedToolsHeader;

    private readonly IEventBroker _eventBroker;
    private readonly Task _eventLoopTask;
    private readonly MarkdownStreamRenderer _markdownStreamRenderer;

    public TerminalUI(IEventBroker eventBroker, CancellationToken cancellationToken)
    {
        _eventBroker = eventBroker;
        _markdownStreamRenderer = new MarkdownStreamRenderer();
        Console.OutputEncoding = Encoding.UTF8;
        AnsiConsole.Clear();

        _eventLoopTask = Task.Run(() => ProcessEventsAsync(cancellationToken), cancellationToken);
    }

    public async Task<string> GetUserInputAsync(CancellationToken cancellationToken)
    {
        if (_hasPrompted)
        {
            AnsiConsole.WriteLine(Environment.NewLine);
        }
        else
        {
            _hasPrompted = true;
        }

        return await AnsiConsole.PromptAsync(
            new TextPrompt<string>($"[bold red]{Environment.UserName}[/][bold white]://>[/]")
                .PromptStyle("white")
                .AllowEmpty(),
            cancellationToken
        );
    }

    public async Task WaitForCompletionAsync()
    {
        try
        {
            await _eventLoopTask;
        }
        catch (Exception ex) when (ex is OperationCanceledException or ChannelClosedException)
        {
            // Expected completion / cancellation
        }
    }

    private async Task ProcessEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (IEvent @event in _eventBroker.ReadAllAsync(cancellationToken))
            {
                await HandleEventAsync(@event, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or ChannelClosedException)
        {
            // Shutdown expected
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]UI Event Processing Error: {Markup.Escape(ex.Message)}[/]");
        }
        finally
        {
            await _markdownStreamRenderer.CompleteStreamAsync();
        }
    }

    private void EnsureToolsHeaderRendered()
    {
        if (!_hasRenderedToolsHeader)
        {
            _hasRenderedToolsHeader = true;
            ProgressRenderer.RenderLoadingToolsStarted();
        }
    }

    private async Task HandleEventAsync(IEvent @event, CancellationToken cancellationToken)
    {
        switch (@event)
        {
            case StartupStartedEvent:
                await ProgressRenderer.RenderStartupStartedAsync(cancellationToken);
                break;
            case StartupCompletedEvent:
                await ProgressRenderer.RenderStartupCompletedAsync(cancellationToken);
                break;
            case AgentStartedEvent:
                ProgressRenderer.RenderStartAgent();
                break;
            case ToolCompilationStartedEvent e:
                EnsureToolsHeaderRendered();
                ProgressRenderer.RenderToolCompilationStarted(e.ToolName);
                break;
            case ToolCompilationCompletedEvent:
                ProgressRenderer.RenderToolCompilationCompleted();
                break;
            case ToolCompilationFailedEvent e:
                EnsureToolsHeaderRendered();
                ProgressRenderer.RenderToolCompilationFailed(e.ToolName, e.Error);
                break;
            case ToolLoadingStartedEvent e:
                EnsureToolsHeaderRendered();
                ProgressRenderer.RenderToolLoadingStarted(e.ToolName);
                break;
            case ToolLoadingCompletedEvent:
                ProgressRenderer.RenderToolLoadingCompleted();
                break;
            case ToolLoadingFailedEvent e:
                EnsureToolsHeaderRendered();
                ProgressRenderer.RenderToolLoadingFailed(e.ToolName, e.Error);
                break;
            case ChatRequestStartedEvent e:
                ProgressRenderer.RenderChatRequestStarted(e.Description);
                _markdownStreamRenderer.StartStream(cancellationToken);
                break;
            case ChatRequestCompletedEvent:
                await _markdownStreamRenderer.CompleteStreamAsync();
                break;
            case TokenChunkReceivedEvent e:
                await _markdownStreamRenderer.AppendChunkAsync(e.Content, cancellationToken);
                break;
            case ToolExecutionStartedEvent e:
                ProgressRenderer.RenderToolExecutionStarted(e.InvocationMessage);
                break;
            case ToolExecutionCompletedEvent e:
                ProgressRenderer.RenderToolExecutionCompleted(e.Success, e.DisplayMessage);
                break;
            case SessionUpdatedEvent:
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _eventBroker.Complete();

        try
        {
            await _eventLoopTask;
        }
        catch (Exception ex) when (ex is OperationCanceledException or ChannelClosedException)
        {
            // Ignore task cancellation
        }

        _markdownStreamRenderer.ForceDisposePipe();
    }
}
